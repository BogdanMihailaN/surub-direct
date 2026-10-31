import { expect, Page, test } from '@playwright/test';
async function setup(page: Page) {
  const customer = {
    customerType: 'person',
    fullName: 'Client Demo',
    email: 'demo@example.test',
    phone: '+40 700 000 000',
    companyName: null,
    taxId: null,
    registrationNumber: null,
    shippingAddress: {
      street: 'Strada Test 1',
      city: 'București',
      county: 'Sector 1',
      postalCode: '010101',
      country: 'RO',
    },
    billingSameAsShipping: true,
    billingAddress: null,
  };
  const state = { customer, version: 1, writes: 0, failure: '', prepared: false };
  await page.route('**/api/auth/me', (r) =>
    r.fulfill({ json: { id: 'account-one', email: 'demo@example.test', isAdmin: false } }),
  );
  await page.route('**/api/auth/csrf', (r) => r.fulfill({ json: { token: 'csrf' } }));
  await page.route('**/api/v1/ordering/capabilities', (r) =>
    r.fulfill({ json: { preparationEnabled: true } }),
  );
  await page.route('**/api/v1/ordering/cart', (r) => r.fulfill({ status: 204 }));
  await page.route('**/api/v1/ordering/account/orders?*', (r) =>
    r.fulfill({ json: { items: [], total: 0 } }),
  );
  await page.route('**/api/v1/ordering/demo-orders?*', (r) =>
    r.fulfill({ json: { items: [], total: 0 } }),
  );
  await page.route('**/api/v1/ordering/account/cart?*', (r) =>
    r.fulfill({
      json: {
        id: 'cart-one',
        version: state.version,
        customer: state.customer,
        preparedId: state.prepared ? 'prep-one' : null,
        lines: [],
        expiresAt: '2026-11-30T10:00:00Z',
        updatedAt: '2026-10-31T10:00:00Z',
      },
    }),
  );
  await page.route('**/api/v1/ordering/account/cart/preparation?*', (r) =>
    r.fulfill({ status: 503, json: {} }),
  );
  await page.route('**/api/v1/ordering/account/cart/customer?*', async (r) => {
    state.writes++;
    expect(r.request().headers()['x-csrf-token']).toBe('csrf');
    expect(new URL(r.request().url()).searchParams.get('expectedAccountId')).toBe('account-one');
    const data = r.request().postDataJSON();
    expect(data.cartId).toBe('cart-one');
    expect(data.version).toBe(1);
    state.version++;
    if (state.failure === 'conflict') {
      state.customer = { ...customer, fullName: 'Salvat de alt dispozitiv' };
      return r.fulfill({ status: 409, json: { detail: 'Coșul s-a modificat.' } });
    }
    state.customer = data.customer;
    return r.abort();
  });
  return state;
}
test('account customer validates locally and reconciles a lost save response', async ({ page }) => {
  const state = await setup(page);
  await page.goto('/cont');
  await page.getByRole('button', { name: 'Editează datele PF/PJ și adresele' }).click();
  const editor = page.locator('sd-account-cart sd-customer-form');
  await page.setViewportSize({ width: 390, height: 844 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await editor.screenshot({ path: '../work/account-customer-mobile.png' });
  await editor.getByLabel('Email', { exact: true }).fill('invalid');
  await editor.getByRole('button', { name: 'Salvează ciorna' }).click();
  expect(state.writes).toBe(0);
  await editor.getByLabel('Email', { exact: true }).fill('changed@example.test');
  await editor.getByRole('button', { name: 'Salvează ciorna' }).click();
  await expect(editor).toHaveCount(0);
  await expect(page.locator('sd-account-cart').getByRole('alert')).toContainText(
    'Nu putem confirma',
  );
  await page.locator('sd-account-cart summary').click();
  await expect(page.locator('sd-account-cart details')).toContainText('changed@example.test');
  expect(state.writes).toBe(1);
});
test('a conflicting save displays the server draft without overwriting it again', async ({
  page,
}) => {
  const state = await setup(page);
  state.failure = 'conflict';
  await page.goto('/cont');
  await page.getByRole('button', { name: 'Editează datele PF/PJ și adresele' }).click();
  await page
    .locator('sd-customer-form')
    .getByLabel('Nume complet / persoană de contact')
    .fill('Modificare locală');
  await page.getByRole('button', { name: 'Salvează ciorna' }).click();
  await expect(page.locator('sd-account-cart').getByRole('alert')).toContainText(
    'Coșul s-a modificat',
  );
  await page.locator('sd-account-cart summary').click();
  await expect(page.locator('sd-account-cart details')).toContainText('Salvat de alt dispozitiv');
  expect(state.writes).toBe(1);
});
test('editing can be abandoned and an external cart change discards the draft', async ({
  page,
}) => {
  const state = await setup(page);
  await page.goto('/cont');
  await page.getByRole('button', { name: 'Editează datele PF/PJ și adresele' }).click();
  await page.getByRole('button', { name: 'Renunță la editarea datelor' }).click();
  await expect(page.locator('sd-customer-form')).toHaveCount(0);
  await page.getByRole('button', { name: 'Editează datele PF/PJ și adresele' }).click();
  await page.evaluate(() =>
    window.dispatchEvent(
      new StorageEvent('storage', {
        key: 'surub-direct.account-cart.changed',
        newValue: 'changed',
      }),
    ),
  );
  await expect(page.locator('sd-customer-form')).toHaveCount(0);
  expect(state.writes).toBe(0);
});
test('prepared account cart prevents editing customer details', async ({ page }) => {
  const state = await setup(page);
  state.prepared = true;
  await page.goto('/cont');
  await expect(
    page.getByRole('button', { name: 'Editează datele PF/PJ și adresele' }),
  ).toBeDisabled();
  expect(state.writes).toBe(0);
});
