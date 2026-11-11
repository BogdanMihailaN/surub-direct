import { test, expect } from '@playwright/test';
import { createDeliveredReturnOrder } from './support/delivered-return-order';

test('admin reviews receipt alert and recovers retry audit after lost response', async ({ page }) => {
  test.setTimeout(120000);
  const order = await createDeliveredReturnOrder(page);
  const origin = 'http://localhost:4201';
  const path = '/api/v1/admin/sales-orders/' + order.id + '/partial-returns';
  const id = crypto.randomUUID();
  const retries: { operationId: string; version: number; reason: string; actor: string; createdAt: string }[] = [];
  let posts = 0;
  try {
    // Deterministic unavailable-Inventory fixture; persistence and stock replay are integration-tested.
    await page.route('**' + path, route => route.fulfill({ json: [{
      id, orderId: order.id, status: 'Restoring', version: retries.length ? 3 : 2,
      reason: 'Retur produse', actor: 'admin', createdAt: '2026-11-09T07:00:00Z', lines: [],
      receiptRequestedAt: '2026-11-09T07:00:00Z', receiptAttempts: 3,
      nextReceiptAttemptAt: '2026-11-09T07:05:00Z', receiptNeedsAttention: true,
      receiptError: 'Recepția Inventory nu este confirmată.', receiptRetries: retries,
    }] }));
    await page.route('**' + path + '/' + id + '/retry-receipt', async route => {
      const body = route.request().postDataJSON();
      expect(body.expectedVersion).toBe(2);
      expect(body.reason).toBe('Am verificat disponibilitatea Inventory');
      posts++;
      retries.push({ operationId: body.operationId, version: 3, reason: body.reason, actor: 'admin', createdAt: '2026-11-09T07:06:00Z' });
      await route.abort('failed');
    });
    await page.goto(origin + '/comenzi');
    await page.getByLabel('Număr comandă', { exact: true }).fill(order.number);
    await page.getByRole('button', { name: 'Aplică filtrele', exact: true }).click();
    const list = page.getByLabel('Lista comenzilor', { exact: true });
    await expect(list.getByRole('button')).toHaveCount(1);
    await list.getByRole('button').click();
    const card = page.locator('sd-partial-return-card');
    await expect(card.getByRole('alert')).toContainText('trei încercări nereușite');
    const retry = card.getByRole('button', { name: 'Reîncearcă recepția', exact: true });
    await expect(retry).toBeDisabled();
    await card.getByLabel('Motivul operației pentru această cerere').fill('Am verificat disponibilitatea Inventory');
    await card.getByLabel('Confirm operația pentru această cerere', { exact: true }).check();
    await retry.click();
    await expect(card).toContainText('Reîncercare solicitată: Am verificat disponibilitatea Inventory');
    await expect(card.getByRole('status')).toContainText('Restituire stoc în curs');
    expect(posts).toBe(1);
    await page.setViewportSize({ width: 390, height: 844 });
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    await card.screenshot({ path: '../work/receipt-retry-mobile.png' });
  } finally { await order.archive(); }
});
