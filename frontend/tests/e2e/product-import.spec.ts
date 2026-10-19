import { test, expect } from '@playwright/test';
import { adminWrite, loginAdmin } from './support/admin-session';
const origin = 'http://localhost:4201';
const path = '/api/v1/admin/products/import';

test('CSV preview validates rows and imports draft products with variants, packages and prices', async ({
  page,
}) => {
  const key = 'csv-' + Date.now();
  const errors: string[] = [];
  page.on('pageerror', (e) => errors.push(e.message));
  await loginAdmin(page.request, origin);
  const taxonomy = await (await page.request.get(origin + '/api/v1/admin/taxonomy')).json();
  const category = taxonomy.categories.find(
    (x: { name: string }) => x.name === 'Verificare automată',
  );
  expect(category).toBeTruthy();
  const template = await page.request.get(origin + path + '/template');
  expect(template.status()).toBe(200);
  expect(template.headers()['content-disposition']).toContain('model-import-produse.csv');
  const header = (await template.text()).replace(/^\uFEFF/, '').split(/\r?\n/)[0];
  const row = `Produs CSV ${key};${key};${category.slug};"Descriere; cu separator și ""ghilimele""";${key};M6 x 20;6;20;Oțel;Zincat;${key}-100;Cutie;100;1;1;25.50;0.21`;
  const csv =
    header + '\n' + row + '\n' + row.replace(key + '-100;Cutie;100', key + '-10;Pungă;10');
  const select = (content: string) =>
    page
      .getByLabel('Alege fișierul CSV')
      .setInputFiles({ name: 'produse.csv', mimeType: 'text/csv', buffer: Buffer.from(content) });
  const ids: string[] = [];
  try {
    await page.goto(origin + '/import-produse');
    await expect(page.getByRole('heading', { name: 'Import produse din CSV' })).toBeVisible();
    await select(csv.replace(/25.50/g, '-25.50'));
    await expect(page.getByRole('alert')).toContainText('Import blocat: 2 erori');
    await expect(page.getByRole('alert')).toContainText('Linia 2');
    await expect(page.getByRole('button', { name: /Importă/ })).toHaveCount(0);
    await select(csv);
    await expect(
      page.getByText('Produse: 1 · Variante SKU: 1 · Ambalaje cu preț: 2'),
    ).toBeVisible();
    const save = page.getByRole('button', { name: 'Importă produsele ca ciorne' });
    await expect(save).toBeDisabled();
    const before = await page.request.get(origin + '/api/v1/admin/products?search=' + key);
    expect((await before.json()).items).toHaveLength(0);
    await page.setViewportSize({ width: 1440, height: 1100 });
    await page.screenshot({ path: '../work/import-desktop.png', fullPage: true });
    await page.setViewportSize({ width: 390, height: 844 });
    expect(
      await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
    ).toBeTruthy();
    await page.screenshot({ path: '../work/import-mobile.png', fullPage: true });
    await page.getByLabel('Am verificat produsele, ambalajele și prețurile.').check();
    const response = page.waitForResponse(
      (r) => r.url().endsWith(path + '/commit') && r.request().method() === 'POST',
    );
    await save.click();
    const committed = await response;
    expect(committed.ok()).toBeTruthy();
    ids.push(...(await committed.json()).productIds);
    await expect(page.getByRole('heading', { name: 'Import finalizat' })).toBeVisible();
    expect(ids).toHaveLength(1);
    const item = await (await page.request.get(origin + '/api/v1/admin/products/' + ids[0])).json();
    expect(item.product.status).toBe('Draft');
    expect(item.product.variants).toHaveLength(1);
    expect(item.product.variants[0].offers).toHaveLength(2);
    expect(item.prices).toHaveLength(2);
    expect(item.stock).toEqual([]);
    expect(item.prices[0].netAmount).toBe(25.5);
    expect((await page.request.get(origin + '/api/v1/products/' + key)).status()).toBe(404);
    await select(csv);
    await expect(page.getByRole('alert')).toContainText('Slug-ul produsului există deja');
    expect((await adminWrite(page.request, origin, path + '/commit', { csv })).status()).toBe(409);
    expect(errors).toEqual([]);
  } finally {
    for (const id of ids)
      await adminWrite(page.request, origin, '/api/v1/admin/products/' + id + '/archive');
  }
});

test('CSV endpoints require authentication and CSRF through gateway and direct Catalog', async ({
  request,
}) => {
  for (const status of [401, 400]) {
    if (status === 400) await loginAdmin(request, 'http://127.0.0.1:5099');
    for (const port of [5099, 5101]) {
      const base = 'http://127.0.0.1:' + port;
      for (const operation of ['preview', 'commit'])
        expect(
          (
            await request.post(base + path + '/' + operation, { data: { csv: 'invalid' } })
          ).status(),
        ).toBe(status);
      if (status === 401) expect((await request.get(base + path + '/template')).status()).toBe(401);
    }
  }
});
