import { test, expect } from '@playwright/test';
import { adminWrite, loginAdmin } from './support/admin-session';
const origin = 'http://localhost:4201';
const root = '/api/v1/admin/purchasing';
test('closing partial and full orders preserves stock and blocks further receipts', async ({
  page,
}) => {
  const key = Date.now().toString();
  await loginAdmin(page.request, origin);
  const write = (path: string, data: object = {}, method = 'POST') =>
    adminWrite(page.request, origin, path, data, method);
  const create = async (path: string, data: object) => {
    const r = await write(path, data);
    expect(r.ok()).toBeTruthy();
    return (await r.json()).id as string;
  };
  const productId = await create('/api/v1/admin/products', {
    name: 'Recepție test ' + key,
    slug: 'closure-' + key,
    category: 'Verificare automată',
    description: 'Produs demonstrativ.',
  });
  const variantId = await create(`/api/v1/admin/products/${productId}/variants`, {
    sku: 'REC-' + key,
    label: 'M6',
    diameterMm: 6,
    lengthMm: 20,
    material: 'Oțel',
    finish: 'Zincat',
  });
  const supplierId = await create(root + '/suppliers', {
    name: 'Recepții test ' + key,
    countryCode: 'RO',
    taxId: key,
    contactName: '',
    email: '',
    phone: '',
    address: '',
    notes: '',
  });
  try {
    const itemId = await create(root + `/suppliers/${supplierId}/items`, {
      variantId,
      supplierCode: 'EXT',
      packLabel: 'Cutie',
      unitsPerPack: 100,
      netCostPerPack: 10,
      currency: 'RON',
    });
    const orderId = await create(root + '/orders', { supplierId, currency: 'RON', notes: '' });
    expect(
      (
        await write(root + `/orders/${orderId}/lines`, {
          version: 1,
          supplierItemId: itemId,
          packs: 2,
        })
      ).ok(),
    ).toBeTruthy();
    expect((await write(root + `/orders/${orderId}/sent`, { version: 2 })).ok()).toBeTruthy();
    const get = async () => (await page.request.get(origin + root + '/orders/' + orderId)).json();
    let order = await get();
    const lineId = order.lines[0].id;
    const receiptId = crypto.randomUUID();
    expect(
      (
        await write(root + `/orders/${orderId}/receipts`, {
          requestId: receiptId,
          version: 3,
          lineId,
          quantity: 40,
          reference: 'REC-' + key,
        })
      ).status(),
    ).toBe(204);
    expect(
      (
        await write(root + `/orders/${orderId}/corrections`, {
          requestId: crypto.randomUUID(),
          version: 5,
          receiptId,
          quantity: 10,
          reference: 'COR-' + key,
          reason: 'Count',
        })
      ).status(),
    ).toBe(204);
    expect(
      (await write(root + `/orders/${orderId}/close`, { version: 7, reason: '' })).status(),
    ).toBe(400);
    await page.goto(origin + '/aprovizionare');
    await page.getByRole('button', { name: new RegExp('CA-' + order.number + ' ·') }).click();
    const form = page.locator('sd-order-closure');
    await expect(form).toContainText('Rest nelivrat: 170 buc.');
    await form.getByRole('checkbox').check();
    await expect(form.getByRole('button', { name: 'Închide comanda' })).toBeDisabled();
    await form.getByLabel('Motivul închiderii').fill('Furnizorul nu mai poate livra diferența.');
    const closePath = root + `/orders/${orderId}/close`;
    await page.route(
      '**' + closePath,
      async (route) => {
        expect((await route.fetch()).status()).toBe(204);
        await route.abort('failed');
      },
      { times: 1 },
    );
    await form.getByRole('button', { name: 'Închide comanda' }).click();
    await expect(page.getByRole('alert')).toBeVisible();
    await page.getByRole('button', { name: 'Reîncarcă comanda' }).click();
    await expect(form).toContainText('Comandă închisă');
    await expect(form).toContainText('170 buc.');
    await expect(page.getByRole('button', { name: 'Înregistrează recepția' })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Înregistrează corecția' })).toHaveCount(0);
    order = await get();
    expect(order.status).toBe('Closed');
    expect(order.closedAt).toBeTruthy();
    expect(order.closedBy).toBeTruthy();
    expect(order.receipts[0].quantity).toBe(40);
    expect(order.corrections[0].quantity).toBe(10);
    expect(
      (
        await write(root + `/orders/${orderId}/receipts`, {
          requestId: crypto.randomUUID(),
          version: order.version,
          lineId,
          quantity: 1,
          reference: 'NEW',
        })
      ).status(),
    ).toBe(409);
    expect(
      (
        await write(root + `/orders/${orderId}/corrections`, {
          requestId: crypto.randomUUID(),
          version: order.version,
          receiptId,
          quantity: 1,
          reference: 'NEW',
          reason: 'Count',
        })
      ).status(),
    ).toBe(409);
    expect((await write(closePath, { version: order.version, reason: 'Again' })).status()).toBe(
      409,
    );
    const product = await (
      await page.request.get(origin + '/api/v1/admin/products/' + productId)
    ).json();
    expect(product.stock[0].onHand).toBe(30);
    await page.getByRole('combobox', { name: 'Stare comandă', exact: true }).selectOption('Closed');
    await page.getByRole('button', { name: 'Aplică filtrele' }).click();
    await expect(
      page.getByRole('button', { name: new RegExp('CA-' + order.number + ' ·') }),
    ).toContainText('Închisă');
    await page.setViewportSize({ width: 1440, height: 1000 });
    await page.screenshot({ path: '../work/closure-desktop.png', fullPage: true });
    await page.setViewportSize({ width: 390, height: 844 });
    expect(
      await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
    ).toBeTruthy();
    await page.screenshot({ path: '../work/closure-mobile.png', fullPage: true });
    const full = await create(root + '/orders', { supplierId, currency: 'RON', notes: '' });
    expect(
      (
        await write(root + `/orders/${full}/lines`, {
          version: 1,
          supplierItemId: itemId,
          packs: 1,
        })
      ).status(),
    ).toBe(204);
    expect((await write(root + `/orders/${full}/sent`, { version: 2 })).status()).toBe(204);
    const fullOrder = await (await page.request.get(origin + root + '/orders/' + full)).json();
    expect(
      (
        await write(root + `/orders/${full}/receipts`, {
          requestId: crypto.randomUUID(),
          version: 3,
          lineId: fullOrder.lines[0].id,
          quantity: 100,
          reference: 'FULL',
        })
      ).status(),
    ).toBe(204);
    expect((await write(root + `/orders/${full}/close`, { version: 5, reason: '' })).status()).toBe(
      204,
    );
    expect((await (await page.request.get(origin + root + '/orders/' + full)).json()).status).toBe(
      'Closed',
    );
  } finally {
    test.setTimeout(test.info().timeout + 15000);
    const supplier = await (
      await page.request.get(origin + root + '/suppliers/' + supplierId)
    ).json();
    await write(root + '/suppliers/' + supplierId, { ...supplier, isActive: false }, 'PUT');
    await write('/api/v1/admin/products/' + productId + '/archive');
  }
});

test('closing orders require session and CSRF directly and through gateway', async ({
  request,
}) => {
  const id = '00000000-0000-0000-0000-000000000001';
  for (const status of [401, 400]) {
    if (status === 400) await loginAdmin(request, 'http://127.0.0.1:5099');
    for (const port of [5099, 5105]) {
      for (const path of [`/orders/${id}/close`])
        expect(
          (await request.post(`http://127.0.0.1:${port}${root}${path}`, { data: {} })).status(),
        ).toBe(status);
    }
  }
});
