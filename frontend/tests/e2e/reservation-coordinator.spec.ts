import { test, expect } from '@playwright/test';
import { readFileSync } from 'node:fs';
import { adminWrite, loginAdmin } from './support/admin-session';
const secrets = Object.fromEntries(
  readFileSync('../.env', 'utf8')
    .split(/\r?\n/)
    .filter((x) => x.includes('='))
    .map((x) => {
      const i = x.indexOf('=');
      return [x.slice(0, i), x.slice(i + 1)];
    }),
);
const headers = { 'X-Service-Key': secrets['COORDINATOR_SERVICE_KEY'] };
const reader = { 'X-Service-Key': secrets['INVENTORY_SERVICE_KEY'] };
const ordering = 'http://127.0.0.1:5104/internal/reservation-plans';
const admin = 'http://localhost:4201';
test('Ordering coordinates reserve/cancel and prevents a late plan from undoing early cancellation', async ({
  request,
}) => {
  await loginAdmin(request, admin);
  const stamp = crypto.randomUUID();
  const created = await adminWrite(request, admin, '/api/v1/admin/products', {
    name: 'Coordonare ' + stamp,
    slug: 'coord-' + stamp,
    category: 'Verificare automată',
    description: 'Demo coordonare.',
  });
  expect(created.ok()).toBe(true);
  const productId = (await created.json()).id;
  const variant = await adminWrite(
    request,
    admin,
    '/api/v1/admin/products/' + productId + '/variants',
    {
      sku: 'COORD-' + stamp,
      label: 'M6',
      diameterMm: 6,
      lengthMm: 20,
      material: 'Oțel',
      finish: 'Zincat',
    },
  );
  expect(variant.ok()).toBe(true);
  const variantId = (await variant.json()).id;
  expect(
    (
      await adminWrite(request, admin, '/api/v1/admin/variants/' + variantId + '/receipts', {
        requestId: crypto.randomUUID(),
        quantity: 20,
        reference: 'COORD-' + stamp,
      })
    ).ok(),
  ).toBe(true);
  const data = {
    id: crypto.randomUUID(),
    orderId: crypto.randomUUID(),
    lines: [{ variantId, quantity: 7 }],
  };
  const status = async (id: string) =>
    (await (await request.get(ordering + '/' + id, { headers })).json()).status;
  const stock = async () =>
    (
      await (
        await request.post('http://127.0.0.1:5102/internal/stock/query', {
          headers: reader,
          data: { variantIds: [variantId] },
        })
      ).json()
    )[0];
  try {
    expect((await request.post(ordering, { data })).status()).toBe(401);
    expect((await request.post(ordering, { headers: reader, data })).status()).toBe(401);
    expect(
      (
        await request.post('http://localhost:5099/internal/reservation-plans', { headers, data })
      ).status(),
    ).toBe(404);
    expect((await request.post(ordering, { headers, data })).status()).toBe(200);
    await expect.poll(() => status(data.id), { timeout: 20000 }).toBe('Active');
    expect(await stock()).toMatchObject({ onHand: 20, reserved: 7, available: 13 });
    expect((await request.post(ordering, { headers, data })).status()).toBe(200);
    expect(
      (
        await request.post(ordering, {
          headers,
          data: { ...data, lines: [{ variantId, quantity: 8 }] },
        })
      ).status(),
    ).toBe(409);
    expect(
      (
        await request.post(ordering + '/' + data.id + '/cancel', {
          headers,
          data: { orderId: data.orderId },
        })
      ).status(),
    ).toBe(200);
    await expect.poll(() => status(data.id), { timeout: 20000 }).toBe('Released');
    expect(await stock()).toMatchObject({ onHand: 20, reserved: 0 });
    const early = { ...data, id: crypto.randomUUID() };
    expect(
      (
        await request.post(ordering + '/' + early.id + '/cancel', {
          headers,
          data: { orderId: early.orderId },
        })
      ).status(),
    ).toBe(200);
    expect((await request.post(ordering, { headers, data: early })).status()).toBe(200);
    await expect.poll(() => status(early.id), { timeout: 20000 }).toBe('Released');
    expect(await stock()).toMatchObject({ onHand: 20, reserved: 0 });
    expect(
      (await (await request.get('http://localhost:5099/api/v1/ordering/capabilities')).json())
        .checkoutEnabled,
    ).toBe(false);
  } finally {
    await request.post(ordering + '/' + data.id + '/cancel', {
      headers,
      data: { orderId: data.orderId },
    });
    await adminWrite(request, admin, '/api/v1/admin/products/' + productId + '/archive', {});
  }
});
