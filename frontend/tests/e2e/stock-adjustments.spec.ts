import { test, expect } from '@playwright/test';
import { adminWrite, loginAdmin } from './support/admin-session';
const origin = 'http://localhost:4201';
test('stock adjustments retain history, retry safely and reject stale balances', async ({
  page,
}) => {
  const key = Date.now().toString();
  const sku = 'ADJ-' + key;
  await loginAdmin(page.request, origin);
  const write = (path: string, data: object = {}, method = 'POST') =>
    adminWrite(page.request, origin, path, data, method);
  const product = await write('/api/v1/admin/products', {
    name: 'Ajustări ' + key,
    slug: 'adjustments-' + key,
    category: 'Verificare automată',
    description: 'Produs demonstrativ.',
  });
  expect(product.ok()).toBeTruthy();
  const productId = (await product.json()).id;
  try {
    const variant = await write(`/api/v1/admin/products/${productId}/variants`, {
      sku,
      label: 'M6',
      diameterMm: 6,
      lengthMm: 20,
      material: 'Oțel',
      finish: 'Zincat',
    });
    expect(variant.ok()).toBeTruthy();
    const id = (await variant.json()).id;
    const root = `/api/v1/admin/variants/${id}`;
    expect(
      (
        await write(root + '/receipts', {
          requestId: crypto.randomUUID(),
          quantity: 100,
          reference: 'REC-INITIAL',
        })
      ).ok(),
    ).toBeTruthy();
    await page.goto(origin + '/catalog');
    await page.getByLabel('Caută produs sau SKU').fill(sku);
    await page.getByRole('button', { name: 'Aplică filtrele' }).click();
    await expect(page.locator('.product-list button')).toHaveCount(1);
    await page.locator('.product-list button').click();
    const panel = page.locator('sd-stock-panel');
    await panel.getByRole('combobox', { name: 'SKU pentru jurnal', exact: true }).selectOption(id);
    await expect(panel).toContainText('În stoc: 100 buc.');
    await expect(panel).toContainText('REC-INITIAL');
    const form = panel.locator('sd-adjustment-form');
    await form.getByLabel('Diferență în bucăți').fill('-20');
    await form.getByLabel('Document ajustare').fill('INV-1');
    await form.getByLabel('Motiv ajustare').fill('Diferență la inventar');
    await form.getByRole('checkbox').check();
    let submitted: Record<string, unknown> = {};
    await page.route(
      '**' + root + '/adjustments',
      async (route) => {
        submitted = route.request().postDataJSON();
        const response = await route.fetch();
        expect(response.ok()).toBeTruthy();
        await route.abort('failed');
      },
      { times: 1 },
    );
    await form.getByRole('button', { name: 'Înregistrează ajustarea' }).click();
    await expect(panel.getByRole('alert')).toBeVisible();
    await panel.getByRole('button', { name: 'Reia ajustarea' }).click();
    await expect(panel).toContainText('În stoc: 80 buc.');
    await expect(panel).toContainText('Diferență la inventar');
    expect((await write(root + '/adjustments', submitted)).ok()).toBeTruthy();
    expect((await write(root + '/adjustments', { ...submitted, quantity: -21 })).status()).toBe(
      409,
    );
    expect(
      (
        await write(root + '/receipts', {
          requestId: crypto.randomUUID(),
          quantity: 5,
          reference: 'REC-CONCURRENT',
        })
      ).ok(),
    ).toBeTruthy();
    await form.getByLabel('Diferență în bucăți').fill('-1');
    await form.getByLabel('Document ajustare').fill('STALE');
    await form.getByLabel('Motiv ajustare').fill('Test concurență');
    await form.getByRole('checkbox').check();
    await form.getByRole('button', { name: 'Înregistrează ajustarea' }).click();
    await expect(panel.getByRole('alert')).toContainText('Stocul s-a modificat');
    await panel.getByRole('button', { name: 'Reîncarcă stocul' }).click();
    await expect(panel).toContainText('În stoc: 85 buc.');
    await form.getByLabel('Diferență în bucăți').fill('5');
    await form.getByLabel('Document ajustare').fill('INV-2');
    await form.getByLabel('Motiv ajustare').fill('Bucăți găsite');
    await form.getByRole('checkbox').check();
    await form.getByRole('button', { name: 'Înregistrează ajustarea' }).click();
    await expect(panel).toContainText('În stoc: 90 buc.');
    expect(
      (
        await write(root + '/adjustments', {
          requestId: crypto.randomUUID(),
          quantity: -91,
          expectedOnHand: 90,
          expectedReserved: 0,
          reference: 'NEG',
          reason: 'Test',
        })
      ).status(),
    ).toBe(409);
    const ledger = await (await page.request.get(origin + root + '/movements')).json();
    expect(ledger.total).toBe(4);
    expect(ledger.onHand).toBe(90);
    expect(ledger.items.filter((m: { kind: string }) => m.kind === 'Receipt')).toHaveLength(2);
    await page.setViewportSize({ width: 1440, height: 1000 });
    await panel.screenshot({ path: '../work/stock-adjustments-desktop.png' });
    await page.setViewportSize({ width: 390, height: 844 });
    expect(
      await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
    ).toBeTruthy();
    await panel.screenshot({ path: '../work/stock-adjustments-mobile.png' });
  } finally {
    test.setTimeout(test.info().timeout + 15000);
    await write(`/api/v1/admin/products/${productId}/archive`);
  }
});
test('stock ledger and adjustments enforce authentication and CSRF on direct service and gateway', async ({
  request,
}) => {
  const id = '00000000-0000-0000-0000-000000000001';
  for (const status of [401, 400]) {
    if (status === 400) await loginAdmin(request, 'http://127.0.0.1:5099');
    for (const port of [5099, 5102]) {
      const root = `http://127.0.0.1:${port}/api/v1/admin/variants/${id}`;
      expect((await request.post(root + '/adjustments', { data: {} })).status()).toBe(status);
      if (status === 401) expect((await request.get(root + '/movements')).status()).toBe(401);
    }
  }
});
