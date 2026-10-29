import { test, expect, type Page } from '@playwright/test';
const order = (number: number) => ({
  id: `00000000-0000-0000-0000-${String(number).padStart(12, '0')}`,
  number: `DEMO-${number}`,
  status: 'Confirmed',
  createdAt: '2026-10-29T10:00:00Z',
  currency: 'RON',
  grossTotal: 12.1,
  netTotal: 10,
  finalizedAt: null,
  customer: {
    fullName: 'Client Test',
    email: 'client@example.test',
    companyName: null,
    shippingAddress: {
      street: 'Strada Test 1',
      city: 'București',
      county: 'Sector 1',
      postalCode: '010101',
      country: 'RO',
    },
  },
  lines: [
    {
      offerId: 'offer',
      productName: 'Șurub test',
      sku: 'TEST',
      offerLabel: 'Cutie',
      packs: 1,
      grossTotal: 12.1,
    },
  ],
});
const session = async (page: Page) => {
  await page.route('**/api/auth/me', (route) =>
    route.fulfill({ json: { id: 'account-A', email: 'account-a@example.test', isAdmin: false } }),
  );
  await page.route('**/api/auth/csrf', (route) => route.fulfill({ json: { token: 'test-token' } }));
};

test('history failures stay separate and can be retried without false empty results', async ({
  page,
}) => {
  await session(page);
  let failed = true;
  await page.route('**/api/v1/ordering/account/orders?**', (route) =>
    route.fulfill(
      failed ? { status: 503, json: {} } : { json: { items: [], page: 1, total: 0, pageSize: 25 } },
    ),
  );
  await page.route('**/api/v1/ordering/demo-orders?**', (route) =>
    route.fulfill({ json: { items: [order(1)], page: 1, total: 1, pageSize: 25 } }),
  );
  await page.goto('/cont');
  const account = page.getByRole('region', { name: 'Comenzile contului', exact: true });
  const browser = page.getByRole('region', {
    name: 'Comenzile neasociate din browser',
    exact: true,
  });
  await expect(account.getByRole('alert')).toBeVisible();
  await expect(account.getByText('Nu există comenzi în această pagină.')).toHaveCount(0);
  await expect(browser.getByRole('heading', { name: 'DEMO-1', exact: true })).toBeVisible();
  await expect(browser.getByRole('button', { name: 'Asociază DEMO-1' })).toHaveCount(0);
  failed = false;
  await page.getByRole('button', { name: 'Actualizează istoricul', exact: true }).click();
  await expect(account.getByText('Nu există comenzi în această pagină.')).toBeVisible();
  await expect(browser.getByRole('button', { name: 'Asociază DEMO-1' })).toBeVisible();
});

test('account and browser histories preserve independent pagination', async ({ page }) => {
  await session(page);
  for (const kind of ['account/orders', 'demo-orders']) {
    await page.route(`**/api/v1/ordering/${kind}?**`, (route) => {
      const url = new URL(route.request().url());
      if (kind === 'account/orders')
        expect(url.searchParams.get('expectedAccountId')).toBe('account-A');
      const current = Number(url.searchParams.get('page'));
      return route.fulfill({
        json: {
          items: [order((kind === 'account/orders' ? 0 : 10) + current)],
          page: current,
          total: 2,
          pageSize: 1,
        },
      });
    });
  }
  await page.goto('/cont');
  const account = page.getByRole('region', { name: 'Comenzile contului', exact: true });
  const browser = page.getByRole('region', {
    name: 'Comenzile neasociate din browser',
    exact: true,
  });
  await account.getByRole('button', { name: 'Pagina următoare' }).click();
  await expect(account.getByRole('heading', { name: 'DEMO-2', exact: true })).toBeVisible();
  await expect(browser.getByRole('heading', { name: 'DEMO-11', exact: true })).toBeVisible();
  await browser.getByRole('button', { name: 'Pagina următoare' }).click();
  await expect(browser.getByRole('heading', { name: 'DEMO-12', exact: true })).toBeVisible();
  await expect(account.getByRole('heading', { name: 'DEMO-2', exact: true })).toBeVisible();
});

test('confirmation binds displayed account and hides stale history after session change', async ({
  page,
}) => {
  await session(page);
  let changed = false;
  let writes = 0;
  await page.route('**/api/v1/ordering/account/orders?**', (route) =>
    route.fulfill(
      changed
        ? {
            status: 409,
            json: { detail: 'Contul autentificat s-a schimbat. Reîncarcă pagina contului.' },
          }
        : { json: { items: [], page: 1, total: 0, pageSize: 25 } },
    ),
  );
  await page.route('**/api/v1/ordering/demo-orders?**', (route) =>
    route.fulfill({ json: { items: [order(1)], page: 1, total: 1, pageSize: 25 } }),
  );
  await page.route('**/api/v1/ordering/account/orders/*/claim?**', (route) => {
    expect(new URL(route.request().url()).searchParams.get('expectedAccountId')).toBe('account-A');
    writes++;
    changed = true;
    return route.fulfill({
      status: 409,
      json: { detail: 'Contul autentificat s-a schimbat. Reîncarcă pagina contului.' },
    });
  });
  await page.goto('/cont');
  await page.getByRole('button', { name: 'Asociază DEMO-1' }).click();
  const confirmation = page.getByRole('region', { name: 'Confirmă asocierea', exact: true });
  await expect(confirmation).toContainText('account-a@example.test');
  expect(writes).toBe(0);
  await confirmation.getByRole('button', { name: 'Confirmă asocierea', exact: true }).click();
  const account = page.getByRole('region', { name: 'Comenzile contului', exact: true });
  await expect(account.getByRole('alert')).toContainText('Contul autentificat s-a schimbat');
  expect(writes).toBe(1);
  await expect(page.getByRole('button', { name: 'Asociază DEMO-1' })).toHaveCount(0);
  await page.route('**/api/auth/logout', (route) => route.fulfill({ status: 204 }));
  await page.getByRole('button', { name: 'Deconectare', exact: true }).click();
  await expect(page.getByRole('region', { name: 'Istoricul comenzilor', exact: true })).toHaveCount(
    0,
  );
});
