import { expect, Page } from '@playwright/test';
import { adminWrite, loginAdmin } from './admin-session';
const shop = 'http://localhost:4200';
const admin = 'http://localhost:4201';
const cartPath = '/api/v1/ordering/cart';
const person = {
  customerType: 'company',
  fullName: 'Client Demo',
  email: 'client@example.test',
  phone: '+40 700 000 000',
  companyName: 'Firma Demo',
  taxId: 'RO123456',
  registrationNumber: null,
  shippingAddress: {
    street: 'Strada Exemplu 1',
    city: 'București',
    county: 'Sector 1',
    postalCode: '010101',
    country: 'RO',
  },
  billingSameAsShipping: false,
  billingAddress: {
    street: 'Strada Facturare 2',
    city: 'Cluj-Napoca',
    county: 'Cluj',
    postalCode: '400001',
    country: 'RO',
  },
};
export async function createDeliveredReturnOrder(page: Page) {
  await loginAdmin(page.request, admin);
  const stamp = crypto.randomUUID();
  const write = (path: string, data: object = {}, method = 'POST') =>
    adminWrite(page.request, admin, path, data, method);
  const create = async (path: string, data: object) => {
    const r = await write(path, data);
    expect(r.status()).toBe(200);
    return (await r.json()).id as string;
  };
  const slug = 'preparation-' + stamp;
  const productId = await create('/api/v1/admin/products', {
    name: 'Pregătire demo',
    slug,
    category: 'Verificare automată',
    description: 'Verificare rezervare.',
  });
  const productPath = '/api/v1/admin/products/' + productId;
  const csrf = (await (await page.request.get(shop + cartPath + '/csrf')).json()).token;
  const headers = { 'X-CART-CSRF': csrf };
  try {
    const variantId = await create(productPath + '/variants', {
      sku: 'PREP-' + stamp,
      label: 'M6',
      diameterMm: 6,
      lengthMm: 20,
      material: 'Oțel',
      finish: 'Zincat',
    });
    const offers: string[] = [];
    for (const units of [10, 5]) {
      const id = await create(productPath + '/variants/' + variantId + '/offers', {
        code: 'PREP-' + stamp + '-' + units,
        label: 'Pungă ' + units,
        unitsPerPack: units,
        minPacks: 1,
        packStep: 1,
      });
      offers.push(id);
      expect(
        (
          await write(
            '/api/v1/admin/offers/' + id + '/price',
            { netAmount: units, taxRate: 0.21 },
            'PUT',
          )
        ).ok(),
      ).toBe(true);
    }
    expect(
      (
        await write('/api/v1/admin/variants/' + variantId + '/receipts', {
          requestId: crypto.randomUUID(),
          quantity: 100,
          reference: 'PREP-' + stamp,
        })
      ).ok(),
    ).toBe(true);
    expect((await write(productPath + '/publish')).ok()).toBe(true);
    let cart = await (await page.request.post(shop + cartPath + '/session', { headers })).json();
    cart = await (
      await page.request.put(shop + cartPath, {
        headers,
        data: {
          cartId: cart.id,
          version: cart.version,
          lines: offers.map((offerId, i) => ({ productSlug: slug, offerId, packs: i + 2 })),
        },
      })
    ).json();
    expect(
      (
        await page.request.put(shop + cartPath + '/customer', {
          headers,
          data: { cartId: cart.id, version: cart.version, customer: person },
        })
      ).status(),
    ).toBe(200);
    await page.goto(shop + '/cos');
    await expect(page.getByRole('heading', { name: 'Total cu TVA: 42,35 RON' })).toBeVisible();
    const prep = page.getByRole('region', { name: 'Rezervare demonstrativă' });
    await prep.getByLabel('Metodă de livrare').selectOption('demo-standard');
    await page.getByRole('button', { name: 'Pregătește și rezervă stocul' }).click();
    await expect(prep.getByRole('status')).toHaveText('Stoc rezervat', { timeout: 20000 });
    const prepared = await (await page.request.get(shop + cartPath + '/preparation')).json();
    expect(prepared.delivery.code).toBe('demo-standard');
    expect(prepared.grossTotal).toBe(62.35);
    await expect(prep).toContainText('Transport demonstrativ inclus');
    const confirmed = await page.request.post(
      shop + cartPath + '/preparation/' + prepared.id + '/confirm',
      { headers },
    );
    expect(confirmed.status()).toBe(200);
    const number = (await confirmed.json()).number;
    await expect
      .poll(
        async () =>
          (await (await page.request.get(shop + '/api/v1/ordering/demo-orders')).json()).items[0]
            .status,
        { timeout: 20000 },
      )
      .toBe('Confirmed');

    const orderPath = '/api/v1/admin/sales-orders/' + prepared.id;
    async function operation(path: string, expectedVersion: number, fields: object) {
      expect((await write(orderPath + path, { operationId: crypto.randomUUID(), expectedVersion, reason: 'Pregătire test retur client', ...fields })).status()).toBe(204);
    }
    await operation('/processing', 0, { target: 'Processing' });
    await operation('/processing', 1, { target: 'Ready' });
    await operation('/payment-simulation', 0, { action: 'Start' });
    await operation('/payment-simulation', 1, { action: 'Succeed' });
    await operation('/shipment', 0, { target: 'Ready' });
    await operation('/shipment', 1, { target: 'Shipped' });
    await operation('/shipment', 2, { target: 'Delivered' });
    expect((await page.request.post(shop + cartPath + '/preparation/' + prepared.id + '/complete', { headers })).status()).toBe(204);
    return { id: prepared.id as string, number, archive: () => write(productPath + '/archive') };
  } catch (error) {
    await write(productPath + '/archive');
    throw error;
  }
}
