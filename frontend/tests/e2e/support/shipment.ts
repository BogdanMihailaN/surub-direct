import { expect, Page } from '@playwright/test';
import { adminWrite } from './admin-session';
export async function verifyShipment(page: Page, id: string) {
  const path = '/api/v1/admin/sales-orders/' + id + '/shipment';
  const panel = page.getByRole('region', { name: 'Expediere demonstrativă', exact: true });
  async function act(button: string) {
    await panel.getByLabel('Motivul operației de expediere').fill('Verificare expediere fără curier real');
    await panel.getByLabel('Confirm operația de expediere demonstrativă').check();
    await panel.getByRole('button', { name: button, exact: true }).click();
  }
  let writes = 0;
  await page.route('**' + path, async route => {
    writes++;
    expect((await route.fetch()).status()).toBe(204);
    await route.abort('failed');
  }, { times: 1 });
  await act('Creează expedierea demo');
  await expect(panel.getByRole('status')).toContainText('Pregătită pentru expediere');
  await expect(panel.locator('article')).toHaveCount(1);
  expect(writes).toBe(1);
  expect((await adminWrite(page.request, 'http://localhost:4201', path, {
    operationId: crypto.randomUUID(), expectedVersion: 0, target: 'Shipped', reason: 'Versiune veche',
  })).status()).toBe(409);
  await expect(page.locator('sd-order-processing').getByRole('button')).toHaveCount(0);
  await act('Marchează expediată demo');
  await expect(panel.getByRole('status')).toContainText('Expediată');
  await act('Marchează livrată demo');
  await expect(panel.getByRole('status')).toContainText('Livrată');
  await expect(panel.locator('article')).toHaveCount(3);
  await expect(panel.getByRole('button')).toHaveCount(0);
  await expect(panel).toContainText('DEMO-' + id.replaceAll('-', '').toUpperCase());
}
