import { expect, type APIRequestContext, type PlaywrightWorkerArgs } from '@playwright/test';

export async function assertAccountOrderOwnership(
  playwright: PlaywrightWorkerArgs['playwright'],
  original: APIRequestContext,
  id: string,
) {
  const origin = 'http://localhost:4200';
  const path = '/api/v1/ordering/account/orders';
  const email = `order-owner-${crypto.randomUUID()}@example.test`;
  const password = 'Order-owner-test-123!';
  const owner = await playwright.request.newContext({
    storageState: await original.storageState(),
  });
  const clean = await playwright.request.newContext();
  const write = async (client: APIRequestContext, url: string, data = {}) => {
    const csrf = await (await client.get(origin + '/api/auth/csrf')).json();
    return client.post(url, { data, headers: { 'X-CSRF-TOKEN': csrf.token } });
  };
  try {
    for (const base of [origin, 'http://localhost:5104']) {
      expect((await clean.get(base + path)).status()).toBe(401);
      expect((await clean.post(base + path + '/' + id + '/claim')).status()).toBe(401);
    }
    expect((await write(owner, origin + '/api/auth/logout')).status()).toBe(204);
    expect((await write(owner, origin + '/api/auth/register', { email, password })).status()).toBe(
      204,
    );
    expect((await write(owner, origin + '/api/auth/login', { email, password })).status()).toBe(
      200,
    );
    expect((await (await owner.get(origin + path)).json()).items).toEqual([]);
    for (const base of [origin, 'http://localhost:5104']) {
      expect((await owner.post(base + path + '/' + id + '/claim')).status()).toBe(400);
    }
    const claim = origin + path + '/' + id + '/claim';
    expect(
      (await write(owner, claim, { accountId: 'cannot-choose-target-account' })).status(),
    ).toBe(204);
    expect((await write(owner, claim)).status()).toBe(204);
    const result = await owner.get(origin + path);
    expect(result.headers()['cache-control']).toBe('no-store');
    expect((await result.json()).items.map((order: { id: string }) => order.id)).toEqual([id]);
    expect(
      (await (await original.get(origin + '/api/v1/ordering/demo-orders')).json()).items,
    ).toEqual([]);
    // The account can read its order in a browser without the original cart cookie.
    expect((await write(clean, origin + '/api/auth/login', { email, password })).status()).toBe(
      200,
    );
    expect(
      (await (await clean.get(origin + path)).json()).items.map(
        (order: { id: string }) => order.id,
      ),
    ).toEqual([id]);
    expect((await write(clean, claim)).status()).toBe(401);
    // A second account with the same browser ownership proof cannot take the order.
    expect((await write(owner, origin + '/api/auth/logout')).status()).toBe(204);
    const other = { email: `other-${crypto.randomUUID()}@example.test`, password };
    expect((await write(owner, origin + '/api/auth/register', other)).status()).toBe(204);
    expect((await write(owner, origin + '/api/auth/login', other)).status()).toBe(200);
    expect((await (await owner.get(origin + path)).json()).items).toEqual([]);
    expect((await write(owner, claim)).status()).toBe(404);
    expect(
      (await (await clean.get(origin + path)).json()).items.map(
        (order: { id: string }) => order.id,
      ),
    ).toEqual([id]);
  } finally {
    await owner.dispose();
    await clean.dispose();
  }
}
