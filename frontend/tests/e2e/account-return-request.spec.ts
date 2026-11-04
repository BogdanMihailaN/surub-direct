import { test, expect } from '@playwright/test';
import { adminWrite } from './support/admin-session';
import { createDeliveredReturnOrder } from './support/delivered-return-order';

test('owner requests a return once, sees it after lost response, and cannot affect stock or another account', async ({ page, browser, playwright }) => {
  test.setTimeout(120000);
  const order = await createDeliveredReturnOrder(page);
  const origin = 'http://localhost:4200';
  const path = '/api/v1/ordering/account/orders/' + order.id + '/return';
  const context = await browser.newContext({ storageState: await page.request.storageState() });
  const customer = context.request;
  const customerPage = await context.newPage();
  const anonymous = await playwright.request.newContext();
  const customerWrite = async (base: string, data: unknown) => {
    const csrf = await (await customer.get(origin + '/api/auth/csrf')).json();
    return customer.post(base + path, { data, headers: { 'X-CSRF-TOKEN': csrf.token } });
  };
  const credentials = { email: `return-${crypto.randomUUID()}@example.test`, password: 'Return-test-123!Secure' };
  try {
    for (const base of [origin, 'http://localhost:5104']) {
      expect((await anonymous.post(base + path, { data: {} })).status()).toBe(401);
    }
    expect((await adminWrite(customer, origin, '/api/auth/logout')).status()).toBe(204);
    expect((await adminWrite(customer, origin, '/api/auth/register', credentials)).status()).toBe(204);
    expect((await adminWrite(customer, origin, '/api/auth/login', credentials)).status()).toBe(200);
    const account = await (await customer.get(origin + '/api/auth/me')).json();
    expect((await adminWrite(customer, origin, '/api/v1/ordering/account/orders/' + order.id + '/claim?expectedAccountId=' + account.id)).status()).toBe(204);
    for (const base of [origin, 'http://localhost:5104']) {
      expect((await customer.post(base + path, { data: {} })).status()).toBe(400);
      expect((await customerWrite(base, { operationId: crypto.randomUUID(), expectedAccountId: 'wrong-account', reason: 'Retur integral' })).status()).toBe(409);
    }
    await customerPage.goto(origin + '/cont');
    await customerPage.getByRole('button', { name: 'Solicită retur pentru ' + order.number, exact: true }).click();
    const form = customerPage.getByRole('region', { name: 'Solicitare retur integral', exact: true });
    await expect(form).toContainText(credentials.email);
    await expect(form.getByRole('button', { name: 'Trimite solicitarea de retur' })).toBeDisabled();
    await form.getByLabel('Motivul returului integral').fill('Nu mai am nevoie de produsele comandate');
    await form.getByLabel('Confirm comanda, contul și solicitarea de retur integral').check();
    let request: Record<string, unknown> | undefined;
    let writes = 0;
    await customerPage.route('**' + path, async route => {
      writes++;
      request = route.request().postDataJSON();
      expect(request!.expectedAccountId).toBe(account.id);
      expect((await route.fetch()).status()).toBe(204);
      await route.abort('failed');
    }, { times: 1 });
    await form.getByRole('button', { name: 'Trimite solicitarea de retur' }).click();
    await expect(customerPage.getByRole('region', { name: 'Starea returului demo' })).toContainText('Solicitat');
    expect(writes).toBe(1);
    await expect(customerPage.getByRole('button', { name: 'Solicită retur pentru ' + order.number, exact: true })).toHaveCount(0);
    const detailPath = '/api/v1/admin/sales-orders/' + order.id;
    const detail = await (await page.request.get('http://localhost:4201' + detailPath)).json();
    expect(detail.return.history).toHaveLength(1);
    expect(detail.return.history[0].actor).toBe('customer:' + account.id);
    expect(detail.reservation.status).toBe('Consumed');
    expect(detail.paymentSimulation.status).toBe('Paid');
    await page.goto('http://localhost:4201/comenzi');
    await page.getByLabel('Număr comandă', { exact: true }).fill(order.number);
    await page.getByLabel('Stare retur', { exact: true }).selectOption('Requested');
    await page.getByRole('button', { name: 'Aplică filtrele' }).click();
    const queue = page.getByLabel('Lista comenzilor', { exact: true });
    await expect(queue.getByRole('button')).toHaveCount(1);
    await expect(queue).toContainText('Retur: Solicitat');
    await queue.getByRole('button').click();
    const review = page.getByRole('region', { name: 'Retur integral demonstrativ', exact: true });
    await review.getByLabel('Motivul operației de retur').fill('Verificat de magazin');
    await review.getByLabel('Confirm operația de retur integral', { exact: true }).check();
    await review.getByRole('button', { name: 'Aprobă returul integral' }).click();
    await expect(review.getByRole('status')).toContainText('Aprobat');
    await expect(queue).toContainText('Nu există comenzi pentru filtrele alese.');
    await page.getByLabel('Stare retur', { exact: true }).selectOption('Approved');
    await page.getByRole('button', { name: 'Aplică filtrele' }).click();
    await expect(queue.getByRole('button')).toHaveCount(1);
    await expect(queue).toContainText('Retur: Aprobat');
    await page.setViewportSize({ width: 390, height: 844 });
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    await page.screenshot({ path: '../work/admin-return-queue-mobile.png', fullPage: true });
    expect((await adminWrite(customer, origin, path, request)).status()).toBe(204);
    await customerPage.getByRole('button', { name: 'Actualizează istoricul', exact: true }).click();
    await expect(customerPage.getByRole('region', { name: 'Starea returului demo' })).toContainText('Aprobat');
    await customerPage.setViewportSize({ width: 390, height: 844 });
    expect(await customerPage.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    await customerPage.screenshot({ path: '../work/account-return-mobile.png', fullPage: true });
    expect((await adminWrite(customer, origin, '/api/auth/logout')).status()).toBe(204);
    const other = { ...credentials, email: `other-return-${crypto.randomUUID()}@example.test` };
    await adminWrite(customer, origin, '/api/auth/register', other);
    await adminWrite(customer, origin, '/api/auth/login', other);
    const otherAccount = await (await customer.get(origin + '/api/auth/me')).json();
    for (const base of [origin, 'http://localhost:5104']) {
      expect((await customerWrite(base, { ...request, expectedAccountId: otherAccount.id })).status()).toBe(404);
    }
  } finally {
    await context.close(); await anonymous.dispose(); await order.archive();
  }
});
