import { verifyShipment } from './shipment';
import { expect, Page } from '@playwright/test';
import { adminWrite } from './admin-session';

export async function verifyPaymentSimulation(page: Page, orderId: string) {
  const path = '/api/v1/admin/sales-orders/' + orderId + '/payment-simulation';
  const panel = page.getByRole('region', { name: 'Simulator local de plăți', exact: true });
  await expect(panel).toContainText('62,35 RON');
  await expect(panel).toContainText('Nu se încasează și nu se rambursează bani reali');
  async function act(button: string) {
    await panel.getByLabel('Motivul operației simulate').fill('Verificare locală fără bani reali');
    await panel.getByLabel('Confirm operația simulată pentru această comandă și sumă').check();
    await panel.getByRole('button', { name: button, exact: true }).click();
  }
  let writes = 0;
  await page.route('**' + path, async route => {
    writes++;
    const response = await route.fetch();
    expect(response.status()).toBe(204);
    await route.abort('failed');
  }, { times: 1 });
  await act('Inițiază plata simulată');
  await expect(panel.getByRole('status')).toContainText('În curs');
  await expect(panel.locator('article')).toHaveCount(1);
  expect(writes).toBe(1);
  const cancellation = page.locator('sd-order-cancellation');
  await expect(cancellation.getByRole('button')).toBeDisabled();
  expect((await adminWrite(page.request, 'http://localhost:4201', '/api/v1/admin/sales-orders/' + orderId + '/cancel', { reason: 'Interzis în timpul plății' })).status()).toBe(409);
  await act('Simulează plata eșuată');
  await expect(panel.getByRole('status')).toContainText('Eșuată');
  await act('Inițiază plata simulată');
  await expect(panel.getByRole('status')).toContainText('În curs');
  expect((await adminWrite(page.request, 'http://localhost:4201', path, { operationId: crypto.randomUUID(), expectedVersion: 1, action: 'Succeed', reason: 'Rezultat întârziat' })).status()).toBe(409);
  await act('Simulează plata reușită');
  await expect(panel.getByRole('status')).toContainText('Reușită');
  expect((await adminWrite(page.request, 'http://localhost:4201', '/api/v1/admin/sales-orders/' + orderId + '/cancel', { reason: 'Necesită rambursare' })).status()).toBe(409);
  await verifyShipment(page, orderId);
  await act('Simulează rambursarea integrală');
  await expect(panel.getByRole('status')).toContainText('Rambursată integral');
  await expect(panel.locator('article')).toHaveCount(5);
  await expect(panel.getByRole('button')).toHaveCount(0);
  const detail = await (await page.request.get('http://localhost:4201/api/v1/admin/sales-orders/' + orderId)).json();
  expect(detail.paymentSimulation.amount).toBe(62.35);
  expect(detail.paymentSimulation.status).toBe('Refunded');
  expect(detail.paymentSimulation.history[0].attemptId).not.toBe(detail.paymentSimulation.history[2].attemptId);
  expect(detail.order.status).toBe('Confirmed');
  expect(detail.reservation.status).toBe('Consumed');
  expect(detail.shipment.status).toBe('Delivered');
  await expect(cancellation.getByRole('button')).toBeDisabled();
  expect((await adminWrite(page.request, 'http://localhost:4201', '/api/v1/admin/sales-orders/' + orderId + '/cancel', { reason: 'Expediere rambursată' })).status()).toBe(409);
}
