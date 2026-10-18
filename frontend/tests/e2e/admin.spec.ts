import { test, expect } from '@playwright/test';
import { adminCredentials } from './support/admin-session';

test('administrator can create, edit, archive, restore and republish without losing stock', async ({
  page,
}) => {
  const token = Date.now().toString();
  const name = 'Produs test browser ' + token;
  const sku = 'BROWSER-' + token;
  const errors: string[] = [];
  page.on('pageerror', (e) => errors.push(e.message));
  await page.goto('http://localhost:4201');
  await page.getByLabel('Adresă e-mail').fill(adminCredentials.email);
  await page.getByLabel('Parolă', { exact: true }).fill(adminCredentials.password);
  await page.getByRole('button', { name: 'Autentificare', exact: true }).click();
  await page.getByRole('button', { name: '+ Produs', exact: true }).click();
  await page.getByLabel('Denumire produs').fill(name);
  await page.getByLabel('Adresă URL (slug)').fill('browser-' + token);
  await page.getByLabel('Categorie', { exact: true }).fill('Verificare automată');
  await page
    .getByLabel('Descriere', { exact: true })
    .fill('Produs creat prin testul automat al fluxului complet.');
  await page.getByRole('button', { name: 'Creează ciorna' }).click();
  await expect(page.getByRole('heading', { name, exact: true })).toBeVisible();
  await page.getByLabel('Caută produs sau SKU').fill(token);
  await page.getByLabel('Stare produs').selectOption('Draft');
  await page.getByRole('button', { name: 'Aplică filtrele' }).click();
  await expect(page.locator('.product-list button')).toHaveCount(1);
  await page.locator('.product-list button').click();
  const variant = page
    .locator('form')
    .filter({ has: page.getByRole('heading', { name: 'Adaugă SKU', exact: true }) });
  await variant.getByLabel('SKU', { exact: true }).fill(sku);
  await variant.getByLabel('Denumire variantă').fill('M12 × 50 mm');
  await variant.getByLabel('Diametru (mm)').fill('12');
  await variant.getByLabel('Lungime (mm)').fill('50');
  await variant.getByRole('button', { name: 'Adaugă varianta' }).click();
  await expect(
    page.getByText('Variantă creată. Configurează ambalajul.', { exact: true }),
  ).toBeVisible();
  const offer = page
    .locator('form')
    .filter({ has: page.getByRole('heading', { name: 'Adaugă ambalaj', exact: true }) });
  await offer.getByLabel('Cod ofertă').fill(sku + '-C100');
  await offer.getByRole('button', { name: 'Adaugă ambalajul' }).click();
  await expect(page.getByText('Ambalaj creat. Completează prețul.', { exact: true })).toBeVisible();
  const price = page
    .locator('form')
    .filter({ has: page.getByRole('heading', { name: 'Stabilește prețul' }) });
  await price.getByLabel('Preț net / ambalaj (lei)').fill('40');
  await price.getByLabel('TVA (%)').fill('21');
  await price.getByRole('button', { name: 'Salvează prețul' }).click();
  await expect(page.getByText('Preț actualizat.', { exact: true })).toBeVisible();
  const receipt = page
    .locator('form')
    .filter({ has: page.getByRole('heading', { name: 'Recepționează marfa' }) });
  await receipt
    .getByRole('combobox', { name: 'Varianta', exact: true })
    .selectOption({ label: sku });
  await receipt.getByLabel('Cantitate în bucăți').fill('250');
  await receipt.getByLabel('Referință document').fill('TEST-' + token);
  await receipt.getByRole('button', { name: 'Înregistrează recepția' }).click();
  await expect(page.getByText('250 buc.', { exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Publică produsul' }).click();
  await expect(page.getByText('Produs publicat în magazin.', { exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Editează produsul', exact: true }).click();
  await page.getByLabel('Denumire produs').fill('');
  await expect(page.getByRole('button', { name: 'Salvează modificările' })).toBeDisabled();
  await page.getByRole('button', { name: 'Renunță la editare' }).click();
  await expect(page.getByRole('heading', { name, exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Editează produsul', exact: true }).click();
  await page.getByLabel('Denumire produs').fill('Z ' + name);
  await page
    .getByLabel('Descriere', { exact: true })
    .fill('Descriere actualizată din administrare.');
  await page.getByRole('button', { name: 'Salvează modificările' }).click();
  await expect(page.getByRole('heading', { name: 'Z ' + name, exact: true })).toBeVisible();
  const shop = await page.context().newPage();
  const productUrl = 'http://localhost:4200/produse/browser-' + token;
  await shop.goto(productUrl);
  await expect(shop.getByRole('heading', { name: 'Z ' + name, exact: true })).toBeVisible();
  await expect(
    shop.getByText('Descriere actualizată din administrare.', { exact: true }),
  ).toBeVisible();
  await expect(shop.getByText('2 ambalaje', { exact: true })).toBeVisible();
  await expect(shop.getByText('48,40 lei', { exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Arhivează produsul', exact: true }).click();
  await page.getByRole('button', { name: 'Renunță', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Confirmă arhivarea' })).toHaveCount(0);
  await page.getByRole('button', { name: 'Arhivează produsul', exact: true }).click();
  await page.getByRole('button', { name: 'Confirmă arhivarea' }).click();
  await expect(
    page.getByText('Produs arhivat. Nu mai este vizibil în magazin.', { exact: true }),
  ).toBeVisible();
  await expect(page.getByText('250 buc.', { exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Publică produsul' })).toHaveCount(0);
  expect((await shop.request.get('/api/v1/products/browser-' + token)).status()).toBe(404);
  await shop.reload();
  await expect(shop.getByRole('alert')).toHaveText('Produsul nu există sau nu este publicat.');
  await page.getByLabel('Caută produs sau SKU').fill(token);
  await page.getByLabel('Stare produs').selectOption('Archived');
  await page.getByRole('button', { name: 'Aplică filtrele' }).click();
  await expect(page.locator('.product-list button')).toHaveCount(1);
  await page.locator('.product-list button').click();
  await page.getByRole('button', { name: 'Restaurează ca ciornă' }).click();
  await expect(
    page.getByText('Produs restaurat ca ciornă. Verifică datele înainte de publicare.', {
      exact: true,
    }),
  ).toBeVisible();
  expect((await shop.request.get('/api/v1/products/browser-' + token)).status()).toBe(404);
  await shop.reload();
  await expect(shop.getByRole('alert')).toHaveText('Produsul nu există sau nu este publicat.');
  await page.getByRole('button', { name: 'Publică produsul' }).click();
  await expect(page.getByText('Produs publicat în magazin.', { exact: true })).toBeVisible();
  await shop.goto(productUrl);
  await expect(shop.getByRole('heading', { name: 'Z ' + name, exact: true })).toBeVisible();
  await expect(shop.getByText('2 ambalaje', { exact: true })).toBeVisible();
  await expect(shop.getByText('48,40 lei', { exact: true })).toBeVisible();
  // Leave automated merchandise hidden after exercising republication.
  await page.getByRole('button', { name: 'Arhivează produsul', exact: true }).click();
  await page.getByRole('button', { name: 'Confirmă arhivarea' }).click();
  await expect(
    page.getByText('Produs arhivat. Nu mai este vizibil în magazin.', { exact: true }),
  ).toBeVisible();
  await shop.close();
  expect(errors).toEqual([]);
});
test('administrative endpoints reject anonymous access and missing CSRF', async ({ request }) => {
  const unauthorized = await request.get('http://127.0.0.1:5099/api/v1/admin/products');
  expect(unauthorized.status()).toBe(401);
  const missingToken = await request.post('http://127.0.0.1:5099/api/auth/login', {
    data: { email: adminCredentials.email, password: adminCredentials.password },
  });
  expect(missingToken.status()).toBe(400);
});

test('authenticated price writes reject malformed numeric data with 400', async ({ request }) => {
  let csrf = await (await request.get('http://127.0.0.1:5099/api/auth/csrf')).json();
  const login = await request.post('http://127.0.0.1:5099/api/auth/login', {
    data: { email: adminCredentials.email, password: adminCredentials.password },
    headers: { 'X-CSRF-TOKEN': csrf.token },
  });
  expect(login.ok()).toBeTruthy();
  csrf = await (await request.get('http://127.0.0.1:5099/api/auth/csrf')).json();
  const result = await request.put(
    'http://127.0.0.1:5099/api/v1/admin/offers/00000000-0000-0000-0000-000000000001/price',
    {
      data: { netAmount: null, taxRate: 0.21 },
      headers: { 'X-CSRF-TOKEN': csrf.token },
    },
  );
  expect(result.status()).toBe(400);
});
