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
test('admin filters orders, reads immutable customer details and recovers read failures', async ({
  page,
}) => {
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
    await page.goto(admin + '/comenzi');
    await page.getByLabel('Număr comandă', { exact: true }).fill(number);
    await page.getByLabel('Stare comandă', { exact: true }).selectOption('Confirmed');
    await page.getByRole('button', { name: 'Aplică filtrele' }).click();
    const list = page.getByLabel('Lista comenzilor', { exact: true });
    await expect(list.getByRole('button')).toHaveCount(1);
    await list.getByRole('button', { name: new RegExp(number) }).click();
    const detail = page.getByRole('region', { name: 'Detalii comandă', exact: true });
    await expect(detail.getByRole('heading', { name: number, exact: true })).toBeVisible();
    await expect(detail).toContainText('Firma Demo');
    await expect(detail).toContainText('RO123456');
    await expect(detail).toContainText('Strada Facturare 2');
    await expect(detail).toContainText('Total cu TVA: 42,35 RON');
    await page.getByLabel('Stare comandă', { exact: true }).selectOption('Failed');
    await page.getByRole('button', { name: 'Aplică filtrele' }).click();
    await expect(list).toContainText('Nu există comenzi');
    await expect(detail.getByRole('heading', { name: number, exact: true })).toBeVisible();
    await page.getByLabel('Stare comandă', { exact: true }).selectOption('Confirmed');
    await page.getByRole('button', { name: 'Aplică filtrele' }).click();
    await expect(list.getByRole('button')).toHaveCount(1);
    await page.route(
      '**/api/v1/admin/sales-orders/' + prepared.id,
      (route) =>
        route.fulfill({
          status: 503,
          contentType: 'application/problem+json',
          body: JSON.stringify({ detail: 'Detaliile sunt indisponibile temporar.' }),
        }),
      { times: 1 },
    );
    await page.getByRole('button', { name: 'Actualizează detaliile' }).click();
    await expect(page.getByRole('alert')).toContainText('indisponibile');
    await expect(detail).toHaveCount(0);
    await page.getByRole('button', { name: 'Actualizează detaliile' }).click();
    await expect(detail).toContainText(number);
    await page.route(
      '**/api/v1/admin/sales-orders?**',
      (route) =>
        route.fulfill({
          status: 503,
          contentType: 'application/problem+json',
          body: JSON.stringify({ detail: 'Lista este indisponibilă temporar.' }),
        }),
      { times: 1 },
    );
    await page.getByRole('button', { name: 'Actualizează lista' }).click();
    await expect(page.getByRole('alert')).toContainText('Lista este indisponibilă');
    await expect(list).toHaveCount(0);
    await page.getByRole('button', { name: 'Actualizează lista' }).click();
    await expect(list.getByRole('button')).toHaveCount(1);
    await page.setViewportSize({ width: 1440, height: 1000 });
    await page.screenshot({ path: '../work/admin-orders-desktop.png', fullPage: true });
    await page.setViewportSize({ width: 390, height: 844 });
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(
      true,
    );
    await page.screenshot({ path: '../work/admin-orders-mobile.png', fullPage: true });
    const result = await page.request.get(
      'http://localhost:5104/api/v1/admin/sales-orders/' + prepared.id,
    );
    expect(result.status()).toBe(200);
    expect(result.headers()['cache-control']).toBe('no-store');
    const data = await result.json();
    expect(data.order.number).toBe(number);
    expect(data.reservation.status).toBe('Consumed');
    expect(JSON.stringify(data)).not.toContain('ownerHash');
    await page.request.post(shop + cartPath + '/preparation/' + prepared.id + '/complete', {
      headers,
    });
  } finally {
    test.setTimeout(test.info().timeout + 15000);
    await write(productPath + '/archive');
  }
});
test('administrative order reads require session through gateway and direct service', async ({
  request,
}) => {
  const path = '/api/v1/admin/sales-orders';
  const id = crypto.randomUUID();
  for (const origin of ['http://127.0.0.1:5099', 'http://127.0.0.1:5104']) {
    expect((await request.get(origin + path)).status()).toBe(401);
    expect((await request.get(origin + path + '/' + id)).status()).toBe(401);
  }
  await loginAdmin(request, 'http://127.0.0.1:5099');
  for (const origin of ['http://127.0.0.1:5099', 'http://127.0.0.1:5104']) {
    const response = await request.get(origin + path + '?status=Confirmed&page=1');
    expect(response.status()).toBe(200);
    expect(response.headers()['cache-control']).toBe('no-store');
    expect((await request.get(origin + path + '/' + id)).status()).toBe(404);
    expect((await request.get(origin + path + '?status=unknown')).status()).toBe(400);
    expect((await request.get(origin + path + '?page=0')).status()).toBe(400);
    expect((await request.get(origin + path + '?number=invalid')).status()).toBe(400);
  }
});
