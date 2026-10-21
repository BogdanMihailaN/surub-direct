import { test, expect, APIRequestContext } from '@playwright/test';
const shop = 'http://localhost:4200';
const path = '/api/v1/ordering/cart';
async function headers(api: APIRequestContext, origin = shop) {
  const response = await api.get(origin + path + '/csrf');
  expect(response.ok()).toBeTruthy();
  return { 'X-CART-CSRF': (await response.json()).token };
}
test('guest cart mutations require CSRF and owner cookie; versions prevent overwrites', async ({
  playwright,
}) => {
  for (const origin of [shop, 'http://127.0.0.1:5104']) {
    const a = await playwright.request.newContext();
    const b = await playwright.request.newContext();
    try {
      expect((await a.get(origin + path)).status()).toBe(204);
      expect((await a.post(origin + path + '/session')).status()).toBe(400);
      const csrf = await headers(a, origin);
      const created = await a.post(origin + path + '/session', { headers: csrf });
      expect(created.status()).toBe(200);
      const cart = await created.json();
      const lines = [{ productSlug: 'surub-test', offerId: crypto.randomUUID(), packs: 2 }];
      expect(
        (
          await a.put(origin + path, { data: { cartId: cart.id, version: cart.version, lines } })
        ).status(),
      ).toBe(400);
      const updated = await a.put(origin + path, {
        headers: csrf,
        data: { cartId: cart.id, version: cart.version, lines },
      });
      expect(updated.status()).toBe(200);
      expect(
        (
          await a.put(origin + path, {
            headers: csrf,
            data: { cartId: cart.id, version: cart.version, lines: [] },
          })
        ).status(),
      ).toBe(409);
      const otherCsrf = await headers(b, origin);
      expect(
        (
          await b.put(origin + path, {
            headers: otherCsrf,
            data: { cartId: cart.id, version: 2, lines: [] },
          })
        ).status(),
      ).toBe(401);
      expect((await b.post(origin + path + '/session', { headers: otherCsrf })).status()).toBe(200);
      expect(
        (
          await b.put(origin + path, {
            headers: otherCsrf,
            data: { cartId: cart.id, version: 1, lines: [] },
          })
        ).status(),
      ).toBe(409);
      expect((await (await a.get(origin + path)).json()).lines).toEqual(lines);
      expect((await (await b.get(origin + path)).json()).lines).toEqual([]);
      const cookie = (await a.storageState()).cookies.find((c) => c.name === 'SurubDirect.Cart')!;
      expect(cookie.httpOnly).toBe(true);
      expect(cookie.sameSite).toBe('Strict');
      expect(cookie.path).toBe('/api/v1/ordering');
      expect(JSON.stringify(cart)).not.toContain('ownerHash');
    } finally {
      await a.dispose();
      await b.dispose();
    }
  }
});
test('migrates local selections and recovers a committed write whose response was lost', async ({
  page,
  context,
}) => {
  const line = { productSlug: 'surub-migration', offerId: crypto.randomUUID(), packs: 2 };
  await page.goto(shop);
  await page.evaluate(
    (value) => localStorage.setItem('surub-direct.cart.v1', JSON.stringify([value])),
    line,
  );
  await page.goto(shop + '/cos');
  await expect(page.locator('sd-cart-line input')).toHaveValue('2');
  await expect
    .poll(() => page.evaluate(() => localStorage.getItem('surub-direct.cart.v1')))
    .toBeNull();
  const before = await (await page.request.get(shop + path)).json();
  await page.route('**/api/v1/ordering/cart', async (route) => {
    if (route.request().method() !== 'PUT') {
      await route.continue();
      return;
    }
    const response = await route.fetch();
    expect(response.status()).toBe(200);
    await route.abort('failed');
  });
  await page.locator('sd-cart-line input').fill('3');
  await page.getByRole('button', { name: 'Actualizează', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('Nu putem confirma');
  await page.unroute('**/api/v1/ordering/cart');
  await page.getByRole('button', { name: 'Reîncarcă coșul', exact: true }).click();
  await expect(page.locator('sd-cart-line input')).toHaveValue('3');
  const after = await (await page.request.get(shop + path)).json();
  expect(after.version).toBe(before.version + 1);
  const second = await context.newPage();
  await second.goto(shop + '/cos');
  await expect(second.locator('sd-cart-line input')).toHaveValue('3');
  await second.locator('sd-cart-line input').fill('4');
  await second.getByRole('button', { name: 'Actualizează', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('alt tab');
  await page.getByRole('button', { name: 'Reîncarcă coșul', exact: true }).click();
  await expect(page.locator('sd-cart-line input')).toHaveValue('4');
  await second.close();
});
test('unavailable cart storage is not shown as an empty cart', async ({ page }) => {
  await page.route('**/api/v1/ordering/cart', (route) => route.fulfill({ status: 503 }));
  await page.goto(shop + '/cos');
  await expect(page.getByRole('alert')).toContainText('Nu putem confirma');
  await expect(page.getByText('Coșul este gol.', { exact: false })).toHaveCount(0);
  await page.unroute('**/api/v1/ordering/cart');
  await page.getByRole('button', { name: 'Reîncarcă coșul', exact: true }).click();
  await expect(page.getByText('Coșul este gol.', { exact: false })).toBeVisible();
});
test('browser storage restrictions do not block the server cart', async ({ page }) => {
  await page.addInitScript(() => {
    Storage.prototype.getItem = () => {
      throw new DOMException('Blocked', 'SecurityError');
    };
  });
  await page.goto(shop + '/cos');
  await expect(page.getByText('Coșul este gol.', { exact: false })).toBeVisible();
  await expect(page.getByRole('alert')).toHaveCount(0);
});
