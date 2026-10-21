import { test, expect, APIRequestContext } from '@playwright/test';
import { readFileSync } from 'node:fs';
import { adminWrite, loginAdmin } from './support/admin-session';
const admin = 'http://localhost:4201';
const inventory = 'http://127.0.0.1:5102';
const secrets = Object.fromEntries(
  readFileSync('../.env', 'utf8')
    .split(/\r?\n/)
    .filter((x) => x.includes('='))
    .map((x) => {
      const i = x.indexOf('=');
      return [x.slice(0, i), x.slice(i + 1)];
    }),
);
const headers = { 'X-Service-Key': secrets['RESERVATIONS_SERVICE_KEY'] };
const reader = { 'X-Service-Key': secrets['INVENTORY_SERVICE_KEY'] };
async function stock(api: APIRequestContext, variantId: string) {
  const response = await api.post(inventory + '/internal/stock/query', {
    headers: reader,
    data: { variantIds: [variantId] },
  });
  expect(response.status()).toBe(200);
  return (await response.json())[0];
}
async function product(api: APIRequestContext) {
  await loginAdmin(api, admin);
  const stamp = crypto.randomUUID();
  const create = async (path: string, data: object) => {
    const response = await adminWrite(api, admin, path, data);
    expect(response.ok()).toBe(true);
    return (await response.json()).id as string;
  };
  const productId = await create('/api/v1/admin/products', {
    name: 'Rezervare demo ' + stamp,
    slug: 'rezervare-' + stamp,
    category: 'Verificare automată',
    description: 'Test rezervări.',
  });
  const variantId = await create('/api/v1/admin/products/' + productId + '/variants', {
    sku: 'RES-' + stamp,
    label: 'M6',
    diameterMm: 6,
    lengthMm: 20,
    material: 'Oțel',
    finish: 'Zincat',
  });
  expect(
    (
      await adminWrite(api, admin, '/api/v1/admin/variants/' + variantId + '/receipts', {
        requestId: crypto.randomUUID(),
        quantity: 20,
        reference: 'RES-' + stamp,
      })
    ).ok(),
  ).toBe(true);
  return { productId, variantId };
}
test('reservation API is internal, needs its own key and updates availability idempotently', async ({
  request,
}) => {
  const { productId, variantId } = await product(request);
  const data = {
    id: crypto.randomUUID(),
    orderId: crypto.randomUUID(),
    durationSeconds: 120,
    lines: [{ variantId, quantity: 10 }],
  };
  const path = '/internal/reservations';
  try {
    expect((await request.post(inventory + path, { data })).status()).toBe(401);
    expect((await request.post(inventory + path, { headers: reader, data })).status()).toBe(401);
    expect((await request.post('http://localhost:5099' + path, { headers, data })).status()).toBe(
      404,
    );
    for (let i = 0; i < 2; i++)
      expect((await request.post(inventory + path, { headers, data })).status()).toBe(200);
    expect(await stock(request, variantId)).toMatchObject({
      onHand: 20,
      reserved: 10,
      available: 10,
    });
    expect(
      (
        await request.post(inventory + path, {
          headers,
          data: { ...data, lines: [{ variantId, quantity: 11 }] },
        })
      ).status(),
    ).toBe(409);
    expect(
      (
        await request.post(inventory + path + '/' + data.id + '/release', {
          headers,
          data: { orderId: crypto.randomUUID() },
        })
      ).status(),
    ).toBe(409);
    for (let i = 0; i < 2; i++)
      expect(
        (
          await request.post(inventory + path + '/' + data.id + '/release', {
            headers,
            data: { orderId: data.orderId },
          })
        ).status(),
      ).toBe(200);
    expect(await stock(request, variantId)).toMatchObject({
      onHand: 20,
      reserved: 0,
      available: 20,
    });
    expect((await (await request.post(inventory + path, { headers, data })).json()).status).toBe(
      'Released',
    );
    const early = { ...data, id: crypto.randomUUID() };
    expect(
      (
        await request.post(inventory + path + '/' + early.id + '/release', {
          headers,
          data: { orderId: early.orderId },
        })
      ).status(),
    ).toBe(200);
    expect(
      (await (await request.post(inventory + path, { headers, data: early })).json()).status,
    ).toBe('Released');
    expect(await stock(request, variantId)).toMatchObject({ reserved: 0 });
  } finally {
    await request.post(inventory + path + '/' + data.id + '/release', {
      headers,
      data: { orderId: data.orderId },
    });
    await adminWrite(request, admin, '/api/v1/admin/products/' + productId + '/archive', {});
  }
});
test('expiry worker releases persisted reservations without polling the reservation endpoint', async ({
  request,
}) => {
  test.setTimeout(100000);
  const { productId, variantId } = await product(request);
  const data = {
    id: crypto.randomUUID(),
    orderId: crypto.randomUUID(),
    durationSeconds: 60,
    lines: [{ variantId, quantity: 5 }],
  };
  const path = '/internal/reservations';
  try {
    expect((await request.post(inventory + path, { headers, data })).status()).toBe(200);
    expect(await stock(request, variantId)).toMatchObject({ reserved: 5, available: 15 });
    await expect
      .poll(async () => (await stock(request, variantId)).reserved, {
        timeout: 85000,
        intervals: [5000],
      })
      .toBe(0);
    const response = await request.get(inventory + path + '/' + data.id, { headers });
    expect(response.status()).toBe(200);
    expect((await response.json()).status).toBe('Expired');
    expect((await (await request.post(inventory + path, { headers, data })).json()).status).toBe(
      'Expired',
    );
    expect(await stock(request, variantId)).toMatchObject({
      onHand: 20,
      reserved: 0,
      available: 20,
    });
  } finally {
    await request.post(inventory + path + '/' + data.id + '/release', {
      headers,
      data: { orderId: data.orderId },
    });
    await adminWrite(request, admin, '/api/v1/admin/products/' + productId + '/archive', {});
  }
});
