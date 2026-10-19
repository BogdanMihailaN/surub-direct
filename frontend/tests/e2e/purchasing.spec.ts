import { test, expect } from '@playwright/test';
import { adminWrite, loginAdmin } from './support/admin-session';
const origin = 'http://localhost:4201';
const root = '/api/v1/admin/purchasing';

test('suppliers and SKU purchasing terms can be managed without changing selling prices or stock', async ({
  page,
}) => {
  const key = Date.now().toString();
  const sku = 'BUY-' + key;
  const errors: string[] = [];
  page.on('pageerror', (e) => errors.push(e.message));
  await loginAdmin(page.request, origin);
  const productResponse = await adminWrite(page.request, origin, '/api/v1/admin/products', {
    name: 'Aprovizionare test ' + key,
    slug: 'purchasing-' + key,
    category: 'Verificare automată',
    description: 'Produs demonstrativ pentru testarea aprovizionării.',
  });
  expect(productResponse.ok()).toBeTruthy();
  const productId = (await productResponse.json()).id;
  const variantResponse = await adminWrite(
    page.request,
    origin,
    `/api/v1/admin/products/${productId}/variants`,
    { sku, label: 'M6 × 20', diameterMm: 6, lengthMm: 20, material: 'Oțel', finish: 'Zincat' },
  );
  expect(variantResponse.ok()).toBeTruthy();
  const variantId = (await variantResponse.json()).id;
  const supplierIds: string[] = [];
  try {
    await page.goto(origin + '/furnizori');
    await expect(page.getByRole('heading', { name: 'Furnizori și articole' })).toBeVisible();
    const form = page.locator('sd-supplier-form');
    await form.getByLabel('Denumire firmă').fill('Furnizor test ' + key);
    await form.getByLabel('Cod fiscal (CUI pentru România)').fill('RO ' + key);
    await form.getByLabel('Persoană de contact').fill('Contact test');
    await form.getByLabel('Email', { exact: true }).fill('test@example.com');
    const created = page.waitForResponse(
      (r) => r.url().endsWith(root + '/suppliers') && r.request().method() === 'POST',
    );
    await form.getByRole('button', { name: 'Salvează furnizorul' }).click();
    const result = await created;
    expect(result.ok()).toBeTruthy();
    const id = (await result.json()).id;
    supplierIds.push(id);
    await expect(
      page.getByRole('status').filter({ hasText: 'Furnizorul a fost salvat.' }),
    ).toBeVisible();
    await expect(form.getByLabel('Cod fiscal (CUI pentru România)')).toHaveValue(key);
    const picker = page.locator('sd-purchasing-variant-picker');
    await picker.getByLabel('Caută SKU sau denumire').fill(sku);
    await page.route('**/api/v1/admin/purchasing/variants?*', (route) =>
      route.fulfill({
        status: 503,
        contentType: 'application/problem+json',
        body: JSON.stringify({ detail: 'Catalogul nu poate fi verificat momentan.' }),
      }),
    );
    await picker.getByRole('button', { name: 'Caută în Catalog' }).click();
    await expect(page.getByRole('alert')).toContainText('Catalogul nu poate fi verificat');
    await expect(picker.getByText(/Niciun SKU găsit/)).toHaveCount(0);
    await page.unroute('**/api/v1/admin/purchasing/variants?*');
    await picker.getByRole('button', { name: 'Caută în Catalog' }).click();
    await picker.getByRole('button', { name: new RegExp(sku) }).click();
    const itemForm = page.locator('sd-supplier-item-form');
    await itemForm.getByLabel('Cod articol la furnizor').fill('EXT-' + key);
    await itemForm.getByLabel('Denumire ambalaj achiziție').fill('Cutie mare');
    await itemForm.getByLabel('Bucăți per ambalaj achiziție').fill('100');
    await itemForm.getByLabel('Cost net per ambalaj').fill('12.50');
    await itemForm.getByRole('combobox', { name: 'Monedă', exact: true }).selectOption('EUR');
    await itemForm.getByRole('button', { name: 'Salvează asocierea' }).click();
    await expect(page.getByText('Asocierea SKU a fost salvată.', { exact: true })).toBeVisible();
    const items = page.locator('sd-supplier-items');
    await expect(items).toContainText('12,50 EUR');
    await items.getByRole('button', { name: 'Editează ' + sku }).click();
    await itemForm.getByLabel('Cost net per ambalaj').fill('15');
    await itemForm.getByLabel('Bucăți per ambalaj achiziție').fill('50');
    await itemForm.getByRole('button', { name: 'Salvează asocierea' }).click();
    await expect(items).toContainText('15,00 EUR');
    await expect(items).toContainText('50 bucăți');
    const itemBody = {
      variantId,
      supplierCode: 'EXT-' + key,
      packLabel: 'Cutie',
      unitsPerPack: 10,
      netCostPerPack: 10,
      currency: 'RON',
    };
    expect(
      (await adminWrite(page.request, origin, root + `/suppliers/${id}/items`, itemBody)).status(),
    ).toBe(409);
    const secondResponse = await adminWrite(page.request, origin, root + '/suppliers', {
      name: 'Alt furnizor ' + key,
      countryCode: 'RO',
      taxId: 'ALT-' + key,
      contactName: '',
      email: '',
      phone: '',
      address: '',
      notes: '',
    });
    expect(secondResponse.ok()).toBeTruthy();
    const secondId = (await secondResponse.json()).id;
    supplierIds.push(secondId);
    expect(
      (
        await adminWrite(page.request, origin, root + `/suppliers/${secondId}/items`, itemBody)
      ).ok(),
    ).toBeTruthy();
    let supplier = await (await page.request.get(origin + root + '/suppliers/' + id)).json();
    expect(
      (
        await adminWrite(
          page.request,
          origin,
          root + `/suppliers/${secondId}/items/${supplier.items[0].id}`,
          { ...itemBody, isActive: true },
          'PUT',
        )
      ).status(),
    ).toBe(404);
    await form.getByLabel('Furnizor activ', { exact: true }).uncheck();
    await form.getByRole('button', { name: 'Salvează furnizorul' }).click();
    await expect(
      page.getByText('Furnizor inactiv. Asocierile sunt păstrate;', { exact: false }),
    ).toBeVisible();
    await expect(items.getByRole('button', { name: 'Editează ' + sku })).toBeDisabled();
    expect(
      (
        await adminWrite(page.request, origin, root + `/suppliers/${id}/items`, {
          ...itemBody,
          supplierCode: 'OTHER',
        })
      ).status(),
    ).toBe(409);
    await form.getByLabel('Furnizor activ', { exact: true }).check();
    await form.getByRole('button', { name: 'Salvează furnizorul' }).click();
    await expect(items.getByRole('button', { name: 'Editează ' + sku })).toBeEnabled();
    await page.setViewportSize({ width: 1440, height: 1000 });
    await page.screenshot({ path: '../work/purchasing-desktop.png', fullPage: true });
    await page.setViewportSize({ width: 390, height: 844 });
    expect(
      await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
    ).toBeTruthy();
    await page.screenshot({ path: '../work/purchasing-mobile.png', fullPage: true });
    supplier = await (await page.request.get(origin + root + '/suppliers/' + id)).json();
    expect(supplier.items[0]).toMatchObject({
      variantId,
      sku,
      unitsPerPack: 50,
      netCostPerPack: 15,
      currency: 'EUR',
    });
    const product = await (
      await page.request.get(origin + '/api/v1/admin/products/' + productId)
    ).json();
    expect(product.prices).toEqual([]);
    expect(product.stock).toEqual([]);
    expect(product.product.status).toBe('Draft');
    expect(errors).toEqual([]);
  } finally {
    test.setTimeout(test.info().timeout + 15000);
    for (const id of supplierIds) {
      const supplier = await (await page.request.get(origin + root + '/suppliers/' + id)).json();
      await adminWrite(
        page.request,
        origin,
        root + '/suppliers/' + id,
        { ...supplier, isActive: false },
        'PUT',
      );
    }
    await adminWrite(page.request, origin, '/api/v1/admin/products/' + productId + '/archive');
  }
});

test('Purchasing protects direct and gateway administration and Catalog keeps lookup routes internal', async ({
  request,
}) => {
  const id = '00000000-0000-0000-0000-000000000001';
  for (const status of [401, 400]) {
    if (status === 400) await loginAdmin(request, 'http://127.0.0.1:5099');
    for (const port of [5099, 5105]) {
      const base = 'http://127.0.0.1:' + port + root;
      expect((await request.post(base + '/suppliers', { data: {} })).status()).toBe(status);
      expect((await request.put(base + '/suppliers/' + id, { data: {} })).status()).toBe(status);
      expect((await request.post(base + `/suppliers/${id}/items`, { data: {} })).status()).toBe(
        status,
      );
      expect(
        (await request.put(base + `/suppliers/${id}/items/${id}`, { data: {} })).status(),
      ).toBe(status);
      if (status === 401) {
        expect((await request.get(base + '/suppliers')).status()).toBe(401);
        expect((await request.get(base + '/variants?search=SKU')).status()).toBe(401);
      }
    }
  }
  for (const path of [
    '/internal/purchasing-variants?search=SKU',
    '/internal/purchasing-variants/' + id,
  ]) {
    expect((await request.get('http://127.0.0.1:5099' + path)).status()).toBe(404);
    expect((await request.get('http://127.0.0.1:5101' + path)).status()).toBe(401);
  }
});
