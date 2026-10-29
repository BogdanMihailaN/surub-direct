import { test, expect } from '@playwright/test';

test('customer can register, login, reload and logout from the storefront', async ({ page }) => {
  const email = `account-ui-${Date.now()}@example.test`;
  const password = 'Account-browser-test-123!';
  await page.goto('/');
  await page.getByRole('link', { name: 'Contul meu', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Contul meu', exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Cont nou', exact: true }).click();
  await expect(page.getByRole('group', { name: 'Creează un cont', exact: true })).toBeVisible();
  await page.getByLabel('Adresă e-mail', { exact: true }).fill(email);
  await page.getByLabel('Parolă', { exact: true }).fill(password);
  await page.getByLabel('Confirmă parola', { exact: true }).fill('Mismatch-123!');
  await expect(page.getByRole('button', { name: 'Creează contul', exact: true })).toBeDisabled();
  await expect(page.getByText('Parolele nu coincid.')).toBeVisible();
  await page.getByLabel('Confirmă parola', { exact: true }).fill(password);
  await page.getByRole('button', { name: 'Creează contul', exact: true }).click();
  await expect(page.getByRole('status')).toContainText('Cererea a fost procesată');
  await expect(page.getByLabel('Parolă', { exact: true })).toHaveValue('');
  await page.getByRole('button', { name: 'Am deja cont', exact: true }).click();
  await expect(page.getByRole('group', { name: 'Autentificare', exact: true })).toBeVisible();
  await page.getByLabel('Adresă e-mail', { exact: true }).fill(email);
  await page.getByLabel('Parolă', { exact: true }).fill('Wrong-password-123!');
  await page.getByRole('button', { name: 'Autentificare', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('Verifică adresa și parola');
  await expect(page.getByLabel('Parolă', { exact: true })).toHaveValue('');
  await page.getByLabel('Parolă', { exact: true }).fill(password);
  await page.getByRole('button', { name: 'Autentificare', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Ești autentificat' })).toBeVisible();
  await expect(page.getByText(email, { exact: true })).toBeVisible();
  await page.reload();
  await expect(page.getByText(email, { exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Deconectare' }).click();
  await expect(page.getByRole('button', { name: 'Autentificare', exact: true })).toBeVisible();
  await expect(page.getByText(email, { exact: true })).toHaveCount(0);
  await page.reload();
  await expect(page.getByRole('button', { name: 'Autentificare', exact: true })).toBeVisible();
});

test('unavailable session offers retry instead of showing a guest form', async ({ page }) => {
  await page.route('**/api/auth/me', (route) => route.fulfill({ status: 503, json: {} }));
  await page.goto('/cont');
  await expect(page.getByText('Nu putem confirma starea sesiunii.')).toBeVisible();
  await expect(page.getByLabel('Adresă e-mail')).toHaveCount(0);
  await page.unroute('**/api/auth/me');
  await page.getByRole('button', { name: 'Reîncearcă verificarea' }).click();
  await expect(page.getByLabel('Adresă e-mail')).toBeVisible();
});

test('mobile registration handles unavailable registration and clears passwords', async ({
  page,
}) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.route('**/api/auth/register', (route) => route.fulfill({ status: 404, json: {} }));
  await page.goto('/cont');
  await page.getByRole('button', { name: 'Cont nou', exact: true }).click();
  await expect(page.getByRole('group', { name: 'Creează un cont', exact: true })).toBeVisible();
  await page.getByLabel('Adresă e-mail', { exact: true }).fill('test@example.test');
  await page.getByLabel('Parolă', { exact: true }).fill('Account-browser-test-123!');
  await page.getByLabel('Confirmă parola', { exact: true }).fill('Account-browser-test-123!');
  await page.getByRole('button', { name: 'Creează contul', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('doar în mediul demonstrativ');
  await expect(page.getByLabel('Parolă', { exact: true })).toHaveValue('');
  await expect(page.getByLabel('Confirmă parola', { exact: true })).toHaveValue('');
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(
    true,
  );
  await page.screenshot({ path: '../work/account-mobile.png', fullPage: true });
});

test('uncertain login and failed logout require session verification', async ({ page }) => {
  let authenticated = false;
  await page.route('**/api/auth/me', (route) =>
    route.fulfill(
      authenticated
        ? {
            status: 200,
            json: { id: 'session-test-id', email: 'session@example.test', isAdmin: true },
          }
        : { status: 401, json: {} },
    ),
  );
  await page.route('**/api/auth/login', (route) => route.abort());
  await page.goto('/cont');
  await expect(page.getByRole('group', { name: 'Autentificare', exact: true })).toBeVisible();
  await page.getByLabel('Adresă e-mail', { exact: true }).fill('session@example.test');
  await page.getByLabel('Parolă', { exact: true }).fill('Account-browser-test-123!');
  await page.getByRole('button', { name: 'Autentificare', exact: true }).click();
  await expect(page.getByText('Nu putem confirma starea sesiunii.')).toBeVisible();
  await expect(page.getByLabel('Adresă e-mail')).toHaveCount(0);
  authenticated = true;
  await page.getByRole('button', { name: 'Reîncearcă verificarea' }).click();
  await expect(page.getByText('session@example.test', { exact: true })).toBeVisible();
  await expect(
    page.getByText('Această sesiune are acces administrativ.', { exact: false }),
  ).toBeVisible();
  await page.route('**/api/auth/logout', (route) => route.fulfill({ status: 503, json: {} }));
  await page.getByRole('button', { name: 'Deconectare' }).click();
  await expect(page.getByText('Nu putem confirma starea sesiunii.')).toBeVisible();
  await expect(page.getByText('session@example.test', { exact: true })).toHaveCount(0);
  await page.getByRole('button', { name: 'Reîncearcă verificarea' }).click();
  await expect(page.getByText('session@example.test', { exact: true })).toBeVisible();
  authenticated = false;
  await page.evaluate(() => window.dispatchEvent(new Event('focus')));
  await expect(page.getByRole('group', { name: 'Autentificare', exact: true })).toBeVisible();
  await expect(page.getByText('session@example.test', { exact: true })).toHaveCount(0);
});
