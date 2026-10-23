import { test, expect } from '@playwright/test';
test('retained message keeps audit visible and does not offer another replay', async ({ page }) => {
  const id = crypto.randomUUID();
  const messageId = crypto.randomUUID();
  const date = '2026-08-01T10:00:00Z';
  const item = {
    id,
    messageId,
    messageType: 'inventory.reservation-outcome.v1',
    previousReplayId: null,
    originalSize: 500,
    attempts: 5,
    canReplay: true,
    capturedAt: date,
    replayRequestId: crypto.randomUUID(),
    replayReason: 'Conexiune reparată',
    requestedBy: 'administrator',
    requestedAt: date,
    publishedAt: date,
    payloadRemovedAt: '2026-10-23T10:00:00Z',
  };
  await page.route('**/api/auth/me', (route) => route.fulfill({ json: { isAdmin: true } }));
  await page.route('**/api/v1/admin/messaging**', (route) => {
    const path = new URL(route.request().url()).pathname;
    return route.fulfill({
      json: path.endsWith('/broker')
        ? { configured: true, available: true, ready: 0, deadReady: 0, checkedAt: date }
        : path.endsWith('/inventory-outbox')
          ? { pending: 0, oldestPendingAt: null, lastPublishedAt: date }
          : {
              inboxTotal: 1,
              lastReceivedAt: date,
              awaitingReview: 0,
              pendingReplays: 0,
              items: [item],
              total: 1,
              page: 1,
              pageSize: 25,
            },
    });
  });
  await page.goto('http://localhost:4201/mesagerie');
  await page.getByRole('button', { name: new RegExp(messageId) }).click();
  const detail = page.getByRole('region', { name: 'Detalii mesaj eșuat' });
  await expect(detail).toContainText('Corpul mesajului a fost eliminat prin retenție');
  await expect(detail).toContainText('23.10.2026');
  await expect(detail).toContainText('hash-ul au fost confirmate în Inbox');
  await expect(detail).toContainText('aceasta nu confirmă plata sau livrarea');
  await expect(detail).toContainText('Conexiune reparată');
  await expect(detail).toContainText('administrator');
  await expect(detail).toContainText('500 octeți');
  await expect(page.getByRole('button', { name: 'Solicită reluarea', exact: true })).toHaveCount(0);
});
