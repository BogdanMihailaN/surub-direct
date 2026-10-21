import { test, expect, APIRequestContext } from '@playwright/test';
const shop = 'http://localhost:4200';
const path = '/api/v1/ordering/cart';
const person = {
  customerType: 'person',
  fullName: 'Client Demo',
  email: 'client@example.test',
  phone: '+40 700 000 000',
  companyName: null,
  taxId: null,
  registrationNumber: null,
  shippingAddress: {
    street: 'Strada Exemplu 1',
    city: 'București',
    county: 'Sector 1',
    postalCode: '010101',
    country: 'RO',
  },
  billingSameAsShipping: true,
  billingAddress: null,
};
async function seed(api: APIRequestContext, origin = shop) {
  const csrf = (await (await api.get(origin + path + '/csrf')).json()).token;
  const headers = { 'X-CART-CSRF': csrf };
  const created = await api.post(origin + path + '/session', { headers });
  expect(created.status()).toBe(200);
  const cart = await created.json();
  const updated = await api.put(origin + path, {
    headers,
    data: {
      cartId: cart.id,
      version: cart.version,
      lines: [{ productSlug: 'surub-demo', offerId: crypto.randomUUID(), packs: 1 }],
    },
  });
  expect(updated.status()).toBe(200);
  return { cart: await updated.json(), headers };
}
test('PF/PJ draft persists, supports distinct billing, clears personal fields and remains a demo', async ({
  page,
}) => {
  await seed(page.request);
  await page.goto(shop + '/cos');
  const form = page.locator('sd-customer-form');
  await form.getByRole('button', { name: 'Salvează ciorna' }).click();
  await expect(form.getByRole('alert')).toContainText('Completează');
  await form.getByLabel('Nume complet / persoană de contact').fill(person.fullName);
  await form.getByLabel('Email', { exact: true }).fill(person.email);
  await form.getByLabel('Telefon', { exact: true }).fill(person.phone);
  const shipping = form.getByRole('group', { name: 'Adresă de livrare', exact: true });
  await shipping.getByLabel('Stradă și număr').fill(person.shippingAddress.street);
  await shipping.getByLabel('Localitate').fill(person.shippingAddress.city);
  await shipping.getByLabel('Județ / sector').fill(person.shippingAddress.county);
  await shipping.getByLabel('Cod poștal').fill(person.shippingAddress.postalCode);
  await form.getByRole('button', { name: 'Salvează ciorna' }).click();
  await expect(form.getByRole('status')).toContainText('Datele au fost salvate');
  await page.reload();
  await expect(form.getByLabel('Email', { exact: true })).toHaveValue(person.email);
  await form.getByLabel('Tip client').selectOption('company');
  await form.getByLabel('Denumire firmă').fill('Firma Demonstrativă');
  await form.getByLabel('CUI', { exact: true }).fill('ro123456');
  await form.getByLabel('Adresa de facturare este aceeași').uncheck();
  const billing = form.getByRole('group', { name: 'Adresă de facturare', exact: true });
  await billing.getByLabel('Stradă și număr').fill('Strada Facturare 2');
  await billing.getByLabel('Localitate').fill('Cluj-Napoca');
  await billing.getByLabel('Județ / sector').fill('Cluj');
  await billing.getByLabel('Cod poștal').fill('400001');
  await form.getByRole('button', { name: 'Salvează ciorna' }).click();
  await expect(form.getByRole('status')).toContainText('Datele au fost salvate');
  await page.reload();
  await expect(form.getByLabel('CUI', { exact: true })).toHaveValue('RO123456');
  await expect(billing.getByLabel('Localitate')).toHaveValue('Cluj-Napoca');
  await page.setViewportSize({ width: 390, height: 844 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await form.screenshot({ path: '../work/customer-mobile.png' });
  expect(await page.evaluate(() => JSON.stringify(localStorage))).not.toContain(person.email);
  expect(await page.evaluate(() => document.cookie)).not.toContain('SurubDirect.Cart=');
  await form.getByLabel('Tip client').selectOption('person');
  await form.getByLabel('Adresa de facturare este aceeași').check();
  await form.getByRole('button', { name: 'Salvează ciorna' }).click();
  await expect(form.getByRole('status')).toContainText('Datele au fost salvate');
  const saved = (await (await page.request.get(shop + path)).json()).customer;
  expect(saved.companyName).toBeNull();
  expect(saved.taxId).toBeNull();
  expect(saved.billingAddress).toBeNull();
  await form.getByRole('button', { name: 'Șterge datele clientului' }).click();
  await expect(form.getByRole('status')).toContainText('au fost șterse');
  await page.reload();
  await expect(form.getByLabel('Email', { exact: true })).toHaveValue('');
  expect((await (await page.request.get(shop + path)).json()).customer).toBeNull();
  expect(
    (await (await page.request.get(shop + '/api/v1/ordering/capabilities')).json()).checkoutEnabled,
  ).toBe(false);
});
test('customer writes enforce owner, CSRF, validation and cart version through gateway and direct API', async ({
  playwright,
}) => {
  for (const origin of [shop, 'http://127.0.0.1:5104']) {
    const api = await playwright.request.newContext();
    const other = await playwright.request.newContext();
    try {
      const { cart, headers } = await seed(api, origin);
      const data = { cartId: cart.id, version: cart.version, customer: person };
      expect((await api.put(origin + path + '/customer', { data })).status()).toBe(400);
      const own = await api.put(origin + path + '/customer', { headers, data });
      expect(own.status()).toBe(200);
      expect(own.headers()['cache-control']).toBe('no-store');
      expect((await api.put(origin + path + '/customer', { headers, data })).status()).toBe(409);
      const otherCart = await seed(other, origin);
      expect(
        (
          await other.put(origin + path + '/customer', { headers: otherCart.headers, data })
        ).status(),
      ).toBe(409);
      const saved = await own.json();
      expect(
        (
          await api.put(origin + path + '/customer', {
            headers,
            data: { ...data, version: saved.version, customer: { ...person, email: 'invalid' } },
          })
        ).status(),
      ).toBe(400);
      expect((await (await api.get(origin + path)).json()).customer).toEqual(person);
      expect((await (await other.get(origin + path)).json()).customer).toBeNull();
      expect(
        (
          await api.put(origin + path, {
            headers,
            data: { cartId: cart.id, version: saved.version, lines: [] },
          })
        ).status(),
      ).toBe(200);
      expect((await (await api.get(origin + path)).json()).customer).toBeNull();
    } finally {
      await api.dispose();
      await other.dispose();
    }
  }
});
test('lost customer save response recovers server draft without replaying the write', async ({
  page,
}) => {
  const { cart, headers } = await seed(page.request);
  const saved = await page.request.put(shop + path + '/customer', {
    headers,
    data: { cartId: cart.id, version: cart.version, customer: person },
  });
  expect(saved.status()).toBe(200);
  const before = await saved.json();
  await page.goto(shop + '/cos');
  const form = page.locator('sd-customer-form');
  await expect(form.getByLabel('Email', { exact: true })).toHaveValue(person.email);
  await form.getByLabel('Email', { exact: true }).fill('changed@example.test');
  await page.route('**/api/v1/ordering/cart/customer', async (route) => {
    expect((await route.fetch()).status()).toBe(200);
    await route.abort('failed');
  });
  await form.getByRole('button', { name: 'Salvează ciorna' }).click();
  await expect(page.getByRole('alert')).toContainText('Nu putem confirma');
  await page.unroute('**/api/v1/ordering/cart/customer');
  await page.getByRole('button', { name: 'Reîncarcă coșul', exact: true }).click();
  await expect(form.getByLabel('Email', { exact: true })).toHaveValue('changed@example.test');
  expect((await (await page.request.get(shop + path)).json()).version).toBe(before.version + 1);
});
