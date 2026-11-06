import { test, expect } from '@playwright/test';
import { adminWrite } from './support/admin-session';
import { createDeliveredReturnOrder } from './support/delivered-return-order';

test('owner requests selected packs, recovers a lost response and sees the administrative decision privately', async ({ page, browser, playwright }) => {
  test.setTimeout(120000);
  const order = await createDeliveredReturnOrder(page);
  const shop = 'http://localhost:4200';
  const path = `/api/v1/ordering/account/orders/${order.id}/partial-returns`;
  const adminPath = `/api/v1/admin/sales-orders/${order.id}/partial-returns`;
  const context = await browser.newContext({ storageState: await page.request.storageState() });
  const customer = context.request;
  const anonymous = await playwright.request.newContext();
  const bases = [shop, 'http://localhost:5104'];
  const register = async () => {
    const credentials = { email: `partial-${crypto.randomUUID()}@example.test`, password: 'Return-test-123!Secure' };
    expect((await adminWrite(customer, shop, '/api/auth/register', credentials)).status()).toBe(204);
    expect((await adminWrite(customer, shop, '/api/auth/login', credentials)).status()).toBe(200);
    return (await customer.get(shop + '/api/auth/me')).json();
  };
  try {
    for (const base of bases) {
      expect((await anonymous.get(base + path)).status()).toBe(401);
      expect((await anonymous.post(base + path, { data: {} })).status()).toBe(401);
    }
    await adminWrite(customer, shop, '/api/auth/logout');
    const owner = await register();
    expect((await adminWrite(customer, shop, `/api/v1/ordering/account/orders/${order.id}/claim?expectedAccountId=${owner.id}`)).status()).toBe(204);
    for (const base of bases) {
      expect((await customer.get(base + path)).status()).toBe(400);
      expect((await customer.get(base + path + '?expectedAccountId=other')).status()).toBe(409);
      expect((await customer.post(base + path, { data: {} })).status()).toBe(400);
    }
    const view = await context.newPage();
    await view.goto(shop + '/cont');
    await view.getByRole('button', { name: 'Retururi parțiale pentru ' + order.number, exact: true }).click();
    const panel = view.getByRole('region', { name: 'Retururile parțiale ale comenzii' });
    const form = panel.getByRole('form', { name: 'Solicitare retur parțial' });
    await expect(form).toContainText(owner.email);
    const packs = form.getByRole('spinbutton').first();
    const available = Number(await packs.getAttribute('max'));
    await packs.fill(String(available + 1));
    await form.getByLabel('Motivul returului parțial').fill('Un ambalaj rămas nefolosit');
    await form.getByLabel('Confirm contul, comanda și cantitățile selectate').check();
    const submit = form.getByRole('button', { name: 'Trimite cererea parțială' });
    await expect(submit).toBeDisabled();
    await packs.fill('1');
    await view.route('**' + path, async route => {
      expect((await route.fetch()).status()).toBe(200);
      await route.abort('failed');
    }, { times: 1 });
    await submit.click();
    await expect(panel.locator('article')).toHaveCount(1);
    await expect(panel.locator('article')).toContainText('Solicitat');
    await expect(packs).toHaveAttribute('max', String(available - 1));
    const admin = 'http://localhost:4201';
    const [saved] = await (await page.request.get(admin + adminPath)).json();
    expect(saved.actor).toBe('customer:' + owner.id);
    expect((await adminWrite(page.request, admin, `${adminPath}/${saved.id}/decision`, {
      operationId: crypto.randomUUID(), expectedVersion: 0, target: 'Approved', reason: 'Motiv intern verificare',
    })).status()).toBe(204);
    await panel.getByRole('button', { name: 'Actualizează retururile parțiale' }).click();
    await expect(panel.locator('article')).toContainText('Aprobat');
    const summary = await (await customer.get(shop + path + '?expectedAccountId=' + owner.id)).json();
    expect(Object.keys(summary.items[0]).sort()).toEqual(['createdAt', 'id', 'inventoryReceivedAt', 'lines', 'status']);
    await view.setViewportSize({ width: 390, height: 844 });
    expect(await view.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    await panel.screenshot({ path: '../work/account-partial-return-mobile.png' });
    await adminWrite(customer, shop, '/api/auth/logout');
    const other = await register();
    const csrf = await (await customer.get(shop + '/api/auth/csrf')).json();
    for (const base of bases) {
      expect((await customer.get(base + path + '?expectedAccountId=' + other.id)).status()).toBe(404);
      expect((await customer.post(base + path, { headers: { 'X-CSRF-TOKEN': csrf.token }, data: {
        expectedAccountId: other.id, requestId: saved.id, lines: [{ offerId: saved.lines[0].offerId, packs: 1 }], reason: saved.reason,
      } })).status()).toBe(404);
    }
  } finally { await anonymous.dispose(); await context.close(); await order.archive(); }
});
