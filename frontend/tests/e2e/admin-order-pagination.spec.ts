import { test, expect } from '@playwright/test';
import { loginAdmin } from './support/admin-session';

for (const outcome of ['remaining', 'empty', 'unavailable'] as const) {
  test(`administrative queue recovers a removed last page: ${outcome}`, async ({ page }) => {
    await loginAdmin(page.request, 'http://localhost:4201');
    let removed = false;
    let failRecovery = outcome === 'unavailable';
    const reads: number[] = [];
    await page.route('**/api/v1/admin/sales-orders?**', async route => {
      const query = new URL(route.request().url()).searchParams;
      const current = Number(query.get('page'));
      const filtered = query.get('returnStatus') === 'Requested';
      if (filtered) reads.push(current);
      if (removed && current === 1 && failRecovery) {
        failRecovery = false;
        await route.fulfill({ status: 503, json: { detail: 'Coada este temporar indisponibilă.' } });
        return;
      }
      const empty = removed && (current === 2 || outcome === 'empty');
      await route.fulfill({ json: {
        page: current, pageSize: 25, total: removed ? (outcome === 'empty' ? 0 : 25) : 26,
        items: empty ? [] : [{
          id: 'queue-order-' + current, number: 'DEMO-' + current, status: 'Confirmed',
          customerName: 'Client demonstrativ', grossTotal: 10, currency: 'RON',
          createdAt: '2026-11-04T10:00:00Z', returnStatus: 'Requested',
        }],
      } });
    });
    await page.goto('http://localhost:4201/comenzi');
    await page.getByLabel('Stare retur', { exact: true }).selectOption('Requested');
    await page.getByRole('button', { name: 'Aplică filtrele' }).click();
    await expect(page.getByText('26 comenzi · Pagina 1', { exact: true })).toBeVisible();
    await page.getByRole('button', { name: 'Pagina următoare', exact: true }).click();
    await expect(page.getByText('26 comenzi · Pagina 2', { exact: true })).toBeVisible();
    removed = true;
    await page.getByRole('button', { name: 'Actualizează lista', exact: true }).click();
    const list = page.getByLabel('Lista comenzilor', { exact: true });
    if (outcome === 'unavailable') {
      await expect(page.getByRole('alert')).toContainText('Coada este temporar indisponibilă');
      await expect(list).toHaveCount(0);
      await page.getByRole('button', { name: 'Actualizează lista', exact: true }).click();
    }
    await expect(page.getByText(`${outcome === 'empty' ? 0 : 25} comenzi · Pagina 1`, { exact: true })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Pagina anterioară', exact: true })).toBeDisabled();
    await expect(page.getByLabel('Stare retur', { exact: true })).toHaveValue('Requested');
    if (outcome === 'empty') await expect(list).toContainText('Nu există comenzi pentru filtrele alese.');
    else await expect(list).toContainText('DEMO-1');
    expect(reads).toEqual(outcome === 'unavailable' ? [1, 2, 2, 1, 1] : [1, 2, 2, 1]);
  });
}
