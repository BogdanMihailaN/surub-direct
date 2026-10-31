import { expect, Page, test } from '@playwright/test';
import { PreparedCart } from '../../src/apps/storefront/app/features/cart/models/prepared-cart.model';
import { StoredCart } from '../../src/apps/storefront/app/features/cart/models/stored-cart.model';
const base = '**/api/v1/ordering/account/cart';
async function setup(page: Page) {
  const customer = {
    customerType: 'person' as const,
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
  const state = {
    cart: {
      id: 'cart-one',
      version: 1,
      updatedAt: '2026-10-30T10:00:00Z',
      expiresAt: '2026-11-29T10:00:00Z',
      preparedId: null,
      customer,
      lines: [{ productSlug: 'surub-demo', offerId: 'offer-one', packs: 2 }],
    } as StoredCart,
    prep: {
      id: 'prep-one',
      status: 'Active',
      cancellationRequested: false,
      confirmationRequested: false,
      orderNumber: null,
      checkedAt: '2026-10-30T10:00:00Z',
      deadline: '2026-11-01T10:00:00Z',
      remoteExpiresAt: '2026-11-01T10:00:00Z',
      currency: 'RON',
      netTotal: 10,
      grossTotal: 12.1,
      customer,
      lines: [
        {
          offerId: 'offer-one',
          productName: 'Șurub demo',
          sku: 'DEMO',
          offerLabel: 'Pungă 5',
          packs: 2,
          unitGross: 6.05,
          netTotal: 10,
          grossTotal: 12.1,
        },
      ],
    } as PreparedCart,
    unavailable: false,
    changed: false,
    writes: [] as string[],
  };
  await page.route('**/api/auth/me', (r) =>
    r.fulfill({ json: { id: 'account-one', email: 'demo@example.test', isAdmin: false } }),
  );
  await page.route('**/api/auth/csrf', (r) => r.fulfill({ json: { token: 'csrf' } }));
  await page.route('**/api/v1/ordering/capabilities', (r) =>
    r.fulfill({ json: { preparationEnabled: true } }),
  );
  await page.route('**/api/v1/ordering/cart', (r) => r.fulfill({ status: 204 }));
  await page.route('**/api/v1/ordering/cart/quote', (r) =>
    r.fulfill({ json: { lines: [], grossTotal: null } }),
  );
  await page.route('**/api/v1/ordering/account/orders?*', (r) =>
    r.fulfill({ json: { items: [], page: 1, total: 0, pageSize: 25 } }),
  );
  await page.route('**/api/v1/ordering/demo-orders?*', (r) =>
    r.fulfill({ json: { items: [], page: 1, total: 0, pageSize: 25 } }),
  );
  await page.route(base + '?*', (r) =>
    r.fulfill(
      state.changed
        ? { status: 409, json: { detail: 'Contul autentificat s-a schimbat.' } }
        : { json: state.cart },
    ),
  );
  await page.route(base + '/preparation**', async (r) => {
    expect(new URL(r.request().url()).searchParams.get('expectedAccountId')).toBe('account-one');
    if (r.request().method() === 'GET')
      return r.fulfill(state.unavailable ? { status: 503, json: {} } : { json: state.prep });
    expect(r.request().headers()['x-csrf-token']).toBe('csrf');
    const action = new URL(r.request().url()).pathname.split('/').pop()!;
    state.writes.push(action);
    if (state.changed)
      return r.fulfill({ status: 409, json: { detail: 'Contul autentificat s-a schimbat.' } });
    if (action === 'preparation') {
      state.cart = { ...state.cart, preparedId: state.prep.id, version: 2 };
      return r.abort(); // Mutation committed; reply lost.
    }
    if (action === 'confirm') {
      state.prep = {
        ...state.prep,
        confirmationRequested: true,
        status: 'Consumed',
        orderNumber: 'DEMO-123',
      };
      return r.abort();
    }
    if (action === 'complete')
      state.cart =
        state.prep.status === 'Expired'
          ? { ...state.cart, preparedId: null }
          : { ...state.cart, preparedId: null, lines: [], customer: null };
    if (action === 'cancel') {
      if (state.prep.status === 'Released') state.cart = { ...state.cart, preparedId: null };
      else state.prep = { ...state.prep, status: 'Released', cancellationRequested: true };
    }
    return r.fulfill({ status: 204 });
  });
  return state;
}
test('lost preparation and confirmation responses reconcile without duplicate writes', async ({
  page,
}) => {
  const state = await setup(page);
  await page.goto('/cont');
  const section = page.getByRole('region', { name: 'Rezervarea coșului de cont' });
  await section.getByRole('button', { name: 'Pregătește coșul contului' }).click();
  await expect(section.getByRole('status')).toHaveText('Stoc rezervat');
  await expect(page.locator('sd-account-cart sd-cart-line input')).toBeDisabled();
  await section.getByRole('button', { name: 'Continuă la confirmare' }).click();
  const confirm = section.getByRole('group', { name: 'Confirmare comandă pe cont' });
  await expect(confirm).toBeFocused();
  await expect(confirm).toContainText('demo@example.test');
  await confirm.getByRole('button', { name: 'Renunță la confirmare' }).click();
  expect(state.writes).toEqual(['preparation']);
  await section.getByRole('button', { name: 'Continuă la confirmare' }).click();
  await confirm.getByRole('button', { name: 'Confirmă comanda contului' }).click();
  await expect(section.getByRole('status')).toHaveText('Comandă confirmată');
  await expect(section).toContainText('DEMO-123');
  await section.getByRole('button', { name: 'Golește coșul finalizat' }).click();
  await expect(page.getByText('Coșul contului este gol.', { exact: true })).toBeVisible();
  expect(state.writes).toEqual(['preparation', 'confirm', 'complete']);
});
test('unavailable preparation hides stale actions and cancellation preserves selection', async ({
  page,
}) => {
  const state = await setup(page);
  state.cart.preparedId = state.prep.id;
  await page.goto('/cont');
  const section = page.getByRole('region', { name: 'Rezervarea coșului de cont' });
  await expect(section.getByRole('status')).toHaveText('Stoc rezervat');
  state.unavailable = true;
  await section.getByRole('button', { name: 'Actualizează rezervarea contului' }).click();
  await expect(section.getByRole('alert')).toContainText('Nu putem confirma');
  await expect(section.getByRole('button', { name: 'Continuă la confirmare' })).toHaveCount(0);
  state.unavailable = false;
  await section.getByRole('button', { name: 'Actualizează rezervarea contului' }).click();
  await section.getByRole('button', { name: 'Anulează rezervarea contului' }).click();
  await section.getByRole('button', { name: 'Reia editarea coșului de cont' }).click();
  await expect(page.locator('sd-account-cart sd-cart-line input')).toBeEnabled();
  expect(state.cart.lines).toHaveLength(1);
  expect(state.cart.customer).not.toBeNull();
});
test('account switch rejects confirmation and removes the previous snapshot', async ({ page }) => {
  const state = await setup(page);
  state.cart.preparedId = state.prep.id;
  await page.goto('/cont');
  await page.getByRole('button', { name: 'Continuă la confirmare' }).click();
  state.changed = true;
  await page.getByRole('button', { name: 'Confirmă comanda contului' }).click();
  await expect(page.locator('sd-account-cart')).toContainText('Contul autentificat s-a schimbat.');
  await expect(page.locator('sd-account-preparation')).toHaveCount(0);
  expect(state.writes).toEqual(['confirm']);
});
test('another tab invalidates the confirmation before any mutation', async ({ page }) => {
  const state = await setup(page);
  state.cart.preparedId = state.prep.id;
  await page.goto('/cont');
  await page.getByRole('button', { name: 'Continuă la confirmare' }).click();
  await page.evaluate(() =>
    window.dispatchEvent(
      new StorageEvent('storage', {
        key: 'surub-direct.account-cart.changed',
        newValue: 'changed',
      }),
    ),
  );
  await expect(page.getByRole('group', { name: 'Confirmare comandă pe cont' })).toHaveCount(0);
  expect(state.writes).toEqual([]);
});

test('failed confirmation resumes editing without clearing the selection', async ({ page }) => {
  const state = await setup(page);
  state.cart.preparedId = state.prep.id;
  state.prep.status = 'Expired';
  state.prep.confirmationRequested = true;
  await page.goto('/cont');
  await expect(page.locator('sd-account-preparation')).toContainText(
    'Selecția și datele clientului vor fi păstrate.',
  );
  await page.getByRole('button', { name: 'Reia coșul după eșec' }).click();
  await expect(page.locator('sd-account-cart sd-cart-line input')).toBeEnabled();
  expect(state.cart.lines).toHaveLength(1);
  expect(state.cart.customer).not.toBeNull();
  expect(state.writes).toEqual(['complete']);
});
