import { test, expect } from '@playwright/test';
import { adminWrite, loginAdmin } from './support/admin-session';
const origin = 'http://localhost:4201';
const root = '/api/v1/admin/purchasing';
test('partial receipts survive a lost browser response and cannot over-receive or duplicate stock', async ({
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
    slug: 'receipt-' + key,
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
    await page.goto(origin + '/aprovizionare');
    await page.getByRole('button', { name: new RegExp('CA-' + order.number + ' ·') }).click();
    const form = page.locator('sd-order-receipts');
    await form
      .getByRole('combobox', { name: 'Linie recepționată', exact: true })
      .selectOption(lineId);
    await form.getByLabel('Cantitate primită în bucăți').fill('40');
    await form.getByLabel('Document de recepție', { exact: true }).fill('AVIZ-' + key + '-1');
    const receiptPath = root + `/orders/${orderId}/receipts`;
    let submitted: Record<string, unknown> = {};
    await page.route(
      '**' + receiptPath,
      async (route) => {
        submitted = route.request().postDataJSON();
        const result = await route.fetch();
        expect(result.ok()).toBeTruthy();
        await route.abort('failed');
      },
      { times: 1 },
    );
    await form.getByRole('button', { name: 'Înregistrează recepția' }).click();
    await expect(page.getByRole('alert')).toBeVisible();
    await page.getByRole('button', { name: 'Reîncarcă comanda' }).click();
    await expect(form).toContainText('Confirmată în stoc:');
    order = await get();
    expect(order.receipts).toHaveLength(1);
    expect(order.receipts[0].quantity).toBe(40);
    expect((await write(receiptPath, submitted)).status()).toBe(204);
    expect((await write(receiptPath, { ...submitted, quantity: 41 })).status()).toBe(409);
    expect(
      (
        await write(receiptPath, {
          ...submitted,
          requestId: crypto.randomUUID(),
          version: order.version,
        })
      ).status(),
    ).toBe(409);
    expect(
      (
        await write(receiptPath, {
          requestId: crypto.randomUUID(),
          version: order.version,
          lineId,
          quantity: 161,
          reference: 'OVER',
        })
      ).status(),
    ).toBe(409);
    expect(
      (
        await write(root + `/orders/${orderId}/cancel`, {
          version: order.version,
          reason: 'Forbidden',
        })
      ).status(),
    ).toBe(409);
    await expect(page.getByRole('button', { name: 'Înregistrează anularea' })).toHaveCount(0);
    await form
      .getByRole('combobox', { name: 'Linie recepționată', exact: true })
      .selectOption(lineId);
    await form.getByLabel('Cantitate primită în bucăți').fill('160');
    await form.getByLabel('Document de recepție', { exact: true }).fill('AVIZ-' + key + '-2');
    await form.getByRole('button', { name: 'Înregistrează recepția' }).click();
    await expect(page.getByRole('status')).toContainText('Recepția a fost confirmată în stoc');
    order = await get();
    expect(order.receipts).toHaveLength(2);
    expect(order.receipts.every((r: { appliedAt: string | null }) => !!r.appliedAt)).toBeTruthy();
    const product = await (
      await page.request.get(origin + '/api/v1/admin/products/' + productId)
    ).json();
    expect(product.stock).toHaveLength(1);
    expect(product.stock[0].onHand).toBe(200);
    expect(product.prices).toEqual([]);
    expect(
      (await write(root + `/orders/${orderId}/receipts/${order.receipts[0].id}/retry`)).status(),
    ).toBe(204);
    const afterRetry = await (
      await page.request.get(origin + '/api/v1/admin/products/' + productId)
    ).json();
    expect(afterRetry.stock[0].onHand).toBe(200);
    await page.setViewportSize({ width: 1440, height: 1000 });
    await page.screenshot({ path: '../work/receipts-desktop.png', fullPage: true });
    await page.setViewportSize({ width: 390, height: 844 });
    expect(
      await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
    ).toBeTruthy();
    await page.screenshot({ path: '../work/receipts-mobile.png', fullPage: true });
  } finally {
    test.setTimeout(test.info().timeout + 15000);
    const supplier = await (
      await page.request.get(origin + root + '/suppliers/' + supplierId)
    ).json();
    await write(root + '/suppliers/' + supplierId, { ...supplier, isActive: false }, 'PUT');
    await write('/api/v1/admin/products/' + productId + '/archive');
  }
});

test('receiving and retrying require session and CSRF directly and through gateway', async ({
  request,
}) => {
  const id = '00000000-0000-0000-0000-000000000001';
  for (const status of [401, 400]) {
    if (status === 400) await loginAdmin(request, 'http://127.0.0.1:5099');
    for (const port of [5099, 5105]) {
      for (const path of [`/orders/${id}/receipts`, `/orders/${id}/receipts/${id}/retry`])
        expect(
          (await request.post(`http://127.0.0.1:${port}${root}${path}`, { data: {} })).status(),
        ).toBe(status);
    }
  }
});
