import { expect, Page, test } from '@playwright/test';
import { StoredCart } from '../../src/apps/storefront/app/features/cart/models/stored-cart.model';
async function setup(page: Page) {
  const state = {
    cart: {
      id: 'cart-one',
      version: 4,
      customer: null,
      preparedId: null,
      lines: [
        { productSlug: 'other', offerId: 'other-offer', packs: 5 },
        { productSlug: 'demo', offerId: 'offer-one', packs: 2 },
      ],
      updatedAt: '2026-10-31T10:00:00Z',
      expiresAt: '2026-11-30T10:00:00Z',
    } as StoredCart | null,
    writes: 0,
    creates: 0,
    guestWrites: 0,
    mode: '',
    readStatus: 200,
    authStatus: 200,
  };
  await page.route('**/api/v1/products/demo', (r) =>
    r.fulfill({
      json: {
        product: {
          id: 'product',
          name: 'Șurub Demo',
          slug: 'demo',
          category: 'Șuruburi',
          description: 'Demo',
          images: [],
          variants: [
            {
              id: 'variant',
              sku: 'DEMO',
              label: 'M6',
              material: 'Oțel',
              finish: 'Zincat',
              attributes: [],
              offers: [
                { id: 'offer-one', label: 'Pungă 5', unitsPerPack: 5, minPacks: 1, packStep: 1 },
              ],
            },
          ],
        },
        prices: [{ offerId: 'offer-one', netAmount: 5, grossAmount: 6.05 }],
        stock: [{ variantId: 'variant', available: 100 }],
      },
    }),
  );
  await page.route('**/api/v1/ordering/cart', (r) => {
    if (r.request().method() !== 'GET') state.guestWrites++;
    return r.fulfill({ status: 204 });
  });
  await page.route('**/api/auth/me', (r) =>
    r.fulfill({
      status: state.authStatus,
      json:
        state.authStatus === 200
          ? { id: 'account-one', email: 'demo@example.test', isAdmin: false }
          : {},
    }),
  );
  await page.route('**/api/auth/csrf', (r) => r.fulfill({ json: { token: 'csrf' } }));
  await page.route('**/api/v1/ordering/account/cart/session?*', (r) => {
    state.creates++;
    state.cart = {
      id: 'new-cart',
      version: 1,
      lines: [],
      customer: null,
      preparedId: null,
      updatedAt: '',
      expiresAt: '',
    };
    return r.fulfill({ json: state.cart });
  });
  await page.route('**/api/v1/ordering/account/cart?*', (r) => {
    expect(new URL(r.request().url()).searchParams.get('expectedAccountId')).toBe('account-one');
    if (r.request().method() === 'GET')
      return r.fulfill(
        state.readStatus !== 200
          ? { status: state.readStatus, json: {} }
          : state.cart
            ? { json: state.cart }
            : { status: 204 },
      );
    expect(r.request().headers()['x-csrf-token']).toBe('csrf');
    state.writes++;
    const data = r.request().postDataJSON();
    expect(data.version).toBe(state.cart!.version);
    if (state.mode === 'conflict') {
      state.cart = {
        ...state.cart!,
        version: 5,
        lines: [
          ...state.cart!.lines.filter((x) => x.offerId !== 'offer-one'),
          { productSlug: 'demo', offerId: 'offer-one', packs: 7 },
        ],
      };
      return r.fulfill({ status: 409, json: { detail: 'Coșul s-a modificat.' } });
    }
    if (state.mode === 'switch') {
      state.readStatus = 409;
      return r.fulfill({ status: 409, json: { detail: 'Contul autentificat s-a schimbat.' } });
    }
    state.cart = { ...state.cart!, version: state.cart!.version + 1, lines: data.lines };
    return state.mode === 'lost' ? r.abort() : r.fulfill({ json: state.cart });
  });
  return state;
}
test('explicit account confirmation preserves other lines and recovers a lost response', async ({
  page,
}) => {
  const state = await setup(page);
  state.mode = 'lost';
  await page.goto('/produse/demo');
  await page.getByLabel('Număr ambalaje').fill('3');
  await page.getByRole('button', { name: 'Adaugă în cont', exact: true }).click();
  const confirm = page.getByRole('group', { name: 'Confirmare adăugare în cont' });
  await expect(confirm).toBeFocused();
  await expect(confirm).toContainText('demo@example.test');
  await expect(confirm).toContainText('Înlocuiește cantitatea existentă de 2');
  await confirm.getByRole('button', { name: 'Renunță', exact: true }).click();
  expect(state.writes).toBe(0);
  await page.getByRole('button', { name: 'Adaugă în cont', exact: true }).click();
  await page.getByRole('button', { name: 'Confirmă cantitatea în cont' }).click();
  await expect(page.locator('sd-add-to-account')).toContainText('Serverul indică 3 ambalaje');
  expect(state.cart!.lines).toEqual([
    { productSlug: 'other', offerId: 'other-offer', packs: 5 },
    { productSlug: 'demo', offerId: 'offer-one', packs: 3 },
  ]);
  expect(state.writes).toBe(1);
  expect(state.guestWrites).toBe(0);
});
test('a missing account cart is created only after confirmation', async ({ page }) => {
  const state = await setup(page);
  state.cart = null;
  await page.goto('/produse/demo');
  await page.getByRole('button', { name: 'Adaugă în cont', exact: true }).click();
  await expect(page.getByRole('group', { name: 'Confirmare adăugare în cont' })).toContainText(
    'Vom crea coșul',
  );
  expect(state.creates).toBe(0);
  await page.getByRole('button', { name: 'Confirmă cantitatea în cont' }).click();
  await expect(page.locator('sd-add-to-account')).toContainText('Cantitatea a fost salvată');
  expect(state.creates).toBe(1);
  expect(state.writes).toBe(1);
});
test('conflicts and a changed session never replay a write or fall back to the guest cart', async ({
  page,
}) => {
  const state = await setup(page);
  state.mode = 'conflict';
  await page.goto('/produse/demo');
  await page.getByRole('button', { name: 'Adaugă în cont', exact: true }).click();
  await page.getByRole('button', { name: 'Confirmă cantitatea în cont' }).click();
  await expect(page.locator('sd-add-to-account')).toContainText('Serverul indică 7 ambalaje');
  state.mode = 'switch';
  await page.getByRole('button', { name: 'Adaugă în cont', exact: true }).click();
  await page.getByRole('button', { name: 'Confirmă cantitatea în cont' }).click();
  await expect(page.getByRole('alert')).toContainText('Contul autentificat s-a schimbat');
  await expect(page.locator('sd-add-to-account')).not.toContainText('Serverul indică');
  expect(state.writes).toBe(2);
  expect(state.guestWrites).toBe(0);
});
test('expired, unavailable, prepared carts and unauthenticated users cannot be added to', async ({
  page,
}) => {
  const state = await setup(page);
  await page.goto('/produse/demo');
  for (const status of [410, 503]) {
    state.readStatus = status;
    await page.getByRole('button', { name: 'Adaugă în cont', exact: true }).click();
    await expect(page.getByRole('alert')).toBeVisible();
    await expect(page.getByRole('group', { name: 'Confirmare adăugare în cont' })).toHaveCount(0);
  }
  state.readStatus = 200;
  state.cart!.preparedId = 'prep-one';
  await page.getByRole('button', { name: 'Adaugă în cont', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('pregătire activă');
  state.authStatus = 401;
  await page.getByRole('button', { name: 'Adaugă în cont', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('Autentifică-te');
  expect(state.writes).toBe(0);
  expect(state.creates).toBe(0);
});
test('quantity changes and changes in another tab invalidate the confirmation', async ({
  page,
}) => {
  const state = await setup(page);
  await page.goto('/produse/demo');
  await page.getByRole('button', { name: 'Adaugă în cont', exact: true }).click();
  await page.getByLabel('Număr ambalaje').fill('4');
  await expect(page.getByRole('group', { name: 'Confirmare adăugare în cont' })).toHaveCount(0);
  await page.getByRole('button', { name: 'Adaugă în cont', exact: true }).click();
  await page.evaluate(() =>
    window.dispatchEvent(
      new StorageEvent('storage', {
        key: 'surub-direct.account-cart.changed',
        newValue: 'changed',
      }),
    ),
  );
  await expect(page.getByRole('group', { name: 'Confirmare adăugare în cont' })).toHaveCount(0);
  expect(state.writes).toBe(0);
});

test('late account reads cannot restore a confirmation invalidated by another tab', async ({
  page,
}) => {
  await setup(page);
  let release!: () => void;
  let reading = false;
  const gate = new Promise<void>((resolve) => (release = resolve));
  await page.route('**/api/v1/ordering/account/cart?*', async (route) => {
    reading = true;
    await gate;
    await route.fulfill({ status: 204 });
  });
  await page.goto('/produse/demo');
  await page.getByRole('button', { name: 'Adaugă în cont', exact: true }).click();
  await expect.poll(() => reading).toBe(true);
  await page.evaluate(() =>
    window.dispatchEvent(
      new StorageEvent('storage', {
        key: 'surub-direct.account-cart.changed',
        newValue: 'changed',
      }),
    ),
  );
  release();
  await expect(page.getByRole('button', { name: 'Adaugă în cont', exact: true })).toBeEnabled();
  await expect(page.getByRole('group', { name: 'Confirmare adăugare în cont' })).toHaveCount(0);
});

test('a cart created concurrently with products requires a new review', async ({ page }) => {
  const state = await setup(page);
  state.cart = null;
  await page.route('**/api/v1/ordering/account/cart/session?*', (route) =>
    route.fulfill({
      json: {
        id: 'cart-concurrent',
        version: 2,
        customer: null,
        preparedId: null,
        lines: [{ productSlug: 'other', offerId: 'other-offer', packs: 8 }],
      },
    }),
  );
  await page.goto('/produse/demo');
  await page.getByRole('button', { name: 'Adaugă în cont', exact: true }).click();
  await page.getByRole('button', { name: 'Confirmă cantitatea în cont' }).click();
  await expect(page.getByRole('alert')).toContainText('Coșul contului a fost modificat');
  expect(state.writes).toBe(0);
});
