import { test, expect } from '@playwright/test';
import { adminWrite, loginAdmin } from './support/admin-session';
const shop = 'http://localhost:4200';
const admin = 'http://localhost:4201';
const cartPath = '/api/v1/ordering/cart';
const person = {
  customerType: 'person',
  fullName: 'Client Demo',
  email: 'client@example.test',
  phone: '+40 700 000 000',
  companyName: null,
  taxId: null,
  registrationNumber: null,
  shippingAddress: {
    street: 'Strada Exemplu 1',
    city: 'București',
    county: 'Sector 1',
    postalCode: '010101',
    country: 'RO',
  },
  billingSameAsShipping: true,
  billingAddress: null,
};
test('demo confirmation recovers a lost response, consumes stock once and retains history after clearing cart', async ({
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
    await page.route('**/api/v1/ordering/cart/preparation/*/confirm', async (route) => {
      expect((await route.fetch()).status()).toBe(200);
      await route.abort('failed');
    });
    await prep.getByRole('button', { name: 'Confirmă comanda demonstrativă' }).click();
    await expect(page.getByRole('alert')).toContainText('Nu putem confirma');
    await page.unroute('**/api/v1/ordering/cart/preparation/*/confirm');
    await page.getByRole('button', { name: 'Reîncarcă coșul', exact: true }).click();
    await expect(
      prep.getByText('Comanda demonstrativă este confirmată.', { exact: true }),
    ).toBeVisible({ timeout: 20000 });
    const history = page.getByRole('region', { name: 'Comenzi demonstrative', exact: true });
    await history.getByRole('button', { name: 'Actualizează comenzile' }).click();
    await expect(history.getByRole('status')).toHaveText('Confirmată');
    const orders = await (await page.request.get(shop + '/api/v1/ordering/demo-orders')).json();
    expect(orders.total).toBe(1);
    expect(orders.items[0]).toMatchObject({
      id: prepared.id,
      status: 'Confirmed',
      grossTotal: 42.35,
    });
    expect(orders.items[0].number).toMatch(/^DEMO-\d+$/);
    const stock = async () => (await (await page.request.get(admin + productPath)).json()).stock[0];
    expect(await stock()).toMatchObject({ onHand: 65, reserved: 0, available: 65 });
    const retry = await page.request.post(
      shop + cartPath + '/preparation/' + prepared.id + '/confirm',
      { headers },
    );
    expect(retry.status()).toBe(200);
    expect((await retry.json()).number).toBe(orders.items[0].number);
    expect(
      (
        await page.request.post(shop + cartPath + '/preparation/' + prepared.id + '/cancel', {
          headers,
        })
      ).status(),
    ).toBe(409);
    await page.setViewportSize({ width: 390, height: 844 });
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(
      true,
    );
    await prep.screenshot({ path: '../work/demo-order-confirmed-mobile.png' });
    await prep.getByRole('button', { name: 'Golește coșul confirmat' }).click();
    await expect(page.getByText('Coșul este gol.', { exact: false })).toBeVisible();
    await page.reload();
    await expect(history.getByRole('heading', { name: orders.items[0].number })).toBeVisible();
    await history.getByText('Produse și date salvate').click();
    await expect(history).toContainText(person.email);
    await history.screenshot({ path: '../work/demo-order-history-mobile.png' });
    expect(
      (await (await page.request.get(shop + '/api/v1/ordering/demo-orders')).json()).total,
    ).toBe(1);
    expect(await stock()).toMatchObject({ onHand: 65, reserved: 0 });
    expect(
      (await (await page.request.get(shop + '/api/v1/ordering/capabilities')).json())
        .checkoutEnabled,
    ).toBe(false);
    expect((await page.request.post(shop + '/api/v1/ordering/orders', { data: {} })).status()).toBe(
      404,
    );
  } finally {
    const response = await page.request.get(shop + cartPath);
    const cart = response.status() === 200 ? await response.json() : null;
    if (cart?.preparedId)
      await page.request.post(shop + cartPath + '/preparation/' + cart.preparedId + '/cancel', {
        headers,
      });
    await write(productPath + '/archive');
  }
});
test('demo orders enforce CSRF and owner isolation through gateway and direct API', async ({
  playwright,
}) => {
  for (const origin of [shop, 'http://127.0.0.1:5104']) {
    const api = await playwright.request.newContext();
    try {
      expect((await (await api.get(origin + '/api/v1/ordering/demo-orders')).json()).items).toEqual(
        [],
      );
      const token = (await (await api.get(origin + cartPath + '/csrf')).json()).token;
      const headers = { 'X-CART-CSRF': token };
      const id = crypto.randomUUID();
      const path = origin + cartPath + '/preparation/' + id;
      expect((await api.post(path + '/confirm', { headers })).status()).toBe(401);
      expect((await api.post(origin + cartPath + '/session', { headers })).status()).toBe(200);
      for (const action of ['confirm', 'complete'])
        expect((await api.post(path + '/' + action)).status()).toBe(400);
      expect((await api.post(path + '/confirm', { headers })).status()).toBe(409);
      expect((await api.post(path + '/complete', { headers })).status()).toBe(404);
      const response = await api.get(origin + '/api/v1/ordering/demo-orders');
      expect(response.headers()['cache-control']).toBe('no-store');
      expect((await response.json()).items).toEqual([]);
      expect((await api.get(origin + '/api/v1/ordering/demo-orders?page=0')).status()).toBe(400);
    } finally {
      await api.dispose();
    }
  }
});
