import {
  expect,
  type APIRequestContext,
  type Browser,
  type PlaywrightWorkerArgs,
} from '@playwright/test';

export async function assertAccountOrderOwnership(
  playwright: PlaywrightWorkerArgs['playwright'],
  original: APIRequestContext,
  id: string,
  browser: Browser,
) {
  const origin = 'http://localhost:4200';
  const path = '/api/v1/ordering/account/orders';
  const email = `order-owner-${crypto.randomUUID()}@example.test`;
  const password = 'Order-owner-test-123!';
  const context = await browser.newContext({
    storageState: await original.storageState(),
  });
  const owner = context.request;
  const page = await context.newPage();
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
    expect((await write(owner, claim + '?expectedAccountId=wrong-account')).status()).toBe(409);
    expect((await owner.get(origin + path + '?expectedAccountId=wrong-account')).status()).toBe(
      409,
    );
    const unclaimed = await (await owner.get(origin + '/api/v1/ordering/demo-orders')).json();
    const number = unclaimed.items.find((order: { id: string }) => order.id === id).number;
    await page.goto(origin + '/cont');
    const browserOrders = page.getByRole('region', {
      name: 'Comenzile neasociate din browser',
      exact: true,
    });
    const accountOrders = page.getByRole('region', { name: 'Comenzile contului', exact: true });
    await browserOrders.getByRole('button', { name: 'Asociază ' + number, exact: true }).click();
    const confirmation = page.getByRole('region', { name: 'Confirmă asocierea', exact: true });
    await expect(confirmation).toContainText(email);
    await confirmation.getByRole('button', { name: 'Renunță', exact: true }).click();
    expect((await (await owner.get(origin + path)).json()).items).toEqual([]);
    await browserOrders.getByRole('button', { name: 'Asociază ' + number, exact: true }).click();
    await page.route('**/api/v1/ordering/account/orders/*/claim?**', async (route) => {
      expect((await route.fetch()).status()).toBe(204);
      await route.abort();
    });
    await confirmation.getByRole('button', { name: 'Confirmă asocierea', exact: true }).click();
    await expect(accountOrders.getByRole('heading', { name: number, exact: true })).toBeVisible();
    await expect(browserOrders.getByRole('heading', { name: number, exact: true })).toHaveCount(0);
    await expect(page.getByRole('status')).toContainText('Starea a fost reverificată');
    await page.unroute('**/api/v1/ordering/account/orders/*/claim?**');
    await page.reload();
    await expect(accountOrders.getByRole('heading', { name: number, exact: true })).toBeVisible();
    await page.setViewportSize({ width: 390, height: 844 });
    await accountOrders.getByText('Detalii ' + number, { exact: true }).click();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(
      true,
    );
    await page.screenshot({ path: '../work/account-order-history-mobile.png', fullPage: true });

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
    await context.close();
    await clean.dispose();
  }
}
