import { test, expect } from '@playwright/test';
import { adminWrite, loginAdmin } from './support/admin-session';

const origin = 'http://localhost:4201';

test('variant and package edits preserve stock and price, and pack size changes require republication', async ({
  page,
}) => {
  const token = Date.now().toString();
  const sku = 'EDIT-' + token;
  const code = sku + '-PACK';
  const name = 'Editare variante ' + token;
  const errors: string[] = [];
  page.on('pageerror', (e) => errors.push(e.message));
  await loginAdmin(page.request, origin);
  const create = await adminWrite(page.request, origin, '/api/v1/admin/products', {
    name,
    slug: 'edit-' + token,
    category: 'Verificare automată',
    description: 'Test editare variante și ambalaje.',
  });
  expect(create.ok()).toBeTruthy();
  const { id } = await create.json();
  const productPath = '/api/v1/admin/products/' + id;
  const variantResponse = await adminWrite(page.request, origin, productPath + '/variants', {
    sku,
    label: 'M8 × 40 mm',
    diameterMm: 8,
    lengthMm: 40,
    material: 'Oțel',
    finish: 'Zincat',
  });
  expect(variantResponse.ok()).toBeTruthy();
  const { id: variantId } = await variantResponse.json();
  const variantPath = productPath + '/variants/' + variantId;
  const offerResponse = await adminWrite(page.request, origin, variantPath + '/offers', {
    code,
    label: 'Cutie 100 buc.',
    unitsPerPack: 100,
    minPacks: 1,
    packStep: 1,
  });
  expect(offerResponse.ok()).toBeTruthy();
  const { id: offerId } = await offerResponse.json();
  const offerPath = variantPath + '/offers/' + offerId;
  expect(
    (
      await adminWrite(
        page.request,
        origin,
        '/api/v1/admin/offers/' + offerId + '/price',
        { netAmount: 40, taxRate: 0.21 },
        'PUT',
      )
    ).ok(),
  ).toBeTruthy();
  expect(
    (
      await adminWrite(page.request, origin, '/api/v1/admin/variants/' + variantId + '/receipts', {
        requestId: crypto.randomUUID(),
        quantity: 250,
        reference: 'EDIT-' + token,
      })
    ).ok(),
  ).toBeTruthy();
  expect((await adminWrite(page.request, origin, productPath + '/publish')).ok()).toBeTruthy();
  const publicPath = origin + '/api/v1/products/edit-' + token;
  try {
    await page.goto(origin);
    await page.getByLabel('Caută produs sau SKU').fill(sku);
    await page.getByRole('button', { name: 'Aplică filtrele' }).click();
    await expect(page.locator('.product-list button')).toHaveCount(1);
    await page.locator('.product-list button').click();
    await page.getByRole('button', { name: 'Editează varianta ' + sku, exact: true }).click();
    const variantForm = page.locator('sd-variant-edit-form');
    await expect(variantForm.getByRole('textbox', { name: 'SKU', exact: true })).toHaveCount(0);
    await variantForm.getByLabel('Diametru (mm)').fill('0.0001');
    await expect(variantForm.getByRole('button', { name: 'Salvează varianta' })).toBeDisabled();
    await variantForm.getByRole('button', { name: 'Renunță la editare' }).click();
    await expect(page.getByText('M8 × 40 mm', { exact: true })).toBeVisible();
    await page.getByRole('button', { name: 'Editează varianta ' + sku, exact: true }).click();
    await variantForm.getByLabel('Denumire variantă').fill('M10 × 65 mm');
    await variantForm.getByLabel('Diametru (mm)').fill('10');
    await variantForm.getByLabel('Lungime (mm)').fill('65');
    await variantForm.getByLabel('Material').fill('Inox A2');
    await variantForm.getByLabel('Finisaj').fill('Natural');
    await variantForm.getByRole('button', { name: 'Salvează varianta' }).click();
    await expect(
      page.getByText('Varianta a fost actualizată. SKU-ul și stocul au fost păstrate.', {
        exact: true,
      }),
    ).toBeVisible();
    const updated = await (await page.request.get(publicPath)).json();
    expect(updated.product.variants[0]).toMatchObject({
      id: variantId,
      sku,
      label: 'M10 × 65 mm',
      diameterMm: 10,
      lengthMm: 65,
      material: 'Inox A2',
      finish: 'Natural',
    });
    // Invalid requests must be rejected by the API as well as the form.
    expect(
      (
        await adminWrite(
          page.request,
          origin,
          variantPath,
          {
            label: 'Invalid',
            diameterMm: 0.0001,
            lengthMm: 40,
            material: 'Oțel',
            finish: 'Zincat',
          },
          'PUT',
        )
      ).status(),
    ).toBe(400);
    expect(
      (
        await adminWrite(
          page.request,
          origin,
          offerPath,
          { label: 'Invalid', unitsPerPack: 1.5, minPacks: 1, packStep: 1 },
          'PUT',
        )
      ).status(),
    ).toBe(400);
    await page.getByRole('button', { name: 'Editează ambalajul ' + code, exact: true }).click();
    const offerForm = page.locator('sd-offer-edit-form');
    await offerForm.getByLabel('Bucăți / ambalaj').fill('50.5');
    await expect(offerForm.getByRole('button', { name: 'Salvează ambalajul' })).toBeDisabled();
    await offerForm.getByLabel('Denumire ambalaj').fill('Cutie 50 buc.');
    await offerForm.getByLabel('Bucăți / ambalaj').fill('50');
    await offerForm.getByLabel('Minim ambalaje').fill('2');
    await offerForm.getByLabel('Pas cantitate').fill('2');
    await expect(offerForm.getByRole('button', { name: 'Salvează ambalajul' })).toBeDisabled();
    await offerForm.getByRole('checkbox').check();
    await offerForm.getByRole('button', { name: 'Salvează ambalajul' }).click();
    await expect(
      page.getByText(
        'Ambalaj actualizat. Produsul este ciornă; verifică prețul pe ambalaj și republică.',
        { exact: true },
      ),
    ).toBeVisible();
    await expect(page.getByText('250 buc.', { exact: true })).toBeVisible();
    expect((await page.request.get(publicPath)).status()).toBe(404);
    const draft = await (await page.request.get(origin + productPath)).json();
    expect(draft.product.status).toBe('Draft');
    expect(draft.product.variants[0].offers[0]).toMatchObject({
      id: offerId,
      code,
      label: 'Cutie 50 buc.',
      unitsPerPack: 50,
      minPacks: 2,
      packStep: 2,
    });
    expect(draft.prices[0]).toMatchObject({ offerId, netAmount: 40 });
    expect(draft.stock[0]).toMatchObject({ variantId, available: 250 });
    await page.getByRole('button', { name: 'Publică produsul', exact: true }).click();
    await expect(page.getByText('Produs publicat în magazin.', { exact: true })).toBeVisible();
    await page.goto('http://localhost:4200/produse/edit-' + token);
    await expect(page.getByText('M10 × 65 mm', { exact: true })).toBeVisible();
    await expect(
      page.getByRole('cell', { name: 'Cutie 50 buc. Minim 2 · pas 2', exact: true }),
    ).toBeVisible();
    await expect(page.getByText('Minim 2 · pas 2', { exact: true })).toBeVisible();
    await expect(page.getByText('48,40 lei', { exact: true })).toBeVisible();
    await expect(page.getByText('5 ambalaje', { exact: true })).toBeVisible();
    expect(errors).toEqual([]);
  } finally {
    expect((await adminWrite(page.request, origin, productPath + '/archive')).ok()).toBeTruthy();
  }
});
