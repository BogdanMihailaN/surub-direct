import { test, expect } from '@playwright/test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { loginAdmin, adminWrite } from './support/admin-session';
const admin = 'http://localhost:4201';
const root = '/api/v1/admin/messaging';
test('monitoring distinguishes outages and replays an audited failure after a lost response', async ({
  page,
  playwright,
}) => {
  test.setTimeout(90000);
  await loginAdmin(page.request, admin);
  const env = Object.fromEntries(
    readFileSync(resolve('../.env'), 'utf8')
      .split('\n')
      .filter((x) => x.includes('='))
      .map((x) => {
        const n = x.indexOf('=');
        return [x.slice(0, n), x.slice(n + 1)];
      }),
  );
  const broker = await playwright.request.newContext({
    httpCredentials: {
      username: 'surub_admin',
      password: env['RABBITMQ_ADMIN_PASSWORD'],
      send: 'always',
    },
  });
  const messageId = crypto.randomUUID();
  const invalidId = crypto.randomUUID();
  const payload = {
    EventId: messageId,
    ReservationId: crypto.randomUUID(),
    OrderId: crypto.randomUUID(),
    Status: 'Consumed',
    ExpiresAt: new Date(Date.now() + 900000).toISOString(),
    OccurredAt: new Date().toISOString(),
    Lines: [{ VariantId: crypto.randomUUID(), Quantity: 1 }],
  };
  try {
    for (const [id, body] of [
      [messageId, JSON.stringify(payload)],
      [invalidId, '{}'],
    ]) {
      const sent = await broker.post(
        'http://127.0.0.1:15672/api/exchanges/surub-direct/inventory.reservations.v1/publish',
        {
          data: {
            properties: {
              message_id: id,
              type: 'inventory.reservation-outcome.v1',
              delivery_mode: 2,
              headers: { attempt: 5 },
            },
            routing_key: 'reservation.outcome.dead',
            payload: body,
            payload_encoding: 'string',
          },
        },
      );
      expect(sent.status()).toBe(200);
      expect((await sent.json()).routed).toBe(true);
    }
  } finally {
    await broker.dispose();
  }
  let record: {
    id: string;
    replayRequestId: string;
    publishedAt: string | null;
    replayReason: string;
    requestedBy: string;
  };
  await expect
    .poll(async () => {
      const data = await (await page.request.get(admin + root)).json();
      return data.items.some((x: { messageId: string }) => x.messageId === messageId);
    })
    .toBe(true);
  const data = await (await page.request.get(admin + root)).json();
  record = data.items.find((x: { messageId: string }) => x.messageId === messageId);
  for (const item of data.items) {
    expect(item).not.toHaveProperty('payload');
    expect(item).not.toHaveProperty('payloadHash');
    expect(item).not.toHaveProperty('fingerprint');
  }
  for (const origin of [admin, 'http://localhost:5104']) {
    expect(
      (
        await page.request.post(origin + root + '/' + record.id + '/replay', {
          data: { requestId: crypto.randomUUID(), reason: 'Dependență reparată' },
        })
      ).status(),
    ).toBe(400);
  }
  await page.goto(admin + '/mesagerie');
  await expect(page.getByRole('heading', { name: 'Mesagerie între servicii' })).toBeVisible();
  await page.getByRole('button', { name: new RegExp(invalidId) }).click();
  await expect(page.getByRole('region', { name: 'Detalii mesaj eșuat' })).toContainText(
    'Contractul este invalid',
  );
  await expect(page.getByRole('button', { name: 'Solicită reluarea', exact: true })).toHaveCount(0);
  await page.getByRole('button', { name: new RegExp(messageId) }).click();
  await expect(page.getByRole('region', { name: 'Detalii mesaj eșuat' })).toContainText(
    'Încercări raportate: 5',
  );
  await page
    .getByLabel('Motivul reluării', { exact: true })
    .fill('Dependență reparată pentru test');
  await expect(page.getByRole('button', { name: 'Solicită reluarea', exact: true })).toBeDisabled();
  await page
    .getByRole('checkbox', { name: 'Am verificat cauza și solicit reluarea acestui mesaj' })
    .check();
  await page.setViewportSize({ width: 390, height: 844 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.screenshot({ path: '../work/messaging-admin-mobile.png', fullPage: true });
  await page.route(
    '**' + root + '/' + record.id + '/replay',
    async (route) => {
      await route.fetch();
      await route.abort('failed');
    },
    { times: 1 },
  );
  await page.getByRole('button', { name: 'Solicită reluarea', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('Actualizează lista');
  await expect
    .poll(
      async () => {
        const items = (await (await page.request.get(admin + root)).json()).items;
        record = items.find((x: { messageId: string }) => x.messageId === messageId);
        return !!record.publishedAt;
      },
      { timeout: 20000 },
    )
    .toBe(true);
  expect(record.replayReason).toBe('Dependență reparată pentru test');
  expect(record.requestedBy).toBeTruthy();
  const token = (await (await page.request.get(admin + '/api/auth/csrf')).json()).token;
  expect(
    (
      await page.request.post('http://localhost:5104' + root + '/' + record.id + '/replay', {
        headers: { 'X-CSRF-TOKEN': token },
        data: { requestId: record.replayRequestId, reason: record.replayReason },
      })
    ).status(),
  ).toBe(200);
  expect(
    (
      await adminWrite(page.request, admin, root + '/' + record.id + '/replay', {
        requestId: crypto.randomUUID(),
        reason: 'Altă cerere',
      })
    ).status(),
  ).toBe(409);
  await page.getByRole('button', { name: 'Actualizează mesageria' }).click();
  await expect(page.getByRole('region', { name: 'Detalii mesaj eșuat' })).toContainText(
    'Aceasta nu confirmă procesarea mesajului',
  );
  await page.route(
    '**' + root + '/broker',
    (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          configured: true,
          available: false,
          ready: null,
          deadReady: null,
          checkedAt: new Date().toISOString(),
        }),
      }),
    { times: 1 },
  );
  await page.route(
    '**' + root + '/inventory-outbox',
    (route) =>
      route.fulfill({
        status: 503,
        contentType: 'application/problem+json',
        body: JSON.stringify({ detail: 'Inventory indisponibil temporar.' }),
      }),
    { times: 1 },
  );
  await page.getByRole('button', { name: 'Actualizează mesageria' }).click();
  await expect(page.getByRole('region', { name: 'Stare broker' })).toContainText(
    'Broker indisponibil',
  );
  await expect(page.getByRole('region', { name: 'Stare Outbox' })).toContainText(
    'Date indisponibile',
  );
  await page.getByRole('button', { name: 'Actualizează mesageria' }).click();
  await expect(page.getByRole('region', { name: 'Stare broker' })).toContainText(
    'Conexiune disponibilă',
  );
});
test('messaging administration requires authentication through gateway and direct service', async ({
  request,
}) => {
  for (const origin of ['http://127.0.0.1:5099', 'http://127.0.0.1:5104']) {
    expect((await request.get(origin + root)).status()).toBe(401);
    expect((await request.get(origin + root + '/broker')).status()).toBe(401);
    expect(
      (
        await request.post(origin + root + '/' + crypto.randomUUID() + '/replay', {
          data: { requestId: crypto.randomUUID(), reason: 'Reparat' },
        })
      ).status(),
    ).toBe(401);
  }
  expect((await request.get('http://127.0.0.1:5102' + root + '/inventory-outbox')).status()).toBe(
    401,
  );
  await loginAdmin(request, 'http://127.0.0.1:5099');
  const response = await request.get('http://127.0.0.1:5104' + root);
  expect(response.status()).toBe(200);
  expect(response.headers()['cache-control']).toBe('no-store');
  expect((await request.get('http://127.0.0.1:5104' + root + '?page=0')).status()).toBe(400);
  expect((await request.get('http://127.0.0.1:5102' + root + '/inventory-outbox')).status()).toBe(
    200,
  );
});
