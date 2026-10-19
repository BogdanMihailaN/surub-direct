import { test, expect } from '@playwright/test';
import { adminWrite, loginAdmin } from './support/admin-session';

const admin = 'http://localhost:4201';

test('category inheritance and typed attributes work from administration to storefront', async ({
  page,
}) => {
  const token = Date.now().toString();
  const parent = 'Familie ' + token;
  const child = 'Subcategorie ' + token;
  const attribute = 'Pas test ' + token;
  const sku = 'TAX-' + token;
  const errors: string[] = [];
  page.on('pageerror', (error) => errors.push(error.message));
  await loginAdmin(page.request, admin);
  await page.goto(admin + '/categorii');
  await expect(
    page.getByRole('heading', { name: 'Categorii și atribute tehnice', exact: true }),
  ).toBeVisible();
  await page.getByLabel('Denumire atribut').fill(attribute);
  await page.getByLabel('Cod atribut').fill('pitch-' + token);
  await page.getByLabel('Tip atribut').selectOption('Number');
  await page.getByLabel('Unitate de măsură').fill('mm');
  await page.getByRole('button', { name: 'Salvează atributul' }).click();
  await expect(page.getByText('Atributul a fost salvat.', { exact: true })).toBeVisible();
  await page.getByLabel('Denumire categorie').fill(parent);
  await page.getByLabel('Slug categorie').fill('family-' + token);
  await page.getByRole('checkbox', { name: attribute + ' (mm)', exact: true }).check();
  await page.getByRole('checkbox', { name: 'Diametru (mm)', exact: true }).check();
  await page.getByRole('button', { name: 'Salvează categoria' }).click();
  await expect(page.getByText('Categoria a fost salvată.', { exact: true })).toBeVisible();
  await page.getByRole('button', { name: '+ Categorie', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Categorie nouă', exact: true })).toBeVisible();
  await expect(page.getByLabel('Denumire categorie')).toHaveValue('');
  await page.getByLabel('Denumire categorie').fill(child);
  await page.getByLabel('Slug categorie').fill('child-' + token);
  await page.getByLabel('Categoria părinte').selectOption({ label: parent });
  await page.getByRole('button', { name: 'Salvează categoria' }).click();
  await expect(page.getByText('Categoria a fost salvată.', { exact: true })).toBeVisible();
  await expect(
    page.locator('sd-category-form').getByText('Moștenite:', { exact: false }),
  ).toContainText(attribute);
  await page.getByLabel('Denumire categorie').fill(child + ' editată');
  await page.getByRole('button', { name: 'Salvează categoria' }).click();
  await expect(
    page.getByRole('button', { name: parent + ' / ' + child + ' editată', exact: true }),
  ).toBeVisible();
  await page.setViewportSize({ width: 390, height: 844 });
  expect(
    await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
  ).toBeTruthy();
  await page.screenshot({ path: '../work/taxonomy-admin-mobile.png', fullPage: true });
  await page.setViewportSize({ width: 1440, height: 1000 });
  await page.screenshot({ path: '../work/taxonomy-admin-desktop.png', fullPage: true });

  const taxonomy = await (await page.request.get(admin + '/api/v1/admin/taxonomy')).json();
  const category = taxonomy.categories.find((c: { slug: string }) => c.slug === 'child-' + token);
  const definition = taxonomy.attributes.find((a: { code: string }) => a.code === 'pitch-' + token);
  const created = await adminWrite(page.request, admin, '/api/v1/admin/products', {
    name: 'Produs taxonomie ' + token,
    slug: 'taxonomy-' + token,
    categoryId: category.id,
    category: category.name,
    description: 'Produs demonstrativ pentru testul atributelor.',
  });
  expect(created.ok()).toBeTruthy();
  const productPath = '/api/v1/admin/products/' + (await created.json()).id;
  try {
    const variant = await adminWrite(page.request, admin, productPath + '/variants', {
      sku,
      label: 'M8',
      diameterMm: 8,
      lengthMm: 40,
      material: 'Oțel',
      finish: 'Zincat',
    });
    expect(variant.ok()).toBeTruthy();
    const variantPath = productPath + '/variants/' + (await variant.json()).id;
    const offer = await adminWrite(page.request, admin, variantPath + '/offers', {
      code: sku + '-BOX',
      label: 'Cutie',
      unitsPerPack: 100,
      minPacks: 1,
      packStep: 1,
    });
    expect(offer.ok()).toBeTruthy();
    expect(
      (
        await adminWrite(
          page.request,
          admin,
          '/api/v1/admin/offers/' + (await offer.json()).id + '/price',
          { netAmount: 40, taxRate: 0.21 },
          'PUT',
        )
      ).ok(),
    ).toBeTruthy();
    await page.goto(admin);
    await page.getByLabel('Caută produs sau SKU').fill(sku);
    await page.getByRole('button', { name: 'Aplică filtrele' }).click();
    await expect(page.locator('.product-list button')).toHaveCount(1);
    await page.locator('.product-list button').click();
    await page.getByRole('button', { name: 'Atribute tehnice ' + sku, exact: true }).click();
    await page.getByLabel(attribute + ' (mm)', { exact: true }).fill('1.25');
    await page.getByRole('button', { name: 'Salvează atributele' }).click();
    await expect(page.locator('sd-variant-attributes-form')).toHaveCount(0);
    expect(
      (await adminWrite(page.request, admin, variantPath + '/attributes', [null], 'PUT')).status(),
    ).toBe(400);
    expect(
      (
        await adminWrite(
          page.request,
          admin,
          variantPath + '/attributes',
          [{ attributeId: definition.id, textValue: 'wrong type' }],
          'PUT',
        )
      ).status(),
    ).toBe(400);
    expect((await adminWrite(page.request, admin, productPath + '/publish')).ok()).toBeTruthy();
    await page.goto('http://localhost:4200');
    await page.getByRole('button', { name: child + ' editată', exact: true }).click();
    await expect(page.getByLabel(attribute + ' minim')).toBeVisible();
    await page.getByLabel(attribute + ' minim').fill('1');
    await page.getByLabel(attribute + ' maxim').fill('1.5');
    await page.getByLabel('Diametru (mm)').fill('8');
    await page.getByRole('button', { name: 'Aplică filtrele' }).click();
    await expect(page).toHaveURL(/atribute=/);
    await expect(page.locator('.product-card')).toHaveCount(1);
    await page.reload();
    await expect(page.getByLabel(attribute + ' minim')).toHaveValue('1');
    await expect(page.locator('.product-card')).toHaveCount(1);
    await page.screenshot({ path: '../work/taxonomy-storefront.png', fullPage: true });
    await page.getByLabel(attribute + ' minim').fill('2');
    await page.getByLabel(attribute + ' maxim').fill('3');
    await page.getByRole('button', { name: 'Aplică filtrele' }).click();
    await expect(page.getByText('Nu am găsit produse pentru aceste filtre.')).toBeVisible();
    await page.goto('http://localhost:4200/produse/taxonomy-' + token);
    await expect(page.getByText(attribute, { exact: false })).toContainText('1,25');
    expect(errors).toEqual([]);
  } finally {
    expect((await adminWrite(page.request, admin, productPath + '/archive')).ok()).toBeTruthy();
  }
});

test('taxonomy mutations require session and CSRF at gateway and direct service', async ({
  request,
}) => {
  const id = '00000000-0000-0000-0000-000000000001';
  const routes = [
    {
      path: '/api/v1/admin/taxonomy/categories',
      method: 'POST',
      data: { name: 'X', slug: 'x', parentId: null, sortOrder: 0, attributeIds: [] },
    },
    {
      path: '/api/v1/admin/taxonomy/categories/' + id,
      method: 'PUT',
      data: { name: 'X', parentId: null, sortOrder: 0, attributeIds: [] },
    },
    {
      path: '/api/v1/admin/taxonomy/attributes',
      method: 'POST',
      data: { code: 'x', name: 'X', kind: 'Text', unit: null, sortOrder: 0 },
    },
    {
      path: '/api/v1/admin/taxonomy/attributes/' + id,
      method: 'PUT',
      data: { name: 'X', sortOrder: 0 },
    },
    {
      path: '/api/v1/admin/products/' + id + '/variants/' + id + '/attributes',
      method: 'PUT',
      data: [],
    },
  ];
  for (const status of [401, 400]) {
    if (status === 400) await loginAdmin(request, 'http://127.0.0.1:5099');
    for (const port of [5099, 5101]) {
      for (const route of routes) {
        expect(
          (
            await request.fetch(`http://127.0.0.1:${port}` + route.path, {
              method: route.method,
              data: route.data,
            })
          ).status(),
        ).toBe(status);
      }
    }
  }
  expect(
    (
      await request.get('http://127.0.0.1:5099/api/v1/products', {
        params: { attributes: '[null]' },
      })
    ).status(),
  ).toBe(400);
});
