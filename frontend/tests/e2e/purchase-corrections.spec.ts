import { test, expect } from '@playwright/test';
import { adminWrite, loginAdmin } from './support/admin-session';
const origin = 'http://localhost:4201';
const root = '/api/v1/admin/purchasing';
test('receipt correction preserves history, survives lost response and reopens capacity', async ({
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
    slug: 'correction-' + key,
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
          reference: 'AVIZ-' + key,
        })
      ).status(),
    ).toBe(204);
    await page.goto(origin + '/aprovizionare');
    await page.getByRole('button', { name: new RegExp('CA-' + order.number + ' ·') }).click();
    const form = page.locator('sd-order-corrections');
    await form
      .getByRole('combobox', { name: 'Recepția de corectat', exact: true })
      .selectOption(receiptId);
    await form.getByLabel('Cantitate de scăzut (buc.)').fill('10');
    await form.getByLabel('Document corecție').fill('COR-' + key);
    await form.getByLabel('Motivul corecției').fill('Cantitate introdusă greșit la recepție');
    await form.getByRole('checkbox').check();
    const correctionPath = root + `/orders/${orderId}/corrections`;
    let submitted: Record<string, unknown> = {};
    await page.route(
      '**' + correctionPath,
      async (route) => {
        submitted = route.request().postDataJSON();
        expect((await route.fetch()).ok()).toBeTruthy();
        await route.abort('failed');
      },
      { times: 1 },
    );
    await form.getByRole('button', { name: 'Înregistrează corecția' }).click();
    await expect(page.getByRole('alert')).toBeVisible();
    await page.getByRole('button', { name: 'Reîncarcă comanda' }).click();
    await expect(form).toContainText('Confirmată în stoc');
    order = await get();
    expect(order.corrections).toHaveLength(1);
    expect(order.receipts[0].quantity).toBe(40);
    expect((await write(correctionPath, submitted)).status()).toBe(204);
    expect((await write(correctionPath, { ...submitted, quantity: 11 })).status()).toBe(409);
    expect(
      (
        await write(correctionPath, {
          ...submitted,
          requestId: crypto.randomUUID(),
          version: order.version,
          quantity: 31,
          reference: 'OVER',
        })
      ).status(),
    ).toBe(409);
    expect((await write(correctionPath + '/' + order.corrections[0].id + '/retry')).status()).toBe(
      204,
    );
    const stock = async () =>
      (await (await page.request.get(origin + '/api/v1/admin/products/' + productId)).json())
        .stock[0].onHand;
    expect(await stock()).toBe(30);
    const ledger = await (
      await page.request.get(origin + `/api/v1/admin/variants/${variantId}/movements`)
    ).json();
    expect(ledger.items.find((m: { kind: string }) => m.kind === 'Correction')).toMatchObject({
      quantity: -10,
      originalReceiptId: receiptId,
    });
    expect(
      (
        await write(root + `/orders/${orderId}/receipts`, {
          requestId: crypto.randomUUID(),
          version: order.version,
          lineId,
          quantity: 170,
          reference: 'REST-' + key,
        })
      ).status(),
    ).toBe(204);
    expect(await stock()).toBe(200);
    await page.getByRole('button', { name: 'Reîncarcă comanda' }).click();
    await expect(page.locator('sd-order-receipts')).toContainText(
      'Toată cantitatea comandată este confirmată în stoc.',
    );
    await page.setViewportSize({ width: 1440, height: 1000 });
    await page.screenshot({ path: '../work/corrections-desktop.png', fullPage: true });
    await page.setViewportSize({ width: 390, height: 844 });
    expect(
      await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
    ).toBeTruthy();
    await page.screenshot({ path: '../work/corrections-mobile.png', fullPage: true });
  } finally {
    test.setTimeout(test.info().timeout + 15000);
    const supplier = await (
      await page.request.get(origin + root + '/suppliers/' + supplierId)
    ).json();
    await write(root + '/suppliers/' + supplierId, { ...supplier, isActive: false }, 'PUT');
    await write('/api/v1/admin/products/' + productId + '/archive');
  }
});

test('correcting and retrying require session and CSRF directly and through gateway', async ({
  request,
}) => {
  const id = '00000000-0000-0000-0000-000000000001';
  for (const status of [401, 400]) {
    if (status === 400) await loginAdmin(request, 'http://127.0.0.1:5099');
    for (const port of [5099, 5105]) {
      for (const path of [`/orders/${id}/corrections`, `/orders/${id}/corrections/${id}/retry`])
        expect(
          (await request.post(`http://127.0.0.1:${port}${root}${path}`, { data: {} })).status(),
        ).toBe(status);
    }
  }
});

test('internal stock correction cannot be called through gateway or without both session and service credentials', async ({
  request,
}) => {
  expect(
    (
      await request.post('http://127.0.0.1:5099/internal/receipt-corrections', { data: {} })
    ).status(),
  ).toBe(404);
  expect(
    (
      await request.post('http://127.0.0.1:5102/internal/receipt-corrections', { data: {} })
    ).status(),
  ).toBe(401);
  await loginAdmin(request, 'http://127.0.0.1:5099');
  expect(
    (
      await request.post('http://127.0.0.1:5102/internal/receipt-corrections', { data: {} })
    ).status(),
  ).toBe(400);
  const token = await (await request.get('http://127.0.0.1:5099/api/auth/csrf')).json();
  expect(
    (
      await request.post('http://127.0.0.1:5102/internal/receipt-corrections', {
        data: {},
        headers: { 'X-CSRF-TOKEN': token.token },
      })
    ).status(),
  ).toBe(401);
});
