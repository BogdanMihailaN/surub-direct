import { test, expect } from '@playwright/test';
import { adminWrite, loginAdmin } from './support/admin-session';
const admin = 'http://localhost:4201';
const shop = 'http://localhost:4200';
test('cart persists quantities, recalculates prices and stock, and never enables checkout', async ({
  page,
}) => {
  const key = Date.now().toString();
  const slug = 'cart-' + key;
  await loginAdmin(page.request, admin);
  const write = (path: string, data: object = {}, method = 'POST') =>
    adminWrite(page.request, admin, path, data, method);
  const create = async (path: string, data: object) => {
    const response = await write(path, data);
    expect(response.ok()).toBeTruthy();
    return (await response.json()).id as string;
  };
  const productId = await create('/api/v1/admin/products', {
    name: 'Coș demonstrativ ' + key,
    slug,
    category: 'Verificare automată',
    description: 'Produs pentru verificarea coșului.',
  });
  const path = '/api/v1/admin/products/' + productId;
  try {
    const variantId = await create(path + '/variants', {
      sku: 'CART-' + key,
      label: 'M6',
      diameterMm: 6,
      lengthMm: 20,
      material: 'Oțel',
      finish: 'Zincat',
    });
    const offerId = await create(path + '/variants/' + variantId + '/offers', {
      code: 'CART-' + key + '-100',
      label: 'Cutie 100',
      unitsPerPack: 100,
      minPacks: 2,
      packStep: 2,
    });
    const small = await create(path + '/variants/' + variantId + '/offers', {
      code: 'CART-' + key + '-10',
      label: 'Pungă 10',
      unitsPerPack: 10,
      minPacks: 1,
      packStep: 1,
    });
    for (const id of [offerId, small])
      expect(
        (
          await write(
            '/api/v1/admin/offers/' + id + '/price',
            { netAmount: 40, taxRate: 0.21 },
            'PUT',
          )
        ).ok(),
      ).toBeTruthy();
    expect(
      (
        await write('/api/v1/admin/variants/' + variantId + '/receipts', {
          requestId: crypto.randomUUID(),
          quantity: 300,
          reference: 'CART-' + key,
        })
      ).ok(),
    ).toBeTruthy();
    expect((await write(path + '/publish')).ok()).toBeTruthy();
    await page.goto(shop + '/produse/' + slug);
    const add = page.locator('tr').filter({ hasText: 'Cutie 100' }).locator('sd-add-to-cart');
    await expect(add.getByLabel('Număr ambalaje')).toHaveValue('2');
    await add.getByRole('button', { name: 'Adaugă în coș' }).click();
    await page.getByRole('link', { name: 'Coș (1)', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Total cu TVA: 96,80 RON' })).toBeVisible();
    await page.reload();
    await expect(page.locator('sd-cart-line input')).toHaveValue('2');
    expect(
      await page.evaluate(() => JSON.parse(localStorage.getItem('surub-direct.cart.v1')!)),
    ).toEqual([{ productSlug: slug, offerId, packs: 2 }]);
    const quotePath = '/api/v1/ordering/cart/quote';
    const quote = async (lines: object[]) =>
      (await page.request.post(shop + quotePath, { data: { lines } })).json();
    const combined = await quote([
      { productSlug: slug, offerId, packs: 2 },
      { productSlug: slug, offerId: small, packs: 11 },
    ]);
    expect(combined.grossTotal).toBeNull();
    expect(
      combined.lines.every((l: { issue: string }) => l.issue.includes('Stoc insuficient')),
    ).toBeTruthy();
    expect((await quote([{ productSlug: slug, offerId, packs: 3 }])).grossTotal).toBeNull();
    expect(
      (
        await write(
          '/api/v1/admin/offers/' + offerId + '/price',
          { netAmount: 10, taxRate: 0.21 },
          'PUT',
        )
      ).ok(),
    ).toBeTruthy();
    await page.getByRole('button', { name: 'Recalculează coșul' }).click();
    await expect(page.getByRole('heading', { name: 'Total cu TVA: 24,20 RON' })).toBeVisible();
    await page.route(
      '**' + quotePath,
      (route) =>
        route.fulfill({
          status: 503,
          contentType: 'application/problem+json',
          body: JSON.stringify({ detail: 'Verificare indisponibilă temporar.' }),
        }),
      { times: 1 },
    );
    await page.getByRole('button', { name: 'Recalculează coșul' }).click();
    await expect(page.getByRole('alert')).toContainText('Verificare indisponibilă');
    await expect(page.getByRole('heading', { name: /Total cu TVA/ })).toHaveCount(0);
    await page.getByRole('button', { name: 'Recalculează coșul' }).click();
    await expect(page.getByRole('heading', { name: 'Total cu TVA: 24,20 RON' })).toBeVisible();
    await page.setViewportSize({ width: 1440, height: 900 });
    await page.screenshot({ path: '../work/cart-desktop.png', fullPage: true });
    await page.setViewportSize({ width: 390, height: 844 });
    expect(
      await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
    ).toBeTruthy();
    await expect(page.getByRole('link', { name: 'Coș (1)', exact: true })).toBeVisible();
    await page.screenshot({ path: '../work/cart-mobile.png', fullPage: true });
    expect(
      (await (await page.request.get(shop + '/api/v1/ordering/capabilities')).json())
        .checkoutEnabled,
    ).toBe(false);
    expect((await page.request.post(shop + '/api/v1/ordering/orders', { data: {} })).status()).toBe(
      404,
    );
    expect((await write(path + '/archive')).ok()).toBeTruthy();
    await page.getByRole('button', { name: 'Recalculează coșul' }).click();
    await expect(page.getByRole('alert')).toContainText('nu mai este publicat');
    await expect(page.getByRole('heading', { name: /Total cu TVA/ })).toHaveCount(0);
    await page.getByRole('button', { name: 'Elimină', exact: true }).click();
    await expect(page.getByText('Coșul este gol.', { exact: false })).toBeVisible();
    const data = await (await page.request.get(admin + path)).json();
    expect(data.stock[0].onHand).toBe(300);
    expect(data.stock[0].reserved).toBe(0);
  } finally {
    await write(path + '/archive');
  }
});
test('corrupted local cart is recoverable and invalid public quote payloads are rejected', async ({
  page,
}) => {
  await page.addInitScript(() => localStorage.setItem('surub-direct.cart.v1', 'not-json'));
  await page.goto(shop + '/cos');
  await expect(page.getByRole('alert')).toContainText('Coșul salvat nu poate fi citit');
  for (const origin of [shop, 'http://127.0.0.1:5104']) {
    expect(
      (
        await page.request.post(origin + '/api/v1/ordering/cart/quote', {
          data: { lines: [{ productSlug: '../internal', offerId: crypto.randomUUID(), packs: 1 }] },
        })
      ).status(),
    ).toBe(400);
    const empty = await page.request.post(origin + '/api/v1/ordering/cart/quote', {
      data: { lines: [] },
    });
    expect(empty.status()).toBe(200);
    expect((await empty.json()).grossTotal).toBeNull();
  }
});
