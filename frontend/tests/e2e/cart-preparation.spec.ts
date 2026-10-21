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
test('preparation recovers lost response, freezes prices and releases aggregated stock before editing', async ({
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
    await page.route('**/api/v1/ordering/cart/preparation', async (route) => {
      if (route.request().method() !== 'POST') {
        await route.continue();
        return;
      }
      expect((await route.fetch()).status()).toBe(200);
      await route.abort('failed');
    });
    await page.getByRole('button', { name: 'Pregătește și rezervă stocul' }).click();
    await expect(page.getByRole('alert')).toContainText('Nu putem confirma');
    await page.unroute('**/api/v1/ordering/cart/preparation');
    await page.getByRole('button', { name: 'Reîncarcă coșul', exact: true }).click();
    const preparation = page.getByRole('region', { name: 'Rezervare demonstrativă' });
    await expect(preparation.getByRole('status')).toHaveText('Stoc rezervat', { timeout: 20000 });
    await expect(preparation).toContainText('42,35 RON');
    const stock = async () => (await (await page.request.get(admin + productPath)).json()).stock[0];
    expect(await stock()).toMatchObject({ onHand: 100, reserved: 35, available: 65 });
    await expect(page.locator('sd-cart-line input').first()).toBeDisabled();
    await expect(page.getByRole('button', { name: 'Salvează ciorna' })).toBeDisabled();
    expect(
      (
        await write(
          '/api/v1/admin/offers/' + offers[0] + '/price',
          { netAmount: 100, taxRate: 0.21 },
          'PUT',
        )
      ).ok(),
    ).toBe(true);
    await page.getByRole('button', { name: 'Actualizează starea rezervării' }).click();
    await expect(preparation).toContainText('42,35 RON');
    await page.setViewportSize({ width: 390, height: 844 });
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(
      true,
    );
    await preparation.screenshot({ path: '../work/cart-preparation-mobile.png' });
    await preparation.getByRole('button', { name: 'Anulează rezervarea' }).click();
    await expect(preparation.getByRole('button', { name: 'Reia editarea coșului' })).toBeVisible({
      timeout: 20000,
    });
    await preparation.getByRole('button', { name: 'Reia editarea coșului' }).click();
    await expect(page.locator('sd-cart-line input').first()).toBeEnabled();
    await expect(page.getByRole('heading', { name: 'Total cu TVA: 260,15 RON' })).toBeVisible();
    expect(await stock()).toMatchObject({ onHand: 100, reserved: 0, available: 100 });
    expect((await page.request.get(shop + cartPath + '/preparation')).status()).toBe(204);
    expect(
      (await (await page.request.get(shop + '/api/v1/ordering/capabilities')).json())
        .checkoutEnabled,
    ).toBe(false);
    expect((await page.request.post(shop + '/api/v1/ordering/orders', { data: {} })).status()).toBe(
      404,
    );
  } finally {
    const cart = await (await page.request.get(shop + cartPath)).json();
    if (cart.preparedId)
      await page.request.post(shop + cartPath + '/preparation/' + cart.preparedId + '/cancel', {
        headers,
      });
    await write(productPath + '/archive');
  }
});
test('preparation requires ownership and CSRF on gateway and direct service', async ({
  playwright,
}) => {
  for (const origin of [shop, 'http://127.0.0.1:5104']) {
    const api = await playwright.request.newContext();
    try {
      const token = (await (await api.get(origin + cartPath + '/csrf')).json()).token;
      const headers = { 'X-CART-CSRF': token };
      const data = { requestId: crypto.randomUUID(), cartId: crypto.randomUUID(), version: 1 };
      expect((await api.post(origin + cartPath + '/preparation', { headers, data })).status()).toBe(
        401,
      );
      expect((await api.get(origin + cartPath + '/preparation')).status()).toBe(401);
      const cart = await (await api.post(origin + cartPath + '/session', { headers })).json();
      data.cartId = cart.id;
      expect((await api.post(origin + cartPath + '/preparation', { data })).status()).toBe(400);
      expect((await api.post(origin + cartPath + '/preparation', { headers, data })).status()).toBe(
        400,
      );
      expect(
        (await api.post(origin + cartPath + '/preparation/' + data.requestId + '/cancel')).status(),
      ).toBe(400);
      expect((await api.get(origin + cartPath + '/preparation')).status()).toBe(204);
    } finally {
      await api.dispose();
    }
  }
});
