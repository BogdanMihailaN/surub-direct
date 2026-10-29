import { test, expect } from '@playwright/test';

test('customer registration through gateway grants no administrative access', async ({
  request,
}) => {
  const origin = 'http://127.0.0.1:5099';
  const email = `customer-browser-${Date.now()}@example.test`;
  const password = 'Browser-test-customer-123!';
  const csrf = await (await request.get(`${origin}/api/auth/csrf`)).json();
  const registration = await request.post(`${origin}/api/auth/register`, {
    data: { email, password, roles: ['Administrator'] },
    headers: { 'X-CSRF-TOKEN': csrf.token },
  });
  expect(registration.status()).toBe(204);
  expect((await request.get(`${origin}/api/auth/me`)).status()).toBe(401);
  expect(
    (
      await request.post(`${origin}/api/auth/login`, {
        data: { email, password },
        headers: { 'X-CSRF-TOKEN': csrf.token },
      })
    ).status(),
  ).toBe(200);
  const me = await request.get(`${origin}/api/auth/me`);
  expect(me.headers()['cache-control']).toBe('no-store');
  expect(await me.json()).toEqual({
    id: expect.any(String),
    email,
    isAdmin: false,
  });
  for (const port of [5099, 5101]) {
    expect((await request.get(`http://127.0.0.1:${port}/api/v1/admin/products`)).status()).toBe(
      403,
    );
    const authenticatedCsrf = await (await request.get(`${origin}/api/auth/csrf`)).json();
    expect(
      (
        await request.post(`http://127.0.0.1:${port}/api/v1/admin/products`, {
          data: { name: 'Forbidden', slug: 'forbidden', description: 'Forbidden' },
          headers: { 'X-CSRF-TOKEN': authenticatedCsrf.token },
        })
      ).status(),
    ).toBe(403);
  }
  expect((await request.get('http://127.0.0.1:5104/api/v1/admin/messaging')).status()).toBe(403);
  const logoutCsrf = await (await request.get(`${origin}/api/auth/csrf`)).json();
  expect(
    (
      await request.post(`${origin}/api/auth/logout`, {
        headers: { 'X-CSRF-TOKEN': logoutCsrf.token },
      })
    ).status(),
  ).toBe(204);
  expect((await request.get(`${origin}/api/auth/me`)).status()).toBe(401);
});
