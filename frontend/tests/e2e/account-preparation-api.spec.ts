import { APIRequestContext, expect, test } from '@playwright/test';
import { adminWrite, loginAdmin } from './support/admin-session';
const gateway = 'http://127.0.0.1:5099';
const accountPath = '/api/v1/ordering/account/cart';
async function headers(api: APIRequestContext) {
  return { 'X-CSRF-TOKEN': (await (await api.get(gateway + '/api/auth/csrf')).json()).token };
}
async function write(api: APIRequestContext, url: string, data: unknown = {}, method = 'POST') {
  return api.fetch(url, { method, headers: await headers(api), data });
}
const customer = {
  customerType: 'person',
  fullName: 'Client Cont Demo',
  email: 'client@example.test',
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
test('account preparation releases or consumes stock once and keeps order history private across devices', async ({
  playwright,
}) => {
  test.setTimeout(120000);
  const admin = await playwright.request.newContext();
  const owner = await playwright.request.newContext();
  const otherDevice = await playwright.request.newContext();
  const stranger = await playwright.request.newContext();
  let productPath = '';
  try {
    await loginAdmin(admin, gateway);
    const create = async (path: string, data: object) => {
      const response = await adminWrite(admin, gateway, path, data);
      expect(response.status()).toBe(200);
      return (await response.json()).id as string;
    };
    const key = crypto.randomUUID();
    const slug = 'account-preparation-' + key;
    productPath =
      '/api/v1/admin/products/' +
      (await create('/api/v1/admin/products', {
        name: 'Pregătire cont demo',
        slug,
        category: 'Verificare automată',
        description: 'Test demonstrativ cont.',
      }));
    const variant = await create(productPath + '/variants', {
      sku: 'ACC-' + key,
      label: 'M6',
      diameterMm: 6,
      lengthMm: 20,
      material: 'Oțel',
      finish: 'Zincat',
    });
    const offer = await create(productPath + '/variants/' + variant + '/offers', {
      code: 'ACC-' + key,
      label: 'Pungă 5',
      unitsPerPack: 5,
      minPacks: 1,
      packStep: 1,
    });
    expect(
      (
        await adminWrite(
          admin,
          gateway,
          '/api/v1/admin/offers/' + offer + '/price',
          { netAmount: 5, taxRate: 0.21 },
          'PUT',
        )
      ).ok(),
    ).toBe(true);
    expect(
      (
        await adminWrite(admin, gateway, '/api/v1/admin/variants/' + variant + '/receipts', {
          requestId: crypto.randomUUID(),
          quantity: 100,
          reference: 'ACC-' + key,
        })
      ).ok(),
    ).toBe(true);
    expect((await adminWrite(admin, gateway, productPath + '/publish')).ok()).toBe(true);
    const email = 'account-preparation-' + key + '@example.test';
    const password = 'Account-preparation-123!';
    expect((await write(owner, gateway + '/api/auth/register', { email, password })).status()).toBe(
      204,
    );
    expect((await write(owner, gateway + '/api/auth/login', { email, password })).status()).toBe(
      200,
    );
    const accountId = (await (await owner.get(gateway + '/api/auth/me')).json()).id;
    const query = '?expectedAccountId=' + encodeURIComponent(accountId);
    const cartUrl = gateway + accountPath;
    let cart = await (await write(owner, cartUrl + '/session' + query)).json();
    cart = await (
      await write(
        owner,
        cartUrl + query,
        {
          cartId: cart.id,
          version: cart.version,
          lines: [{ productSlug: slug, offerId: offer, packs: 2 }],
        },
        'PUT',
      )
    ).json();
    cart = await (
      await write(
        owner,
        cartUrl + '/customer' + query,
        { cartId: cart.id, version: cart.version, customer },
        'PUT',
      )
    ).json();
    let id = crypto.randomUUID();
    for (const origin of [gateway, 'http://127.0.0.1:5104']) {
      const prep = origin + accountPath + '/preparation';
      expect((await stranger.get(prep + query)).status()).toBe(401);
      expect((await owner.get(prep + '?expectedAccountId=other')).status()).toBe(409);
      for (const suffix of [
        '',
        '/' + id + '/cancel',
        '/' + id + '/confirm',
        '/' + id + '/complete',
      ]) {
        expect((await stranger.post(prep + suffix + query, { data: {} })).status()).toBe(401);
        expect((await owner.post(prep + suffix + query, { data: {} })).status()).toBe(400);
        expect(
          (
            await write(owner, prep + suffix + '?expectedAccountId=other', {
              requestId: id,
              cartId: cart.id,
              version: cart.version,
            })
          ).status(),
        ).toBe(409);
      }
    }
    const prep = cartUrl + '/preparation';
    const read = async () => await (await owner.get(prep + query)).json();
    expect(
      (
        await write(owner, prep + query, { requestId: id, cartId: cart.id, version: cart.version })
      ).status(),
    ).toBe(200);
    await expect.poll(async () => (await read()).status, { timeout: 25000 }).toBe('Active');
    const stock = async () => (await (await admin.get(gateway + productPath)).json()).stock[0];
    expect(await stock()).toMatchObject({ onHand: 100, reserved: 10, available: 90 });
    expect((await write(owner, prep + '/' + id + '/cancel' + query)).status()).toBe(204);
    await expect.poll(async () => (await read()).status, { timeout: 25000 }).toBe('Released');
    expect((await write(owner, prep + '/' + id + '/cancel' + query)).status()).toBe(204);
    expect(await stock()).toMatchObject({ onHand: 100, reserved: 0, available: 100 });
    cart = await (await owner.get(cartUrl + query)).json();
    expect(cart.preparedId).toBeNull();
    id = crypto.randomUUID();
    expect(
      (
        await write(owner, prep + query, { requestId: id, cartId: cart.id, version: cart.version })
      ).status(),
    ).toBe(200);
    await expect.poll(async () => (await read()).status, { timeout: 25000 }).toBe('Active');
    expect(
      (await write(otherDevice, gateway + '/api/auth/login', { email, password })).status(),
    ).toBe(200);
    expect((await (await otherDevice.get(prep + query)).json()).id).toBe(id);
    expect(
      (await otherDevice.storageState()).cookies.some((c) => c.name === 'SurubDirect.Cart'),
    ).toBe(false);
    const confirmed = await write(otherDevice, prep + '/' + id + '/confirm' + query);
    expect(confirmed.status()).toBe(200);
    const number = (await confirmed.json()).number;
    expect((await (await write(owner, prep + '/' + id + '/confirm' + query)).json()).number).toBe(
      number,
    );
    await expect.poll(async () => (await read()).status, { timeout: 25000 }).toBe('Consumed');
    expect(await stock()).toMatchObject({ onHand: 90, reserved: 0, available: 90 });
    const historyUrl = gateway + '/api/v1/ordering/account/orders' + query;
    const order = (await (await owner.get(historyUrl)).json()).items[0];
    expect(order).toMatchObject({ id, number, status: 'Confirmed', grossTotal: 12.1 });
    expect(
      (await (await owner.get(gateway + '/api/v1/ordering/demo-orders')).json()).items,
    ).toEqual([]);
    expect((await write(otherDevice, prep + '/' + id + '/complete' + query)).status()).toBe(204);
    expect((await write(owner, prep + '/' + id + '/complete' + query)).status()).toBe(204);
    cart = await (await owner.get(cartUrl + query)).json();
    expect(cart.lines).toEqual([]);
    expect(cart.customer).toBeNull();
    expect(cart.preparedId).toBeNull();
    expect((await (await otherDevice.get(historyUrl)).json()).items[0].number).toBe(number);
    expect((await write(owner, gateway + '/api/auth/logout')).status()).toBe(204);
    expect((await owner.get(prep + query)).status()).toBe(401);
  } finally {
    if (productPath)
      expect((await adminWrite(admin, gateway, productPath + '/archive')).ok()).toBe(true);
    await Promise.all([
      admin.dispose(),
      owner.dispose(),
      otherDevice.dispose(),
      stranger.dispose(),
    ]);
  }
});
