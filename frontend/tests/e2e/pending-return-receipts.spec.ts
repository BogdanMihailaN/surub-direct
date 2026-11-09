import { expect, test } from '@playwright/test';
import { loginAdmin } from './support/admin-session';

test('admin sees every scheduled receipt on the matching order', async ({ page }) => {
  const admin = 'http://localhost:4201';
  await loginAdmin(page.request, admin);
  await page.route('**/api/v1/admin/sales-orders?**', async route => {
    const filtered = new URL(route.request().url()).searchParams.get('partialReturnStatus') === 'Restoring';
    const pendingReceipts = ['retur-A', 'retur-B'].map((requestId, attempts) => ({
      requestId, attempts, requestedAt: '2026-11-06T11:00:00Z', nextAttemptAt: '2026-11-06T11:05:00Z',
    }));
    const items = filtered ? [{ id: 'order', number: 'DEMO-005', status: 'Confirmed', returnStatus: 'NotRequested',
      paymentStatus: 'Paid', pendingReceipts, customerName: 'Client', grossTotal: 44.20, currency: 'RON', createdAt: '2026-11-01T12:00:00Z' }] : [];
    await route.fulfill({ json: { items, total: items.length, page: 1, pageSize: 25 } });
  });
  await page.goto(admin + '/comenzi');
  await page.getByLabel('Stare retur parțial', { exact: true }).selectOption('Restoring');
  await page.getByRole('button', { name: 'Aplică filtrele' }).click();
  const row = page.getByLabel('Lista comenzilor', { exact: true }).getByRole('button');
  await expect(row).toHaveCount(1);
  for (const id of ['retur-A', 'retur-B']) {
    const receipt = row.locator('span').filter({ hasText: id });
    await expect(receipt).toContainText('În așteptare din 06.11.2026');
    await expect(receipt).toContainText('Următoarea verificare:');
  }
  await expect(row).toContainText('Încercări: 1');
});
