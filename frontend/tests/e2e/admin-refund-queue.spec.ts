import { test, expect } from '@playwright/test';
import { adminWrite } from './support/admin-session';
import { createDeliveredReturnOrder } from './support/delivered-return-order';

test('restored returns awaiting a simulated refund can be found and leave the queue after refund', async ({ page }) => {
  test.setTimeout(120000);
  const order = await createDeliveredReturnOrder(page);
  const origin = 'http://localhost:4201';
  const path = '/api/v1/admin/sales-orders/' + order.id;
  try {
    for (const [expectedVersion, target] of ['Requested', 'Approved', 'Restoring'].entries()) {
      expect((await adminWrite(page.request, origin, path + '/return', {
        operationId: crypto.randomUUID(), expectedVersion, target,
        reason: 'Retur verificat pentru rambursare demo', restockConfirmed: target === 'Restoring',
      })).status()).toBe(204);
    }
    await expect.poll(async () => (await (await page.request.get(origin + path)).json()).return.status,
      { timeout: 30000 }).toBe('Restored');
    await page.goto(origin + '/comenzi');
    await page.getByLabel('Număr comandă', { exact: true }).fill(order.number);
    await page.getByLabel('Stare retur', { exact: true }).selectOption('Restored');
    await page.getByLabel('Stare plată simulată', { exact: true }).selectOption('Paid');
    await page.getByRole('button', { name: 'Aplică filtrele', exact: true }).click();
    const queue = page.getByLabel('Lista comenzilor', { exact: true });
    await expect(queue.getByRole('button')).toHaveCount(1);
    await expect(queue).toContainText('Retur: Stoc restituit');
    await expect(queue).toContainText('Plată simulată: Reușită');
    await queue.getByRole('button').click();
    const panel = page.getByRole('region', { name: 'Simulator local de plăți', exact: true });
    await panel.getByLabel('Motivul operației simulate').fill('Rambursare integrală după recepția returului');
    await panel.getByLabel('Confirm operația simulată pentru această comandă și sumă').check();
    let writes = 0;
    await page.route('**' + path + '/payment-simulation', async route => {
      writes++;
      expect((await route.fetch()).status()).toBe(204);
      await route.abort('failed');
    }, { times: 1 });
    await panel.getByRole('button', { name: 'Simulează rambursarea integrală', exact: true }).click();
    await expect(panel.getByRole('status')).toContainText('Rambursată integral');
    await expect(queue).toContainText('Nu există comenzi pentru filtrele alese.');
    expect(writes).toBe(1);
    await page.getByLabel('Stare plată simulată', { exact: true }).selectOption('Refunded');
    await page.getByRole('button', { name: 'Aplică filtrele', exact: true }).click();
    await expect(queue.getByRole('button')).toHaveCount(1);
    await expect(queue).toContainText('Plată simulată: Rambursată integral');
    const detail = await (await page.request.get(origin + path)).json();
    expect(detail.paymentSimulation.history.filter((x: { action: string }) => x.action === 'Refund')).toHaveLength(1);
    expect(detail.return.status).toBe('Restored');
    expect(detail.order.status).toBe('Confirmed');
    await page.setViewportSize({ width: 390, height: 844 });
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    await page.screenshot({ path: '../work/admin-refund-queue-mobile.png', fullPage: true });
  } finally { await order.archive(); }
});
