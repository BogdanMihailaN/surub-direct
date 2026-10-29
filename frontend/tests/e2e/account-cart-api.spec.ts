import { APIRequestContext, expect, test } from '@playwright/test';

const gateway = 'http://127.0.0.1:5099';
const cartPath = '/api/v1/ordering/account/cart';
async function identityHeaders(api: APIRequestContext) {
  const token = await (await api.get(gateway + '/api/auth/csrf')).json();
  return { 'X-CSRF-TOKEN': token.token };
}
async function login(api: APIRequestContext, email: string, password: string) {
  expect(
    (
      await api.post(gateway + '/api/auth/login', {
        headers: await identityHeaders(api),
        data: { email, password },
      })
    ).status(),
  ).toBe(200);
  return (await (await api.get(gateway + '/api/auth/me')).json()).id as string;
}

test('account carts require session and Identity CSRF, transfer explicitly and work on another device', async ({
  playwright,
}) => {
  const first = await playwright.request.newContext();
  const second = await playwright.request.newContext();
  const stranger = await playwright.request.newContext();
  const email = `cart-account-${Date.now()}@example.test`;
  const password = 'Account-cart-test-123!';
  try {
    expect(
      (
        await first.post(gateway + '/api/auth/register', {
          headers: await identityHeaders(first),
          data: { email, password },
        })
      ).status(),
    ).toBe(204);
    const accountId = await login(first, email, password);
    const query = '?expectedAccountId=' + encodeURIComponent(accountId);
    for (const origin of [gateway, 'http://127.0.0.1:5104']) {
      const url = origin + cartPath;
      expect((await stranger.get(url + query)).status()).toBe(401);
      expect((await stranger.post(url + '/session' + query)).status()).toBe(401);
      expect((await first.get(url + '?expectedAccountId=someone-else')).status()).toBe(409);
      expect((await first.get(url)).status()).toBe(400);
      expect((await first.post(url + '/session' + query)).status()).toBe(400);
      for (const suffix of ['', '/customer'])
        expect((await first.put(url + suffix + query, { data: {} })).status()).toBe(400);
      expect((await first.post(url + '/import' + query, { data: {} })).status()).toBe(400);
    }
    const url = gateway + cartPath;
    expect((await first.get(url + query)).status()).toBe(204);
    const created = await first.post(url + '/session' + query, {
      headers: await identityHeaders(first),
    });
    expect(created.status()).toBe(200);
    const target = await created.json();
    expect(JSON.stringify(target)).not.toContain('ownerHash');
    expect((await first.storageState()).cookies.some((c) => c.name === 'SurubDirect.Cart')).toBe(
      false,
    );

    const guestUrl = gateway + '/api/v1/ordering/cart';
    const csrf = await (await first.get(guestUrl + '/csrf')).json();
    const guestHeaders = { 'X-CART-CSRF': csrf.token };
    const guest = await (await first.post(guestUrl + '/session', { headers: guestHeaders })).json();
    const lines = [{ productSlug: 'surub-test', offerId: crypto.randomUUID(), packs: 2 }];
    const populated = await first.put(guestUrl, {
      headers: guestHeaders,
      data: { cartId: guest.id, version: guest.version, lines },
    });
    expect(populated.status()).toBe(200);
    const source = await populated.json();
    const transfer = {
      cartId: source.id,
      version: source.version,
      targetCartId: target.id,
      targetVersion: target.version,
      includeCustomer: false,
    };
    expect(
      (
        await first.post(url + '/import?expectedAccountId=someone-else', {
          headers: await identityHeaders(first),
          data: transfer,
        })
      ).status(),
    ).toBe(409);
    const imported = await first.post(url + '/import' + query, {
      headers: await identityHeaders(first),
      data: transfer,
    });
    expect(imported.status()).toBe(200);
    const moved = await imported.json();
    expect(moved.lines).toEqual(lines);
    expect((await (await first.get(guestUrl)).json()).lines).toEqual([]);
    expect(
      (
        await first.post(url + '/import' + query, {
          headers: await identityHeaders(first),
          data: transfer,
        })
      ).status(),
    ).toBe(409);

    expect(await login(second, email, password)).toBe(accountId);
    expect((await second.storageState()).cookies.some((c) => c.name === 'SurubDirect.Cart')).toBe(
      false,
    );
    const read = await second.get(url + query);
    expect(read.headers()['cache-control']).toBe('no-store');
    expect((await read.json()).lines).toEqual(lines);
    const changedLines = [{ ...lines[0], packs: 4 }];
    expect(
      (
        await second.put(url + query, {
          headers: await identityHeaders(second),
          data: { cartId: moved.id, version: moved.version, lines: changedLines },
        })
      ).status(),
    ).toBe(200);
    expect(
      (
        await first.put(url + query, {
          headers: await identityHeaders(first),
          data: { cartId: moved.id, version: moved.version, lines: [] },
        })
      ).status(),
    ).toBe(409);
    expect((await (await first.get(url + query)).json()).lines).toEqual(changedLines);

    const otherEmail = `other-${email}`;
    expect(
      (
        await stranger.post(gateway + '/api/auth/register', {
          headers: await identityHeaders(stranger),
          data: { email: otherEmail, password },
        })
      ).status(),
    ).toBe(204);
    const otherId = await login(stranger, otherEmail, password);
    expect((await stranger.get(url + query)).status()).toBe(409);
    expect((await stranger.get(url + '?expectedAccountId=' + otherId)).status()).toBe(204);
    expect(
      (
        await stranger.put(url + '?expectedAccountId=' + otherId, {
          headers: await identityHeaders(stranger),
          data: { cartId: moved.id, version: moved.version, lines: [] },
        })
      ).status(),
    ).toBe(404);
    expect(
      (
        await first.post(gateway + '/api/auth/logout', { headers: await identityHeaders(first) })
      ).status(),
    ).toBe(204);
    expect((await first.get(url + query)).status()).toBe(401);
    expect((await (await first.get(guestUrl)).json()).lines).toEqual([]);
  } finally {
    await first.dispose();
    await second.dispose();
    await stranger.dispose();
  }
});
