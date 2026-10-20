import { test, expect } from '@playwright/test';
import { adminWrite, loginAdmin } from './support/admin-session';
const origin = 'http://localhost:4201';
const root = '/api/v1/admin/purchasing';

test('purchase orders preserve negotiated terms, reject stale edits and lock sent orders', async ({
  page,
}) => {
  const key = Date.now().toString();
  await loginAdmin(page.request, origin);
  const write = (path: string, data: object = {}, method = 'POST') =>
    adminWrite(page.request, origin, path, data, method);
  const product = await write('/api/v1/admin/products', {
    name: 'Comanda test ' + key,
    slug: 'order-' + key,
    category: 'Verificare automată',
    description: 'Produs demonstrativ.',
  });
  expect(product.ok()).toBeTruthy();
  const productId = (await product.json()).id;
  const variant = await write(`/api/v1/admin/products/${productId}/variants`, {
    sku: 'ORDER-' + key,
    label: 'M6',
    diameterMm: 6,
    lengthMm: 20,
    material: 'Oțel',
    finish: 'Zincat',
  });
  const variantId = (await variant.json()).id;
  const supplier = await write(root + '/suppliers', {
    name: 'Comenzi test ' + key,
    countryCode: 'RO',
    taxId: key,
    contactName: '',
    email: '',
    phone: '',
    address: '',
    notes: '',
  });
  const supplierId = (await supplier.json()).id;
  let orderId = '';
  try {
    const item = await write(root + `/suppliers/${supplierId}/items`, {
      variantId,
      supplierCode: 'EXT',
      packLabel: 'Cutie',
      unitsPerPack: 100,
      netCostPerPack: 12.5,
      currency: 'RON',
    });
    const itemId = (await item.json()).id;
    await page.goto(origin + '/aprovizionare');
    const create = page.locator('sd-order-create');
    await create.getByLabel('Caută furnizor activ').fill(key);
    await create.getByRole('button', { name: 'Caută furnizor', exact: true }).click();
    await expect(create.getByRole('combobox', { name: 'Furnizor', exact: true })).toContainText(
      key,
    );
    await create.getByRole('combobox', { name: 'Furnizor', exact: true }).selectOption(supplierId);
    const response = page.waitForResponse(
      (r) => r.url().endsWith(root + '/orders') && r.request().method() === 'POST',
    );
    await create.getByRole('button', { name: 'Creează ciorna' }).click();
    orderId = (await (await response).json()).id;
    const add = page.locator('sd-order-line-add');
    await add.getByRole('combobox', { name: 'Articol furnizor', exact: true }).selectOption(itemId);
    await add.getByLabel('Număr de ambalaje').fill('2');
    await add.getByRole('button', { name: 'Adaugă în comandă' }).click();
    await expect(page.getByRole('status')).toContainText('Articolul a fost adăugat');
    await page.getByRole('button', { name: 'Editează linia ORDER-' + key }).click();
    const editor = page.locator('sd-order-line-editor');
    await editor.getByLabel('Cost net negociat per ambalaj').fill('11.25');
    await editor.getByRole('button', { name: 'Salvează linia' }).click();
    await expect(page.getByRole('status')).toContainText('Linia a fost actualizată');
    const getOrder = async () =>
      (await page.request.get(origin + root + '/orders/' + orderId)).json();
    let order = await getOrder();
    expect(order.netTotal).toBe(22.5);
    expect(order.lines[0].baseUnits).toBe(200);
    expect(
      (
        await write(
          root + `/suppliers/${supplierId}/items/${itemId}`,
          {
            supplierCode: 'NEW',
            packLabel: 'Pungă',
            unitsPerPack: 10,
            netCostPerPack: 99,
            currency: 'EUR',
            isActive: true,
          },
          'PUT',
        )
      ).ok(),
    ).toBeTruthy();
    expect(
      (
        await write(
          root + '/orders/' + orderId,
          { version: order.version, notes: 'Altă sesiune' },
          'PUT',
        )
      ).ok(),
    ).toBeTruthy();
    const notes = page.locator('sd-order-notes');
    await notes.getByRole('button').click();
    await expect(page.getByRole('alert').first()).toContainText('modificată');
    await page.getByRole('button', { name: 'Reîncarcă comanda' }).click();
    await expect(notes.getByRole('textbox')).toHaveValue('Altă sesiune');
    await expect(page.locator('sd-order-actions input[type=checkbox]')).toBeEnabled();
    await page.locator('sd-order-actions input[type=checkbox]').check();
    await page.getByRole('button', { name: 'Marchează ca trimisă' }).click();
    await expect(page.getByRole('status')).toContainText('marcată manual ca trimisă');
    order = await getOrder();
    expect(order.status).toBe('Sent');
    expect(order.netTotal).toBe(22.5);
    expect(order.lines[0].unitsPerPack).toBe(100);
    expect(
      (
        await write(
          root + '/orders/' + orderId,
          { version: order.version, notes: 'Forbidden' },
          'PUT',
        )
      ).status(),
    ).toBe(409);
    await page.setViewportSize({ width: 1440, height: 1000 });
    await page.screenshot({ path: '../work/orders-desktop.png', fullPage: true });
    await page.setViewportSize({ width: 390, height: 844 });
    expect(
      await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
    ).toBeTruthy();
    await page.screenshot({ path: '../work/orders-mobile.png', fullPage: true });
    await page.getByRole('button', { name: 'Înregistrează anularea' }).click();
    await page.getByLabel('Motivul anulării').fill('Test încheiat');
    await page.getByRole('button', { name: 'Confirmă anularea' }).click();
    await expect(page.getByRole('status')).toContainText('Anularea a fost înregistrată');
    const savedProduct = await (
      await page.request.get(origin + '/api/v1/admin/products/' + productId)
    ).json();
    expect(savedProduct.stock).toEqual([]);
    expect(savedProduct.prices).toEqual([]);
  } finally {
    test.setTimeout(test.info().timeout + 15000);
    if (orderId) {
      const order = await (await page.request.get(origin + root + '/orders/' + orderId)).json();
      if (order.status !== 'Cancelled')
        await write(root + '/orders/' + orderId + '/cancel', {
          version: order.version,
          reason: 'Test cleanup',
        });
    }
    const saved = await (await page.request.get(origin + root + '/suppliers/' + supplierId)).json();
    await write(root + '/suppliers/' + supplierId, { ...saved, isActive: false }, 'PUT');
    await write('/api/v1/admin/products/' + productId + '/archive');
  }
});

test('order mutations require authentication and CSRF at gateway and service', async ({
  request,
}) => {
  const id = '00000000-0000-0000-0000-000000000001';
  for (const status of [401, 400]) {
    if (status === 400) await loginAdmin(request, 'http://127.0.0.1:5099');
    for (const port of [5099, 5105]) {
      const base = `http://127.0.0.1:${port}${root}/orders`;
      for (const path of [
        '',
        `/${id}/lines`,
        `/${id}/lines/${id}/remove`,
        `/${id}/sent`,
        `/${id}/cancel`,
      ])
        expect((await request.post(base + path, { data: {} })).status()).toBe(status);
      for (const path of [`/${id}`, `/${id}/lines/${id}`])
        expect((await request.put(base + path, { data: {} })).status()).toBe(status);
      if (status === 401) expect((await request.get(base)).status()).toBe(401);
    }
  }
});
