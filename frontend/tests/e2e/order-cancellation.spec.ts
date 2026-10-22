import { test, expect } from '@playwright/test';
import { adminWrite, loginAdmin } from './support/admin-session';
const shop = 'http://localhost:4200';
const admin = 'http://localhost:4201';
const cartPath = '/api/v1/ordering/cart';
const person = {
  customerType: 'company',
  fullName: 'Client Demo',
  email: 'client@example.test',
  phone: '+40 700 000 000',
  companyName: 'Firma Demo',
  taxId: 'RO123456',
  registrationNumber: null,
  shippingAddress: {
    street: 'Strada Exemplu 1',
    city: 'București',
    county: 'Sector 1',
    postalCode: '010101',
    country: 'RO',
  },
  billingSameAsShipping: false,
  billingAddress: {
    street: 'Strada Facturare 2',
    city: 'Cluj-Napoca',
    county: 'Cluj',
    postalCode: '400001',
    country: 'RO',
  },
};
test('admin cancels confirmed order and restores stock exactly once', async ({ page }) => {
  test.setTimeout(90000);
  await loginAdmin(page.request, admin);
  const stamp = crypto.randomUUID();
  const write = (path: string, data: object = {}, method = 'POST') =>
    adminWrite(page.request, admin, path, data, method);
  const create = async (path: string, data: object) => {
    const r = await write(path, data);
    expect(r.status()).toBe(200);
    return (await r.json()).id as string;
  };
  const slug = 'preparation-' + stamp;
  const productId = await create('/api/v1/admin/products', {
    name: 'Pregătire demo',
    slug,
    category: 'Verificare automată',
    description: 'Verificare rezervare.',
  });
  const productPath = '/api/v1/admin/products/' + productId;
  const csrf = (await (await page.request.get(shop + cartPath + '/csrf')).json()).token;
  const headers = { 'X-CART-CSRF': csrf };
  try {
    const variantId = await create(productPath + '/variants', {
      sku: 'PREP-' + stamp,
      label: 'M6',
      diameterMm: 6,
      lengthMm: 20,
      material: 'Oțel',
      finish: 'Zincat',
    });
    const offers: string[] = [];
    for (const units of [10, 5]) {
      const id = await create(productPath + '/variants/' + variantId + '/offers', {
        code: 'PREP-' + stamp + '-' + units,
        label: 'Pungă ' + units,
        unitsPerPack: units,
        minPacks: 1,
        packStep: 1,
      });
      offers.push(id);
      expect(
        (
          await write(
            '/api/v1/admin/offers/' + id + '/price',
            { netAmount: units, taxRate: 0.21 },
            'PUT',
          )
        ).ok(),
      ).toBe(true);
    }
    expect(
      (
        await write('/api/v1/admin/variants/' + variantId + '/receipts', {
          requestId: crypto.randomUUID(),
          quantity: 100,
          reference: 'PREP-' + stamp,
        })
      ).ok(),
    ).toBe(true);
    expect((await write(productPath + '/publish')).ok()).toBe(true);
    let cart = await (await page.request.post(shop + cartPath + '/session', { headers })).json();
    cart = await (
      await page.request.put(shop + cartPath, {
        headers,
        data: {
          cartId: cart.id,
          version: cart.version,
          lines: offers.map((offerId, i) => ({ productSlug: slug, offerId, packs: i + 2 })),
        },
      })
    ).json();
    expect(
      (
        await page.request.put(shop + cartPath + '/customer', {
          headers,
          data: { cartId: cart.id, version: cart.version, customer: person },
        })
      ).status(),
    ).toBe(200);
    await page.goto(shop + '/cos');
    await expect(page.getByRole('heading', { name: 'Total cu TVA: 42,35 RON' })).toBeVisible();
    const prep = page.getByRole('region', { name: 'Rezervare demonstrativă' });
    await page.getByRole('button', { name: 'Pregătește și rezervă stocul' }).click();
    await expect(prep.getByRole('status')).toHaveText('Stoc rezervat', { timeout: 20000 });
    const prepared = await (await page.request.get(shop + cartPath + '/preparation')).json();
    const confirmed = await page.request.post(
      shop + cartPath + '/preparation/' + prepared.id + '/confirm',
      { headers },
    );
    expect(confirmed.status()).toBe(200);
    const number = (await confirmed.json()).number;
    await expect
      .poll(
        async () =>
          (await (await page.request.get(shop + '/api/v1/ordering/demo-orders')).json()).items[0]
            .status,
        { timeout: 20000 },
      )
      .toBe('Confirmed');
    const cancelPath = '/api/v1/admin/sales-orders/' + prepared.id + '/cancel';
    for (const origin of [admin, 'http://localhost:5104']) {
      expect(
        (
          await page.request.post(origin + cancelPath, { data: { reason: 'Cerere client' } })
        ).status(),
      ).toBe(400);
    }
    const orderCsrf = (await (await page.request.get(admin + '/api/auth/csrf')).json()).token;
    const adminHeaders = { 'X-CSRF-TOKEN': orderCsrf };
    expect(
      (
        await page.request.post('http://localhost:5104' + cancelPath, {
          headers: adminHeaders,
          data: { reason: 'ab' },
        })
      ).status(),
    ).toBe(400);
    await page.goto(admin + '/comenzi');
    await page.getByLabel('Număr comandă', { exact: true }).fill(number);
    await page.getByRole('button', { name: 'Aplică filtrele' }).click();
    await page
      .getByLabel('Lista comenzilor', { exact: true })
      .getByRole('button', { name: new RegExp(number) })
      .click();
    await page.getByLabel('Motivul anulării', { exact: true }).fill('Cerere client');
    const cancelButton = page.getByRole('button', { name: 'Anulează comanda și restituie stocul' });
    await expect(cancelButton).toBeDisabled();
    await page
      .getByRole('checkbox', { name: 'Confirm anularea integrală și restituirea stocului' })
      .check();
    await page.setViewportSize({ width: 390, height: 844 });
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(
      true,
    );
    await page.screenshot({ path: '../work/order-cancellation-mobile.png', fullPage: true });
    // The server accepts the intent but the browser loses the response.
    await page.route(
      '**' + cancelPath,
      async (route) => {
        await route.fetch();
        await route.abort('failed');
      },
      { times: 1 },
    );
    await cancelButton.click();
    await expect(page.getByRole('alert')).toContainText('Actualizează starea');
    await expect
      .poll(
        async () =>
          (
            await (
              await page.request.get(admin + '/api/v1/admin/sales-orders/' + prepared.id)
            ).json()
          ).order.status,
        { timeout: 20000 },
      )
      .toBe('Cancelled');
    await page.getByRole('button', { name: 'Actualizează detaliile' }).click();
    await expect(page.getByRole('region', { name: 'Detalii comandă', exact: true })).toContainText(
      'Stocul a fost restituit integral',
    );
    expect(
      (
        await page.request.post('http://localhost:5104' + cancelPath, {
          headers: adminHeaders,
          data: { reason: 'Cerere client' },
        })
      ).status(),
    ).toBe(200);
    expect(
      (
        await page.request.post('http://localhost:5104' + cancelPath, {
          headers: adminHeaders,
          data: { reason: 'Alt motiv' },
        })
      ).status(),
    ).toBe(409);
    const ledger = await (
      await page.request.get(admin + '/api/v1/admin/variants/' + variantId + '/movements')
    ).json();
    expect(ledger.onHand).toBe(100);
    expect(ledger.reserved).toBe(0);
    expect(ledger.items.filter((x: { kind: string }) => x.kind === 'Restoration')).toHaveLength(1);
    expect(ledger.items.find((x: { kind: string }) => x.kind === 'Restoration').quantity).toBe(35);
    await page.goto(shop + '/cos');
    await expect(
      page.getByRole('button', { name: 'Golește coșul comenzii anulate' }),
    ).toBeVisible();
    await page.getByRole('button', { name: 'Golește coșul comenzii anulate' }).click();
    const history = page.getByRole('region', { name: 'Comenzi demonstrative' });
    await expect(history).toContainText(number);
    await expect(history).toContainText('Anulată');
  } finally {
    test.setTimeout(test.info().timeout + 15000);
    await write(productPath + '/archive');
  }
});
test('anonymous cancellation and public restoration routes are denied', async ({ request }) => {
  const id = crypto.randomUUID();
  for (const origin of ['http://127.0.0.1:5099', 'http://127.0.0.1:5104'])
    expect(
      (
        await request.post(origin + '/api/v1/admin/sales-orders/' + id + '/cancel', {
          data: { reason: 'Cerere client' },
        })
      ).status(),
    ).toBe(401);
  expect(
    (
      await request.post('http://127.0.0.1:5102/internal/reservations/' + id + '/restore', {
        data: { orderId: id },
      })
    ).status(),
  ).toBe(401);
  expect(
    (
      await request.post('http://127.0.0.1:5099/internal/reservations/' + id + '/restore', {
        data: { orderId: id },
      })
    ).status(),
  ).toBe(404);
});
