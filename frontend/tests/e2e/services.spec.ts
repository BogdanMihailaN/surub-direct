import { test, expect } from '@playwright/test';
import { adminCredentials } from './support/admin-session';

test('gateway exposes business routes but never internal service routes', async ({ request }) => {
  const products = await request.get('http://127.0.0.1:5099/api/v1/products');
  expect(products.status()).toBe(200);
  expect((await products.json()).items.length).toBeGreaterThan(0);
  expect((await request.post('http://127.0.0.1:5099/internal/session')).status()).toBe(404);
  expect((await request.get('http://127.0.0.1:5099/api/v1/ordering/capabilities')).status()).toBe(
    200,
  );
  expect(
    (await (await request.get('http://127.0.0.1:5099/api/v1/ordering/capabilities')).json())
      .checkoutEnabled,
  ).toBe(false);
});

test('services protect direct access and internal APIs independently of gateway', async ({
  request,
}) => {
  expect((await request.get('http://127.0.0.1:5101/api/v1/admin/products')).status()).toBe(401);
  for (const port of [5099, 5101]) {
    const url = `http://127.0.0.1:${port}/api/v1/admin/products/00000000-0000-0000-0000-000000000001`;
    expect((await request.get(url)).status()).toBe(401);
    expect(
      (
        await request.put(url, { data: { name: 'Test', category: 'Test', description: 'Test' } })
      ).status(),
    ).toBe(401);
    expect((await request.post(url + '/archive')).status()).toBe(401);
    expect((await request.post(url + '/restore')).status()).toBe(401);
    const variantUrl = url + '/variants/00000000-0000-0000-0000-000000000002';
    expect(
      (
        await request.put(variantUrl, {
          data: { label: 'Test', diameterMm: 8, lengthMm: 40, material: 'Oțel', finish: 'Zincat' },
        })
      ).status(),
    ).toBe(401);
    expect(
      (
        await request.put(variantUrl + '/offers/00000000-0000-0000-0000-000000000003', {
          data: { label: 'Cutie', unitsPerPack: 100, minPacks: 1, packStep: 1 },
        })
      ).status(),
    ).toBe(401);
  }
  expect(
    (
      await request.post(
        'http://127.0.0.1:5102/api/v1/admin/variants/00000000-0000-0000-0000-000000000001/receipts',
        {
          data: {
            requestId: '00000000-0000-0000-0000-000000000002',
            quantity: 1,
            reference: 'test',
          },
        },
      )
    ).status(),
  ).toBe(401);
  expect(
    (
      await request.post('http://127.0.0.1:5102/internal/stock/query', { data: { variantIds: [] } })
    ).status(),
  ).toBe(401);
  expect(
    (
      await request.get(
        'http://127.0.0.1:5101/internal/variants/00000000-0000-0000-0000-000000000001',
      )
    ).status(),
  ).toBe(401);
  expect((await request.post('http://127.0.0.1:5103/internal/session')).status()).toBe(401);
});

test('an authenticated session cannot bypass CSRF on either service', async ({ request }) => {
  const csrf = await (await request.get('http://127.0.0.1:5099/api/auth/csrf')).json();
  const login = await request.post('http://127.0.0.1:5099/api/auth/login', {
    data: { email: adminCredentials.email, password: adminCredentials.password },
    headers: { 'X-CSRF-TOKEN': csrf.token },
  });
  expect(login.status()).toBe(200);
  expect((await request.get('http://127.0.0.1:5101/api/v1/admin/products')).status()).toBe(200);
  const catalog = await request.post('http://127.0.0.1:5101/api/v1/admin/products', {
    data: {
      name: 'Must not exist',
      slug: 'must-not-exist',
      category: 'Test',
      description: 'No CSRF',
    },
  });
  expect(catalog.status()).toBe(400);
  for (const port of [5099, 5101]) {
    const url = `http://127.0.0.1:${port}/api/v1/admin/products/00000000-0000-0000-0000-000000000001`;
    expect(
      (
        await request.put(url, { data: { name: 'Test', category: 'Test', description: 'Test' } })
      ).status(),
    ).toBe(400);
    expect((await request.post(url + '/archive')).status()).toBe(400);
    expect((await request.post(url + '/restore')).status()).toBe(400);
    const variantUrl = url + '/variants/00000000-0000-0000-0000-000000000002';
    expect(
      (
        await request.put(variantUrl, {
          data: { label: 'Test', diameterMm: 8, lengthMm: 40, material: 'Oțel', finish: 'Zincat' },
        })
      ).status(),
    ).toBe(400);
    expect(
      (
        await request.put(variantUrl + '/offers/00000000-0000-0000-0000-000000000003', {
          data: { label: 'Cutie', unitsPerPack: 100, minPacks: 1, packStep: 1 },
        })
      ).status(),
    ).toBe(400);
  }
  const inventory = await request.post(
    'http://127.0.0.1:5102/api/v1/admin/variants/00000000-0000-0000-0000-000000000001/receipts',
    { data: { requestId: '00000000-0000-0000-0000-000000000002', quantity: 1, reference: 'test' } },
  );
  expect(inventory.status()).toBe(400);
});
