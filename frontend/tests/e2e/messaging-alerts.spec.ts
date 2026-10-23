import { test, expect } from '@playwright/test';
test('operational warnings use global totals, thresholds and recover after refresh', async ({
  page,
}) => {
  const now = Date.parse('2026-10-23T10:00:00Z');
  await page.clock.setFixedTime(now);
  await page.route('**/api/auth/me', (route) => route.fulfill({ json: { isAdmin: true } }));
  let outbox: object = {
    pending: 2,
    oldestPendingAt: new Date(now - 300000).toISOString(),
    lastPublishedAt: null,
  };
  let broker: object = {
    configured: true,
    available: true,
    ready: 100,
    deadReady: 1,
    checkedAt: new Date(now).toISOString(),
  };
  let data = {
    inboxTotal: 1,
    lastReceivedAt: null,
    awaitingReview: 26,
    pendingReplays: 2,
    items: [],
    total: 26,
    page: 1,
    pageSize: 25,
  };
  let unavailable = false;
  await page.route('**/api/v1/admin/messaging**', (route) => {
    const path = new URL(route.request().url()).pathname;
    if (unavailable)
      return route.fulfill({ status: 503, json: { detail: 'Temporar indisponibil.' } });
    return route.fulfill({
      json: path.endsWith('/broker') ? broker : path.endsWith('/inventory-outbox') ? outbox : data,
    });
  });
  await page.goto('http://localhost:4201/mesagerie');
  const alerts = page.getByRole('region', { name: 'Avertizări operaționale' });
  await expect(alerts).toContainText('Publicarea din Inventory întârzie');
  await expect(alerts).toContainText('26 mesaje fără cerere');
  await expect(alerts).toContainText('2 cereri salvate');
  await expect(alerts).toContainText('Coada de procesare este aglomerată');
  await expect(alerts).toContainText('Mesaje în coada de erori');
  await expect(alerts.locator('article').first()).toContainText('Critic');
  await page.setViewportSize({ width: 390, height: 844 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.screenshot({ path: '../work/messaging-alerts-mobile.png', fullPage: true });

  data = { ...data, awaitingReview: 0, pendingReplays: 0, total: 0 };
  broker = { ...broker, ready: 99, deadReady: 0 };
  outbox = { ...outbox, oldestPendingAt: new Date(now - 299999).toISOString() };
  await page.getByRole('button', { name: 'Actualizează mesageria' }).click();
  await expect(alerts).toContainText('Nu sunt avertizări la ultima verificare');
  unavailable = true;
  await page.getByRole('button', { name: 'Actualizează mesageria' }).click();
  await expect(alerts.locator('article')).toHaveCount(3);
  await expect(alerts).toContainText('Starea brokerului nu poate fi verificată');
  await expect(alerts).toContainText('Registrul Ordering nu poate fi verificat');
  await expect(alerts).toContainText('Outbox Inventory nu poate fi verificat');
  await expect(alerts).not.toContainText('Nu sunt avertizări');

  unavailable = false;
  broker = { ...broker, available: false, ready: null, deadReady: null };
  outbox = { ...outbox, oldestPendingAt: null };
  await page.getByRole('button', { name: 'Actualizează mesageria' }).click();
  await expect(alerts).toContainText('Broker indisponibil');
  await expect(alerts).toContainText('Vechimea mesajelor Outbox este necunoscută');
  for (const oldestPendingAt of ['invalid-date', new Date(now + 60000).toISOString()]) {
    outbox = { ...outbox, oldestPendingAt };
    await page.getByRole('button', { name: 'Actualizează mesageria' }).click();
    await expect(alerts).toContainText('Vechimea mesajelor Outbox este necunoscută');
    await expect(alerts).not.toContainText('Publicarea din Inventory întârzie');
  }
  broker = { ...broker, configured: false };
  outbox = { ...outbox, pending: 0 };
  await page.getByRole('button', { name: 'Actualizează mesageria' }).click();
  await expect(alerts).toContainText('Mesageria nu este configurată');
  await expect(alerts.locator('article')).toHaveCount(1);
  broker = { ...broker, configured: true, available: true, ready: 0, deadReady: 0 };
  await page.getByRole('button', { name: 'Actualizează mesageria' }).click();
  await expect(alerts).toContainText('Nu sunt avertizări la ultima verificare');
});
