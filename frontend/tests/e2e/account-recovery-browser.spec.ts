import { expect, test } from '@playwright/test';
import { readdirSync, readFileSync } from 'node:fs';
import { resolve } from 'node:path';
test('customer requests local messages, confirms email and resets password in the UI', async ({
  page,
  playwright,
}) => {
  const origin = 'http://localhost:4200';
  const api = await playwright.request.newContext();
  const email = 'recovery-ui-' + crypto.randomUUID() + '@example.test';
  const password = 'Initial-recovery-123!';
  const replacement = 'Replaced-recovery-123!';
  const write = async (path: string, data: object) => {
    const csrf = (await (await api.get(origin + '/api/auth/csrf')).json()).token;
    return api.post(origin + '/api/auth/' + path, { headers: { 'X-CSRF-TOKEN': csrf }, data });
  };
  const message = (kind: string) => {
    const root = resolve('../work/identity-mail');
    return readdirSync(root)
      .filter((n) => n.endsWith('.json'))
      .map((n) => JSON.parse(readFileSync(resolve(root, n), 'utf8')))
      .find((m) => m.email === email && m.kind === kind);
  };
  try {
    expect((await write('register', { email, password })).status()).toBe(204);
    expect((await write('login', { email, password })).status()).toBe(200);
    await page.goto('/cont');
    await page.getByRole('link', { name: 'Recuperează parola sau confirmă e-mailul' }).click();
    await page.getByLabel('Operațiune').selectOption('email');
    await page.getByRole('textbox', { name: 'Adresa de e-mail', exact: true }).fill(email);
    await page.getByRole('button', { name: 'Solicită mesajul' }).click();
    await expect(page.getByRole('status')).toContainText('Dacă adresa corespunde');
    const confirmation = message('confirm-email');
    expect(confirmation.textBody).toContain(confirmation.actionUrl);
    expect(confirmation.subject).toContain('Confirmă');
    await page.goto(confirmation.actionUrl);
    await page.getByRole('button', { name: 'Confirmă adresa', exact: true }).click();
    await expect(page.getByRole('status')).toContainText('Adresa de e-mail a fost confirmată');
    await page.getByRole('link', { name: 'Solicită un mesaj nou' }).click();
    await page.getByRole('textbox', { name: 'Adresa de e-mail', exact: true }).fill(email);
    await page.getByRole('button', { name: 'Solicită mesajul' }).click();
    await expect(page.getByRole('status')).toContainText('Dacă adresa corespunde');
    const reset = message('reset-password');
    expect(reset.textBody).toContain(reset.actionUrl);
    expect(reset.subject).toContain('Resetează');
    await page.goto(reset.actionUrl);
    await page.getByLabel('Parola nouă', { exact: true }).fill(replacement);
    await page.getByLabel('Repetă parola nouă', { exact: true }).fill(replacement);
    await page.getByRole('button', { name: 'Schimbă parola' }).click();
    await expect(page.getByRole('status')).toContainText('Parola a fost schimbată');
    expect((await api.get(origin + '/api/auth/me')).status()).toBe(401);
    expect((await write('login', { email, password })).status()).toBe(401);
    await page.getByRole('link', { name: 'Mergi la autentificare' }).click();
    await page.getByLabel('Adresă e-mail', { exact: true }).fill(email);
    await page.getByLabel('Parolă', { exact: true }).fill(replacement);
    await page.getByRole('button', { name: 'Autentificare', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Ești autentificat' })).toBeVisible();
  } finally {
    await api.dispose();
  }
});
