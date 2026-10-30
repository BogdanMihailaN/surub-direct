import { expect, Page, test } from '@playwright/test';
import { StoredCart } from '../../src/apps/storefront/app/features/cart/models/stored-cart.model';
const accountPath = '**/api/v1/ordering/account/cart?*';
const browserPath = '**/api/v1/ordering/cart';
const empty: StoredCart = {
  id: '138d3e89-ef00-4341-8ad7-12e7b40a81bb',
  version: 1,
  updatedAt: '2026-10-30T10:00:00Z',
  expiresAt: '2026-11-29T10:00:00Z',
  preparedId: null,
  customer: null,
  lines: [],
};
const guest: StoredCart = {
  ...empty,
  id: '4db2d200-5e47-466c-aa78-4b481ee624d6',
  lines: [{ productSlug: 'surub-demo', offerId: '267d1b64-e772-4f39-b553-a07ac2647559', packs: 2 }],
};
async function session(page: Page) {
  await page.route('**/api/auth/me', (route) =>
    route.fulfill({ json: { id: 'account-one', email: 'client@example.test', isAdmin: false } }),
  );
  await page.route('**/api/auth/csrf', (route) => route.fulfill({ json: { token: 'test-csrf' } }));
  await page.route('**/api/v1/ordering/account/orders?*', (route) =>
    route.fulfill({ json: { items: [], total: 0, page: 1, pageSize: 25 } }),
  );
  await page.route('**/api/v1/ordering/demo-orders?*', (route) =>
    route.fulfill({ json: { items: [], total: 0, page: 1, pageSize: 25 } }),
  );
  await page.route('**/api/v1/ordering/cart/quote', (route) =>
    route.fulfill({
      json: {
        lines: [],
        netTotal: null,
        grossTotal: null,
        currency: 'RON',
        checkedAt: '2026-10-30T10:00:00Z',
        checkoutEnabled: false,
      },
    }),
  );
}
test('cart errors are independent and unavailable account cart is not presented as empty', async ({
  page,
}) => {
  await session(page);
  let unavailable = true;
  await page.route(accountPath, (route) =>
    route.fulfill(unavailable ? { status: 503, json: {} } : { json: empty }),
  );
  await page.route(browserPath, (route) => route.fulfill({ json: guest }));
  await page.goto('/cont');
  const cart = page.locator('sd-account-cart');
  await expect(cart.getByRole('alert')).toContainText('Coșul contului:');
  await expect(cart.getByText('Coșul contului este gol.', { exact: true })).toHaveCount(0);
  await expect(cart.getByRole('button', { name: 'Transferă în cont', exact: true })).toBeDisabled();
  unavailable = false;
  await cart.getByRole('button', { name: 'Reîncarcă coșurile' }).click();
  await expect(cart.getByText('Coșul contului este gol.', { exact: true })).toBeVisible();
  await expect(cart.getByRole('button', { name: 'Transferă în cont', exact: true })).toBeEnabled();
  await page.route(browserPath, (route) => route.fulfill({ status: 503, json: {} }));
  await cart.getByRole('button', { name: 'Reîncarcă coșurile' }).click();
  await expect(cart.getByRole('alert')).toContainText('Coșul browserului:');
  await expect(cart.getByText('Coșul browserului este gol.', { exact: true })).toHaveCount(0);
  await expect(cart.getByText('Coșul contului este gol.', { exact: true })).toBeVisible();
});

test('lost transfer response reconciles both carts and logout removes account content', async ({
  page,
}) => {
  await session(page);
  let account = empty;
  let browser = guest;
  let writes = 0;
  await page.route(accountPath, (route) => {
    expect(new URL(route.request().url()).searchParams.get('expectedAccountId')).toBe(
      'account-one',
    );
    return route.fulfill({ json: account });
  });
  await page.route(browserPath, (route) => route.fulfill({ json: browser }));
  await page.route('**/api/v1/ordering/account/cart/import?*', (route) => {
    writes++;
    expect(route.request().headers()['x-csrf-token']).toBe('test-csrf');
    expect(route.request().postDataJSON()).toEqual({
      cartId: guest.id,
      version: 1,
      targetCartId: empty.id,
      targetVersion: 1,
      includeCustomer: false,
    });
    account = { ...empty, version: 2, lines: guest.lines };
    browser = { ...guest, version: 2, lines: [] };
    return route.abort();
  });
  await page.goto('/cont');
  const cart = page.locator('sd-account-cart');
  await cart.getByRole('button', { name: 'Transferă în cont', exact: true }).click();
  const confirmation = cart.getByRole('group', { name: 'Confirmare transfer coș' });
  await expect(confirmation).toBeFocused();
  await expect(confirmation).toContainText('client@example.test');
  await confirmation.getByRole('button', { name: 'Renunță la transfer' }).click();
  expect(writes).toBe(0);
  await cart.getByRole('button', { name: 'Transferă în cont', exact: true }).click();
  await confirmation.getByRole('button', { name: 'Confirmă transferul' }).click();
  await expect(cart.getByRole('alert')).toContainText('Nu putem confirma operațiunea');
  await expect(cart.locator('sd-cart-line input')).toHaveValue('2');
  await expect(cart.getByText('Coșul browserului este gol.', { exact: true })).toBeVisible();
  expect(writes).toBe(1);
  await page.route('**/api/auth/logout', (route) => route.fulfill({ status: 204 }));
  await page.getByRole('button', { name: 'Deconectare', exact: true }).click();
  await expect(cart).toHaveCount(0);
  await expect(page.getByText('client@example.test', { exact: true })).toHaveCount(0);
});

test('a concurrent write is recovered and stale reads cannot restore invalidated cart data', async ({
  page,
}) => {
  await session(page);
  let account = { ...empty, lines: guest.lines };
  let delay = false;
  let waiting = false;
  let release!: () => void;
  const gate = new Promise<void>((resolve) => {
    release = resolve;
  });
  await page.route(accountPath, async (route) => {
    if (route.request().method() === 'PUT') {
      account = { ...account, version: 3, lines: [{ ...guest.lines[0], packs: 5 }] };
      await route.fulfill({ status: 409, json: { detail: 'Coșul s-a modificat.' } });
      return;
    }
    if (delay) {
      waiting = true;
      await gate;
    }
    await route.fulfill({ json: account });
  });
  await page.route(browserPath, (route) => route.fulfill({ status: 204 }));
  await page.goto('/cont');
  const cart = page.locator('sd-account-cart');
  await cart.locator('sd-cart-line input').fill('3');
  await cart.getByRole('button', { name: 'Actualizează', exact: true }).click();
  await expect(cart.locator('sd-cart-line input')).toHaveValue('5');
  await expect(cart.getByRole('alert')).toContainText('Coșul s-a modificat.');
  delay = true;
  await cart.getByRole('button', { name: 'Reîncarcă coșurile' }).click();
  await expect.poll(() => waiting).toBe(true);
  await page.evaluate(() =>
    window.dispatchEvent(
      new StorageEvent('storage', {
        key: 'surub-direct.account-cart.changed',
        newValue: 'another-tab',
      }),
    ),
  );
  release();
  await expect(cart.getByRole('button', { name: 'Reîncarcă coșurile' })).toBeEnabled();
  await expect(cart.getByRole('alert')).toContainText('alt tab');
  await expect(cart.locator('sd-cart-line')).toHaveCount(0);
  await expect(cart.getByText('Coșul contului este gol.', { exact: true })).toHaveCount(0);
  delay = false;
  await cart.getByRole('button', { name: 'Reîncarcă coșurile' }).click();
  await expect(cart.locator('sd-cart-line input')).toHaveValue('5');
});

test('expired account cart requires explicit confirmation before restarting', async ({ page }) => {
  await session(page);
  let expired = true;
  let writes = 0;
  await page.route(accountPath, (route) =>
    route.fulfill(
      expired ? { status: 410, json: { detail: 'Coșul a expirat.' } } : { json: empty },
    ),
  );
  await page.route(browserPath, (route) => route.fulfill({ status: 204 }));
  await page.route('**/api/v1/ordering/account/cart/session?*', (route) => {
    writes++;
    expired = false;
    return route.fulfill({ json: empty });
  });
  await page.goto('/cont');
  const cart = page.locator('sd-account-cart');
  await cart.getByRole('button', { name: 'Începe un coș de cont nou' }).click();
  const confirmation = cart.getByRole('group', { name: 'Confirmare reîncepere coș' });
  await expect(confirmation).toBeFocused();
  expect(writes).toBe(0);
  await confirmation.getByRole('button', { name: 'Renunță', exact: true }).click();
  expect(writes).toBe(0);
  await cart.getByRole('button', { name: 'Începe un coș de cont nou' }).click();
  await confirmation.getByRole('button', { name: 'Confirmă coșul nou' }).click();
  await expect(cart.getByText('Coșul contului este gol.', { exact: true })).toBeVisible();
  expect(writes).toBe(1);
});

test('a session switch rejects the displayed transfer and hides the previous account cart', async ({
  page,
}) => {
  await session(page);
  let changed = false;
  let writes = 0;
  await page.route(accountPath, (route) =>
    route.fulfill(
      changed
        ? { status: 409, json: { detail: 'Contul autentificat s-a schimbat.' } }
        : { json: empty },
    ),
  );
  await page.route(browserPath, (route) => route.fulfill({ json: guest }));
  await page.route('**/api/v1/ordering/account/cart/import?*', (route) => {
    expect(new URL(route.request().url()).searchParams.get('expectedAccountId')).toBe(
      'account-one',
    );
    writes++;
    return route.fulfill({ status: 409, json: { detail: 'Contul autentificat s-a schimbat.' } });
  });
  await page.goto('/cont');
  const cart = page.locator('sd-account-cart');
  await cart.getByRole('button', { name: 'Transferă în cont', exact: true }).click();
  changed = true;
  await cart.getByRole('button', { name: 'Confirmă transferul' }).click();
  await expect(cart.getByRole('alert').filter({ hasText: 'Coșul contului:' })).toContainText(
    'Contul autentificat s-a schimbat.',
  );
  await expect(cart.getByRole('group', { name: 'Confirmare transfer coș' })).toHaveCount(0);
  await expect(cart.getByText('Coșul contului este gol.', { exact: true })).toHaveCount(0);
  await expect(cart.getByRole('button', { name: 'Transferă în cont', exact: true })).toBeDisabled();
  expect(writes).toBe(1);
});
