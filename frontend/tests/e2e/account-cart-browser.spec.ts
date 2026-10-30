import { APIRequestContext, expect, test } from '@playwright/test';
const origin = 'http://localhost:4200';
const guestPath = origin + '/api/v1/ordering/cart';
async function identityHeaders(api: APIRequestContext) {
  return { 'X-CSRF-TOKEN': (await (await api.get(origin + '/api/auth/csrf')).json()).token };
}
async function login(api: APIRequestContext, email: string, password: string) {
  expect(
    (
      await api.post(origin + '/api/auth/login', {
        headers: await identityHeaders(api),
        data: { email, password },
      })
    ).status(),
  ).toBe(200);
}
test('real account cart transfers a draft, survives reload and rejects a stale edit from another browser', async ({
  page,
  browser,
}) => {
  const email = `cart-ui-${Date.now()}@example.test`;
  const password = 'Account-ui-cart-123!';
  const second = await browser.newContext();
  try {
    expect(
      (
        await page.request.post(origin + '/api/auth/register', {
          headers: await identityHeaders(page.request),
          data: { email, password },
        })
      ).status(),
    ).toBe(204);
    await login(page.request, email, password);
    const headers = {
      'X-CART-CSRF': (await (await page.request.get(guestPath + '/csrf')).json()).token,
    };
    const created = await page.request.post(guestPath + '/session', { headers });
    expect(created.status()).toBe(200);
    const cart = await created.json();
    const changed = await page.request.put(guestPath, {
      headers,
      data: {
        cartId: cart.id,
        version: cart.version,
        lines: [{ productSlug: 'surub-demo', offerId: crypto.randomUUID(), packs: 2 }],
      },
    });
    expect(changed.status()).toBe(200);
    const populated = await changed.json();
    const customer = {
      customerType: 'person',
      fullName: 'Client Transfer Demo',
      email: 'draft@example.test',
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
    expect(
      (
        await page.request.put(guestPath + '/customer', {
          headers,
          data: { cartId: populated.id, version: populated.version, customer },
        })
      ).status(),
    ).toBe(200);
    await page.goto('/cont');
    const account = page.locator('sd-account-cart');
    await account.getByRole('button', { name: 'Creează coșul contului' }).click();
    await expect(account.getByText('Coșul contului este gol.', { exact: true })).toBeVisible();
    await page.setViewportSize({ width: 390, height: 844 });
    await account.getByRole('button', { name: 'Transferă în cont', exact: true }).click();
    const confirmation = account.getByRole('group', { name: 'Confirmare transfer coș' });
    await expect(confirmation).toContainText(email);
    const include = confirmation.getByRole('checkbox');
    await expect(include).not.toBeChecked();
    await include.check();
    await page.route('**/api/v1/ordering/account/cart/import?*', async (route) => {
      const response = await route.fetch();
      expect(response.status()).toBe(200);
      await route.abort();
    });
    await confirmation.getByRole('button', { name: 'Confirmă transferul' }).click();
    await expect(account.locator('sd-cart-line input')).toHaveValue('2');
    await expect(account.getByText('Coșul browserului este gol.', { exact: true })).toBeVisible();
    await expect(
      account.getByRole('alert').filter({ hasText: 'Nu putem confirma operațiunea' }),
    ).toBeVisible();
    await expect(page.getByRole('link', { name: 'Coș (0)', exact: true })).toBeVisible();
    const source = await (await page.request.get(guestPath)).json();
    expect(source.customer).toBeNull();
    expect(source.lines).toEqual([]);
    await page.reload();
    await expect(account.locator('sd-cart-line input')).toHaveValue('2');
    await account.locator('summary').click();
    await expect(account.getByText('Client Transfer Demo', { exact: false })).toBeVisible();
    expect(
      await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth),
    ).toBe(true);
    await page.screenshot({ path: '../work/account-cart-mobile.png', fullPage: true });

    await login(second.request, email, password);
    const otherPage = await second.newPage();
    await otherPage.goto(origin + '/cont');
    const otherCart = otherPage.locator('sd-account-cart');
    await expect(otherCart.locator('sd-cart-line input')).toHaveValue('2');
    expect((await second.storageState()).cookies.some((c) => c.name === 'SurubDirect.Cart')).toBe(
      false,
    );
    await account.locator('sd-cart-line input').fill('3');
    await account.getByRole('button', { name: 'Actualizează', exact: true }).click();
    await expect(
      account.getByRole('status').filter({ hasText: 'Cantitatea a fost actualizată.' }),
    ).toBeVisible();
    await otherCart.locator('sd-cart-line input').fill('4');
    await otherCart.getByRole('button', { name: 'Actualizează', exact: true }).click();
    await expect(
      otherCart.getByRole('alert').filter({ hasText: 'Nu putem confirma operațiunea' }),
    ).toBeVisible();
    await expect(otherCart.locator('sd-cart-line input')).toHaveValue('3');
    await account.locator('summary').click();
    await account.getByRole('button', { name: 'Șterge datele clientului din cont' }).click();
    await expect(account.locator('details')).toHaveCount(0);
    await account.getByRole('button', { name: 'Elimină', exact: true }).click();
    await expect(account.getByText('Coșul contului este gol.', { exact: true })).toBeVisible();
    await page.getByRole('button', { name: 'Deconectare', exact: true }).click();
    await expect(account).toHaveCount(0);
    await expect(page.getByText('Client Transfer Demo', { exact: false })).toHaveCount(0);
  } finally {
    await second.close();
  }
});
