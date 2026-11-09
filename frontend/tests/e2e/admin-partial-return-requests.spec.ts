import { test, expect } from '@playwright/test';
import { adminWrite, loginAdmin } from './support/admin-session';
import { createDeliveredReturnOrder } from './support/delivered-return-order';
const origin = 'http://localhost:4201';

test('partial returns preserve selection, approve without stock effects and receive selected quantities once', async ({ page }) => {
  test.setTimeout(120000);
  const order = await createDeliveredReturnOrder(page);
  const root = '/api/v1/admin/sales-orders/' + order.id;
  const path = root + '/partial-returns';
  try {
    const initial = await (await page.request.get(origin + root)).json();
    const line = initial.order.lines[0];
    const request = { requestId: crypto.randomUUID(), reason: 'Retur parțial demonstrativ', lines: [{ offerId: line.offerId, packs: 1 }] };
    const first = await adminWrite(page.request, origin, path, request);
    expect(first.status()).toBe(200);
    expect(first.headers()['cache-control']).toBe('no-store');
    const saved = await first.json();
    expect(saved.lines[0]).toMatchObject({ offerId: line.offerId, sku: line.sku, packs: 1 });
    expect([5, 10]).toContain(saved.lines[0].unitsPerPack);
    expect(saved.lines[0].quantity).toBe(saved.lines[0].unitsPerPack);
    expect(saved.actor).toBeTruthy();
    expect(await (await adminWrite(page.request, origin, path, request)).json()).toEqual(saved);
    expect((await adminWrite(page.request, origin, path, { ...request, requestId: crypto.randomUUID(), lines: [{ offerId: line.offerId, packs: line.packs }] })).status()).toBe(409);
    expect((await adminWrite(page.request, origin, root + '/return', { operationId: crypto.randomUUID(), expectedVersion: 0, target: 'Requested', reason: 'Retur integral' })).status()).toBe(409);
    const decision = { operationId: crypto.randomUUID(), expectedVersion: 0, target: 'Approved', reason: 'Produse verificate' };
    const decisionPath = path + '/' + request.requestId + '/decision';
    expect((await adminWrite(page.request, origin, decisionPath, decision)).status()).toBe(204);
    expect((await adminWrite(page.request, origin, decisionPath, decision)).status()).toBe(204);
    expect((await adminWrite(page.request, origin, decisionPath, { ...decision, target: 'Rejected' })).status()).toBe(409);
    const replay = await (await adminWrite(page.request, origin, path, request)).json();
    expect(replay.status).toBe('Approved');
    expect(replay.createdAt).toBe(saved.createdAt);
    expect(replay.version).toBe(1);
    const listResponse = await page.request.get(origin + path);
    expect(listResponse.headers()['cache-control']).toBe('no-store');
    expect(await listResponse.json()).toEqual([replay]);
    const after = await (await page.request.get(origin + root)).json();
    expect(after.reservation.status).toBe('Consumed');
    expect(after.return.status).toBe('NotRequested');
    expect(after.paymentSimulation.status).toBe('Paid');
    expect(after.order.status).toBe('Confirmed');
    const variant = saved.lines[0].variantId;
    const movements = '/api/v1/admin/variants/' + variant + '/movements';
    const beforeStock = await (await page.request.get(origin + movements)).json();
    const receivePath = path + '/' + request.requestId + '/receive';
    const receive = { operationId: crypto.randomUUID(), expectedVersion: 1, reason: 'Cantități primite și verificate', resalableConfirmed: true };
    expect((await adminWrite(page.request, origin, receivePath, { ...receive, resalableConfirmed: false })).status()).toBe(400);
    expect((await adminWrite(page.request, origin, receivePath, receive)).status()).toBe(204);
    expect((await adminWrite(page.request, origin, receivePath, receive)).status()).toBe(204);
    await expect.poll(async () => (await (await page.request.get(origin + path)).json())[0].status, { timeout: 30000 }).toBe('Restored');
    const complete = (await (await page.request.get(origin + path)).json())[0];
    expect(complete.inventoryReceivedAt).toBeTruthy();
    expect(complete.version).toBe(2);
    expect(complete.receiptAttempts).toBeGreaterThanOrEqual(1);
    expect(complete.nextReceiptAttemptAt).toBeNull();
    expect((await adminWrite(page.request, origin, receivePath, receive)).status()).toBe(204);
    expect((await adminWrite(page.request, origin, decisionPath, decision)).status()).toBe(204);
    expect((await adminWrite(page.request, origin, receivePath, { ...receive, operationId: crypto.randomUUID(), expectedVersion: 2 })).status()).toBe(409);
    expect((await (await page.request.get(origin + path)).json())[0].inventoryReceivedAt).toBe(complete.inventoryReceivedAt);
    const stock = await (await page.request.get(origin + movements)).json();
    expect(stock.onHand).toBe(beforeStock.onHand + saved.lines[0].quantity);
    expect(stock.items.filter((x: { kind: string; id: string }) => x.kind === 'ReturnReceipt' && x.id === request.requestId)).toHaveLength(1);
    const finalOrder = await (await page.request.get(origin + root)).json();
    expect(finalOrder.reservation.status).toBe('Consumed');
    expect(finalOrder.paymentSimulation.status).toBe('Paid');
  } finally { await order.archive(); }
});

test('partial return administration enforces session role and CSRF through gateway and direct service', async ({ request, playwright }) => {
  const path = '/api/v1/admin/sales-orders/' + crypto.randomUUID() + '/partial-returns';
  const decisionPath = path + '/' + crypto.randomUUID() + '/decision';
  const receivePath = path + '/' + crypto.randomUUID() + '/receive';
  const refundPath = path + '/' + crypto.randomUUID() + '/refund';
  const bases = ['http://localhost:5099', 'http://localhost:5104'];
  for (const base of bases) {
    expect((await request.get(base + path)).status()).toBe(401);
    expect((await request.post(base + path, { data: {} })).status()).toBe(401);
    expect((await request.post(base + decisionPath, { data: {} })).status()).toBe(401);
    expect((await request.post(base + receivePath, { data: {} })).status()).toBe(401);
    expect((await request.post(base + refundPath, { data: {} })).status()).toBe(401);
  }
  await loginAdmin(request, origin);
  for (const base of bases) {
    expect((await request.post(base + path, { data: {} })).status()).toBe(400);
    expect((await request.post(base + decisionPath, { data: {} })).status()).toBe(400);
    expect((await request.post(base + receivePath, { data: {} })).status()).toBe(400);
    expect((await request.post(base + refundPath, { data: {} })).status()).toBe(400);
    expect((await request.get(base + path)).status()).toBe(404);
  }
  const customer = await playwright.request.newContext();
  try {
    const credentials = { email: `partial-${crypto.randomUUID()}@example.test`, password: 'Partial-test-123!Secure' };
    expect((await adminWrite(customer, origin, '/api/auth/register', credentials)).status()).toBe(204);
    expect((await adminWrite(customer, origin, '/api/auth/login', credentials)).status()).toBe(200);
    const csrf = await (await customer.get(origin + '/api/auth/csrf')).json();
    for (const base of bases) {
      expect((await customer.get(base + path)).status()).toBe(403);
      for (const route of [path, decisionPath, receivePath, refundPath])
        expect((await customer.post(base + route, { data: {}, headers: { 'X-CSRF-TOKEN': csrf.token } })).status()).toBe(403);
    }
  } finally { await customer.dispose(); }
});
