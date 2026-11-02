import { expect, test } from '@playwright/test';
import { readdirSync, readFileSync } from 'node:fs';
import { resolve } from 'node:path';
test('recovery works through gateway and direct Identity without exposing tokens in responses', async ({
  playwright,
}) => {
  const mailbox = resolve('../work/identity-mail');
  for (const origin of ['http://127.0.0.1:5099', 'http://127.0.0.1:5103']) {
    const client = await playwright.request.newContext();
    const signedIn = await playwright.request.newContext();
    const email = 'recovery-' + crypto.randomUUID() + '@example.test';
    const password = 'Recovery-initial-123!';
    const newPassword = 'Recovery-replaced-123!';
    const write = async (path: string, data: unknown, api = client) => {
      const csrf = (await (await api.get(origin + '/api/auth/csrf')).json()).token;
      return api.post(origin + '/api/auth/' + path, { headers: { 'X-CSRF-TOKEN': csrf }, data });
    };
    const message = (kind: string) =>
      readdirSync(mailbox)
        .filter((n) => n.endsWith('.json'))
        .map((n) => JSON.parse(readFileSync(resolve(mailbox, n), 'utf8')))
        .find((m) => m.email === email && m.kind === kind);
    try {
      expect((await write('register', { email, password })).status()).toBe(204);
      expect((await write('login', { email, password }, signedIn)).status()).toBe(200);
      expect(
        (
          await client.post(origin + '/api/auth/request-email-confirmation', { data: { email } })
        ).status(),
      ).toBe(400);
      const requested = await write('request-email-confirmation', { email });
      expect(requested.status()).toBe(204);
      expect(await requested.text()).toBe('');
      expect(requested.headers()['cache-control']).toBe('no-store');
      const confirmation = message('confirm-email');
      expect(confirmation).toBeTruthy();
      expect(
        (
          await write('confirm-email', { userId: confirmation.userId, token: confirmation.token })
        ).status(),
      ).toBe(204);
      expect((await write('request-password-reset', { email })).status()).toBe(204);
      const reset = message('reset-password');
      expect(
        (
          await write('reset-password', {
            userId: reset.userId,
            token: reset.token,
            password: newPassword,
          })
        ).status(),
      ).toBe(204);
      expect((await signedIn.get(origin + '/api/auth/me')).status()).toBe(401);
      expect(
        (
          await write('reset-password', {
            userId: reset.userId,
            token: reset.token,
            password: newPassword,
          })
        ).status(),
      ).toBe(400);
      expect((await write('login', { email, password })).status()).toBe(401);
      expect((await write('login', { email, password: newPassword })).status()).toBe(200);
    } finally {
      await client.dispose();
      await signedIn.dispose();
    }
  }
});
