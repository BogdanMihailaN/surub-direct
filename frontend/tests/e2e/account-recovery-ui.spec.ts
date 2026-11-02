import { expect, Page, test } from '@playwright/test';
async function csrf(page: Page) {
  await page.route('**/api/auth/csrf', (r) => r.fulfill({ json: { token: 'csrf' } }));
  await page.route('**/api/v1/ordering/cart', (r) => r.fulfill({ status: 204 }));
}
const link = '#userId=account-one&token=' + encodeURIComponent('test-token+/=');
test('requests remain generic and rate limits allow an explicit retry', async ({ page }) => {
  await csrf(page);
  let writes = 0;
  await page.route('**/api/auth/request-password-reset', (r) => {
    expect(r.request().headers()['x-csrf-token']).toBe('csrf');
    writes++;
    return r.fulfill(writes === 1 ? { status: 429, json: {} } : { status: 204 });
  });
  await page.goto('/cont/recuperare');
  await page.getByLabel('Adresa de e-mail').fill('absent@example.test');
  await page.getByRole('button', { name: 'Solicită mesajul' }).click();
  await expect(page.getByRole('alert')).toContainText('Prea multe solicitări');
  expect(writes).toBe(1);
  await page.getByRole('button', { name: 'Solicită mesajul' }).click();
  await expect(page.getByRole('status')).toContainText('Dacă adresa corespunde');
  expect(writes).toBe(2);
});
test('confirmation requires an explicit click and removes the token from the URL', async ({
  page,
}) => {
  await csrf(page);
  let writes = 0;
  const urls: string[] = [];
  page.on('request', (r) => urls.push(r.url()));
  await page.route('**/api/auth/confirm-email', (r) => {
    writes++;
    expect(r.request().postDataJSON()).toEqual({ userId: 'account-one', token: 'test-token+/=' });
    return r.fulfill({ status: 204 });
  });
  await page.goto('/cont/confirma-email' + link);
  await expect(page.getByRole('button', { name: 'Confirmă adresa', exact: true })).toBeVisible();
  expect(new URL(page.url()).hash).toBe('');
  expect(writes).toBe(0);
  expect(urls.every((url) => !url.includes('test-token'))).toBe(true);
  await page.getByRole('button', { name: 'Confirmă adresa', exact: true }).click();
  await expect(page.getByRole('status')).toContainText('Adresa de e-mail a fost confirmată');
  await expect(page.getByRole('button', { name: 'Confirmă adresa', exact: true })).toHaveCount(0);
  expect(writes).toBe(1);
});
test('passwords must match and are cleared after an uncertain reset without replay', async ({
  page,
}) => {
  await csrf(page);
  let writes = 0;
  await page.route('**/api/auth/reset-password', (r) => {
    writes++;
    return r.abort();
  });
  await page.goto('/cont/reseteaza-parola' + link);
  const password = page.getByLabel('Parola nouă', { exact: true });
  const confirmation = page.getByLabel('Repetă parola nouă', { exact: true });
  await password.fill('Replacement-123!');
  await confirmation.fill('Different-123!');
  await page.getByRole('button', { name: 'Schimbă parola' }).click();
  await expect(page.getByRole('alert')).toContainText('Parolele nu coincid');
  expect(writes).toBe(0);
  await confirmation.fill('Replacement-123!');
  await page.getByRole('button', { name: 'Schimbă parola' }).click();
  await expect(page.getByRole('alert')).toContainText('Nu putem confirma rezultatul');
  await expect(password).toHaveValue('');
  await expect(confirmation).toHaveValue('');
  expect(writes).toBe(1);
});
test('missing and expired links offer a new request without reporting success', async ({
  page,
}) => {
  await csrf(page);
  await page.goto('/cont/reseteaza-parola');
  await expect(page.getByRole('alert')).toContainText('Link incomplet');
  await expect(page.locator('sd-password-reset-form')).toHaveCount(0);
  await page.route('**/api/auth/confirm-email', (r) => r.fulfill({ status: 400, json: {} }));
  await page.goto('/cont/confirma-email' + link);
  await page.getByRole('button', { name: 'Confirmă adresa', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('link expirat ori deja folosit');
  await expect(page.getByRole('link', { name: 'Solicită un mesaj nou' })).toBeVisible();
});
test('successful reset hides the form and stays within the mobile viewport', async ({ page }) => {
  await csrf(page);
  await page.route('**/api/auth/reset-password', (r) => r.fulfill({ status: 204 }));
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/cont/reseteaza-parola' + link);
  await page.getByLabel('Parola nouă', { exact: true }).fill('Replacement-123!');
  await page.getByLabel('Repetă parola nouă', { exact: true }).fill('Replacement-123!');
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.screenshot({ path: '../work/account-recovery-mobile.png', fullPage: true });
  await page.getByRole('button', { name: 'Schimbă parola' }).click();
  await expect(page.getByRole('status')).toContainText('Parola a fost schimbată');
  await expect(page.locator('sd-password-reset-form')).toHaveCount(0);
  await expect(page.getByRole('link', { name: 'Mergi la autentificare' })).toBeVisible();
});

test('a response arriving after navigation cannot appear on another recovery page', async ({
  page,
}) => {
  await csrf(page);
  let release!: () => void;
  let requested = false;
  const gate = new Promise<void>((resolve) => (release = resolve));
  await page.route('**/api/auth/request-password-reset', async (route) => {
    requested = true;
    await gate;
    await route.fulfill({ status: 204 });
  });
  await page.goto('/cont/recuperare');
  await page.getByLabel('Adresa de e-mail').fill('demo@example.test');
  await page.getByRole('button', { name: 'Solicită mesajul' }).click();
  await expect.poll(() => requested).toBe(true);
  await page.goto('/cont/confirma-email');
  release();
  await expect(page.getByRole('alert')).toContainText('Link incomplet');
  await expect(page.getByText('Dacă adresa corespunde', { exact: false })).toHaveCount(0);
});
