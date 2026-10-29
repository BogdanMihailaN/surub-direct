import { expect, test } from '@playwright/test';

const cart = {
  id: 'b3f57901-4d18-40a3-8fbc-5ba13bdad519',
  version: 1,
  updatedAt: '2026-10-29T10:00:00Z',
  expiresAt: '2026-11-28T10:00:00Z',
  preparedId: null,
  customer: null,
  lines: [{ productSlug: 'surub-test', offerId: 'c2751663-3825-414b-8dbe-6edece545d80', packs: 2 }],
};
const path = '**/api/v1/ordering/cart';

test('focus revalidates the cart and a failed read hides the old selection', async ({ page }) => {
  let available = true;
  let packs = 2;
  await page.route(path, (route) =>
    route.fulfill(
      available
        ? { json: { ...cart, lines: [{ ...cart.lines[0], packs }] } }
        : { status: 503, json: {} },
    ),
  );
  await page.goto('/cos');
  await expect(page.locator('sd-cart-line input')).toHaveValue('2');
  available = false;
  await page.evaluate(() => window.dispatchEvent(new Event('focus')));
  await expect(page.getByRole('alert')).toContainText('Nu putem confirma starea coșului');
  await expect(page.locator('sd-cart-line')).toHaveCount(0);
  await expect(page.getByText('Coșul este gol.', { exact: false })).toHaveCount(0);
  available = true;
  packs = 4;
  await page.getByRole('button', { name: 'Reîncarcă coșul', exact: true }).click();
  await expect(page.locator('sd-cart-line input')).toHaveValue('4');
});

test('a read invalidated by another tab cannot unlock the cart or restore old data', async ({
  page,
}) => {
  let delay = false;
  let pending = false;
  let release!: () => void;
  const gate = new Promise<void>((resolve) => {
    release = resolve;
  });
  await page.route(path, async (route) => {
    if (delay) {
      pending = true;
      await gate;
    }
    await route.fulfill({ json: cart });
  });
  await page.goto('/cos');
  await expect(page.locator('sd-cart-line input')).toHaveValue('2');
  delay = true;
  await page.evaluate(() => window.dispatchEvent(new Event('focus')));
  await expect.poll(() => pending).toBe(true);
  await page.evaluate(() =>
    window.dispatchEvent(
      new StorageEvent('storage', {
        key: 'surub-direct.cart.changed',
        newValue: 'another-tab',
      }),
    ),
  );
  release();
  await expect(page.getByRole('button', { name: 'Reîncarcă coșul', exact: true })).toBeEnabled();
  await expect(page.getByRole('alert')).toContainText('alt tab');
  await expect(page.locator('sd-cart-line')).toHaveCount(0);
  delay = false;
  await page.getByRole('button', { name: 'Reîncarcă coșul', exact: true }).click();
  await expect(page.locator('sd-cart-line input')).toHaveValue('2');
});

test('focus during a committed write requires reconciliation instead of accepting a late response', async ({
  page,
}) => {
  let saved = cart;
  let pending = false;
  let release!: () => void;
  const gate = new Promise<void>((resolve) => {
    release = resolve;
  });
  await page.route('**/api/v1/ordering/cart/csrf', (route) =>
    route.fulfill({ json: { token: 'test' } }),
  );
  await page.route(path, async (route) => {
    if (route.request().method() === 'PUT') {
      saved = { ...cart, version: 2, lines: route.request().postDataJSON().lines };
      pending = true;
      await gate;
    }
    await route.fulfill({ json: saved });
  });
  await page.goto('/cos');
  await expect(page.locator('sd-cart-line input')).toHaveValue('2');
  await page.locator('sd-cart-line input').fill('3');
  await page.getByRole('button', { name: 'Actualizează', exact: true }).click();
  await expect.poll(() => pending).toBe(true);
  await page.evaluate(() => window.dispatchEvent(new Event('focus')));
  release();
  await expect(page.getByRole('button', { name: 'Reîncarcă coșul', exact: true })).toBeEnabled();
  await expect(page.locator('sd-cart-line')).toHaveCount(0);
  await expect(page.getByText('Coșul este gol.', { exact: false })).toHaveCount(0);
  await page.getByRole('button', { name: 'Reîncarcă coșul', exact: true }).click();
  await expect(page.locator('sd-cart-line input')).toHaveValue('3');
});

test('login and logout revalidate the existing browser cart without a page reload', async ({
  page,
}) => {
  let authenticated = false;
  let reads = 0;
  await page.route(path, (route) => {
    reads++;
    return route.fulfill({ json: cart });
  });
  await page.route('**/api/auth/csrf', (route) => route.fulfill({ json: { token: 'test' } }));
  await page.route('**/api/auth/me', (route) =>
    route.fulfill(
      authenticated
        ? { json: { id: 'account-test', email: 'test@example.test', isAdmin: false } }
        : { status: 401, json: {} },
    ),
  );
  await page.route('**/api/auth/login', (route) => {
    authenticated = true;
    return route.fulfill({ status: 204 });
  });
  await page.route('**/api/auth/logout', (route) => {
    authenticated = false;
    return route.fulfill({ status: 204 });
  });
  await page.route('**/api/v1/ordering/account/orders?*', (route) =>
    route.fulfill({ json: { items: [], total: 0, page: 1, pageSize: 25 } }),
  );
  await page.goto('/cos');
  await expect(page.locator('sd-cart-line input')).toHaveValue('2');
  await page.getByRole('link', { name: 'Contul meu', exact: true }).click();
  await expect(page.getByRole('group', { name: 'Autentificare', exact: true })).toBeVisible();
  const beforeLogin = reads;
  await page.getByLabel('Adresă e-mail', { exact: true }).fill('test@example.test');
  await page.getByLabel('Parolă', { exact: true }).fill('Account-test-123!');
  await page.getByRole('button', { name: 'Autentificare', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Ești autentificat' })).toBeVisible();
  await expect.poll(() => reads).toBeGreaterThan(beforeLogin);
  const beforeLogout = reads;
  await page.getByRole('button', { name: 'Deconectare', exact: true }).click();
  await expect.poll(() => reads).toBeGreaterThan(beforeLogout);
  await page.getByRole('link', { name: 'Coș (1)', exact: true }).click();
  await expect(page.locator('sd-cart-line input')).toHaveValue('2');
});
