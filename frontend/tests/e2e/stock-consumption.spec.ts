import { test, expect } from '@playwright/test';
import { readFileSync } from 'node:fs';
import { adminWrite, loginAdmin } from './support/admin-session';
const admin = 'http://localhost:4201';
const inventory = 'http://127.0.0.1:5102';
const settings = Object.fromEntries(
  readFileSync('../.env', 'utf8')
    .split(/\r?\n/)
    .filter((x) => x.includes('='))
    .map((x) => {
      const i = x.indexOf('=');
      return [x.slice(0, i), x.slice(i + 1)];
    }),
);
const headers = { 'X-Service-Key': settings['RESERVATIONS_SERVICE_KEY'] };
test('internal consumption is idempotent, protected and visible in the admin ledger', async ({
  page,
}) => {
  await loginAdmin(page.request, admin);
  const write = (path: string, data: object = {}) => adminWrite(page.request, admin, path, data);
  const create = async (path: string, data: object) => {
    const r = await write(path, data);
    expect(r.ok()).toBe(true);
    return (await r.json()).id as string;
  };
  const stamp = crypto.randomUUID();
  const sku = 'CONSUME-' + stamp;
  const productId = await create('/api/v1/admin/products', {
    name: 'Consum demo',
    slug: 'consum-' + stamp,
    category: 'Verificare automată',
    description: 'Test jurnal consum.',
  });
  const productPath = '/api/v1/admin/products/' + productId;
  const variantId = await create(productPath + '/variants', {
    sku,
    label: 'M6',
    diameterMm: 6,
    lengthMm: 20,
    material: 'Oțel',
    finish: 'Zincat',
  });
  const root = '/api/v1/admin/variants/' + variantId;
  const reservation = {
    id: crypto.randomUUID(),
    orderId: crypto.randomUUID(),
    durationSeconds: 120,
    lines: [{ variantId, quantity: 12 }],
  };
  const path = '/internal/reservations/' + reservation.id;
  try {
    expect(
      (
        await write(root + '/receipts', {
          requestId: crypto.randomUUID(),
          quantity: 100,
          reference: 'INITIAL',
        })
      ).ok(),
    ).toBe(true);
    expect(
      (
        await page.request.post(inventory + '/internal/reservations', {
          headers,
          data: reservation,
        })
      ).status(),
    ).toBe(200);
    const data = { orderId: reservation.orderId };
    expect((await page.request.post(inventory + path + '/consume', { data })).status()).toBe(401);
    expect(
      (
        await page.request.post(inventory + path + '/consume', {
          data,
          headers: { 'X-Service-Key': settings['INVENTORY_SERVICE_KEY'] },
        })
      ).status(),
    ).toBe(401);
    expect(
      (
        await page.request.post('http://localhost:5099' + path + '/consume', { headers, data })
      ).status(),
    ).toBe(404);
    expect(
      (
        await page.request.post(inventory + path + '/consume', {
          headers,
          data: { orderId: crypto.randomUUID() },
        })
      ).status(),
    ).toBe(409);
    let endedAt = '';
    for (let i = 0; i < 2; i++) {
      const result = await page.request.post(inventory + path + '/consume', { headers, data });
      expect(result.status()).toBe(200);
      expect(result.headers()['cache-control']).toBe('no-store');
      const body = await result.json();
      expect(body.status).toBe('Consumed');
      if (i) expect(body.endedAt).toBe(endedAt);
      endedAt = body.endedAt;
    }
    expect(
      (await (await page.request.post(inventory + path + '/release', { headers, data })).json())
        .status,
    ).toBe('Consumed');
    const ledger = await (await page.request.get(admin + root + '/movements')).json();
    expect(ledger).toMatchObject({ onHand: 88, reserved: 0, total: 2 });
    expect(ledger.items.filter((x: { kind: string }) => x.kind === 'Consumption')).toEqual([
      expect.objectContaining({
        id: reservation.id,
        quantity: -12,
        reference: reservation.orderId,
      }),
    ]);
    await page.goto(admin + '/catalog');
    await page.getByLabel('Caută produs sau SKU').fill(sku);
    await page.getByRole('button', { name: 'Aplică filtrele' }).click();
    await expect(page.locator('.product-list button')).toHaveCount(1);
    await page.locator('.product-list button').click();
    const panel = page.locator('sd-stock-panel');
    await panel
      .getByRole('combobox', { name: 'SKU pentru jurnal', exact: true })
      .selectOption(variantId);
    await expect(panel).toContainText('În stoc: 88 buc.');
    const row = panel.getByRole('row').filter({ hasText: reservation.orderId });
    await expect(row).toContainText('Consum rezervare');
    await expect(row).toContainText('-12');
    await page.setViewportSize({ width: 390, height: 844 });
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(
      true,
    );
    await panel.screenshot({ path: '../work/stock-consumption-mobile.png' });
  } finally {
    await page.request.post(inventory + path + '/release', {
      headers,
      data: { orderId: reservation.orderId },
    });
    await write(productPath + '/archive');
  }
});
