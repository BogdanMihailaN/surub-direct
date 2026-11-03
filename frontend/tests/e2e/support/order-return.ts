import { expect, Page } from '@playwright/test';
import { adminWrite } from './admin-session';
export async function verifyOrderReturn(page: Page, id: string, variantId: string) {
  const origin = 'http://localhost:4201';
  const path = '/api/v1/admin/sales-orders/' + id + '/return';
  const panel = page.getByRole('region', { name: 'Retur integral demonstrativ', exact: true });
  const ledger = async () => (await page.request.get(origin + '/api/v1/admin/variants/' + variantId + '/movements')).json();
  async function act(button: string) {
    await panel.getByLabel('Motivul operației de retur').fill('Retur integral verificat local');
    await panel.getByLabel('Confirm operația de retur integral', { exact: true }).check();
    await panel.getByRole('button', { name: button, exact: true }).click();
  }
  await act('Înregistrează solicitarea de retur');
  await expect(panel.getByRole('status')).toContainText('Solicitat');
  await act('Aprobă returul integral');
  await expect(panel.getByRole('status')).toContainText('Aprobat');
  expect((await ledger()).onHand).toBe(65);
  expect((await adminWrite(page.request, origin, path, {
    operationId: crypto.randomUUID(), expectedVersion: 2, target: 'Restoring', reason: 'Fără confirmare', restockConfirmed: false,
  })).status()).toBe(400);
  let receipt: object | undefined;
  await page.route('**' + path, async route => {
    receipt = route.request().postDataJSON();
    expect((await route.fetch()).status()).toBe(204);
    await route.abort('failed');
  }, { times: 1 });
  await panel.getByLabel('Confirm primirea tuturor produselor în cantitățile originale și în stare revandabilă').check();
  await act('Confirmă primirea și restituie stocul');
  await expect.poll(async () => {
    await page.getByRole('button', { name: 'Actualizează detaliile', exact: true }).click();
    return (await (await page.request.get(origin + '/api/v1/admin/sales-orders/' + id)).json()).return.status;
  }, { timeout: 30000 }).toBe('Restored');
  await page.getByRole('button', { name: 'Actualizează detaliile', exact: true }).click();
  await expect(panel.getByRole('status')).toContainText('Stoc restituit');
  await expect(panel.locator('article')).toHaveCount(3);
  expect(receipt).toBeTruthy();
  expect((await adminWrite(page.request, origin, path, receipt!)).status()).toBe(204);
  const stock = await ledger();
  expect(stock.onHand).toBe(100);
  expect(stock.reserved).toBe(0);
  expect(stock.items.filter((x: { kind: string }) => x.kind === 'Restoration')).toHaveLength(1);
  expect(stock.items.find((x: { kind: string }) => x.kind === 'Restoration').quantity).toBe(35);
  const detail = await (await page.request.get(origin + '/api/v1/admin/sales-orders/' + id)).json();
  expect(detail.paymentSimulation.status).toBe('Refunded');
  expect(detail.order.status).toBe('Confirmed');
  expect(detail.reservation.status).toBe('Restored');
}
