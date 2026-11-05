import { test, expect } from '@playwright/test';
import { createDeliveredReturnOrder } from './support/delivered-return-order';

test('admin selects packs, approves and receives partial return with lost responses and safe read recovery', async ({ page }) => {
  test.setTimeout(120000);
  const order = await createDeliveredReturnOrder(page);
  const root = '/api/v1/admin/sales-orders/' + order.id;
  const path = root + '/partial-returns';
  const origin = 'http://localhost:4201';
  try {
    await page.goto(origin + '/comenzi');
    await page.getByLabel('Număr comandă', { exact: true }).fill(order.number);
    await page.getByRole('button', { name: 'Aplică filtrele', exact: true }).click();
    await expect(page.getByLabel('Lista comenzilor', { exact: true }).getByRole('button')).toHaveCount(1);
    await page.getByLabel('Lista comenzilor', { exact: true }).getByRole('button', { name: new RegExp(order.number) }).click();
    const panel = page.getByRole('region', { name: 'Retururi parțiale demonstrative', exact: true });
    const form = panel.getByRole('form', { name: 'Cerere nouă de retur parțial', exact: true });
    const packs = form.getByRole('spinbutton').first();
    await packs.fill('999');
    await form.getByLabel('Motivul cererii parțiale').fill('Retur pentru o parte din produse');
    await form.getByLabel('Confirm produsele și cantitățile pentru această comandă').check();
    await expect(form.getByRole('button', { name: 'Înregistrează returul parțial' })).toBeDisabled();
    await packs.fill('1');
    let creates = 0;
    await page.route('**' + path, async route => {
      if (route.request().method() !== 'POST') { await route.continue(); return; }
      creates++;
      expect((await route.fetch()).status()).toBe(200);
      await route.abort('failed');
    });
    await form.getByRole('button', { name: 'Înregistrează returul parțial' }).click();
    const card = panel.locator('sd-partial-return-card').first();
    await expect(card.getByRole('status')).toContainText('Solicitat');
    expect(creates).toBe(1);
    await page.unroute('**' + path);
    await expect(page.getByRole('button', { name: 'Înregistrează solicitarea de retur', exact: true })).toHaveCount(0);
    await card.getByLabel('Motivul operației pentru această cerere').fill('Cantități verificate');
    await card.getByLabel('Confirm operația pentru această cerere', { exact: true }).check();
    await card.getByRole('button', { name: 'Aprobă cererea parțială' }).click();
    await expect(card.getByRole('status')).toContainText('Aprobat');
    await card.getByLabel('Motivul operației pentru această cerere').fill('Produsele au fost primite');
    await card.getByLabel('Confirm operația pentru această cerere', { exact: true }).check();
    await expect(card.getByRole('button', { name: 'Confirmă recepția parțială' })).toBeDisabled();
    await card.getByLabel('Confirm primirea tuturor cantităților acestei cereri în stare revandabilă').check();
    const request = (await (await page.request.get(origin + path)).json())[0];
    const ledgerPath = '/api/v1/admin/variants/' + request.lines[0].variantId + '/movements';
    const before = await (await page.request.get(origin + ledgerPath)).json();
    let receipts = 0;
    await page.route('**' + path + '/' + request.id + '/receive', async route => {
      receipts++;
      expect((await route.fetch()).status()).toBe(204);
      await route.abort('failed');
    });
    await card.getByRole('button', { name: 'Confirmă recepția parțială' }).click();
    await expect.poll(async () => {
      await page.getByRole('button', { name: 'Actualizează detaliile', exact: true }).click();
      return (await (await page.request.get(origin + path)).json())[0].status;
    }, { timeout: 30000 }).toBe('Restored');
    await page.getByRole('button', { name: 'Actualizează detaliile', exact: true }).click();
    await expect(card.getByRole('status')).toContainText('Stoc restituit');
    expect(receipts).toBe(1);
    const after = await (await page.request.get(origin + ledgerPath)).json();
    expect(after.onHand).toBe(before.onHand + request.lines[0].quantity);
    expect(after.items.filter((x: { kind: string; id: string }) => x.kind === 'ReturnReceipt' && x.id === request.id)).toHaveLength(1);
    await page.route('**' + path, route => route.fulfill({ status: 503, json: { detail: 'Retururile sunt indisponibile temporar.' } }), { times: 1 });
    await page.getByRole('button', { name: 'Actualizează detaliile', exact: true }).click();
    await expect(page.getByRole('alert').filter({ hasText: 'Retururile sunt indisponibile temporar.' })).toBeVisible();
    await expect(panel).toHaveCount(0);
    await expect(page.getByRole('region', { name: 'Detalii comandă', exact: true })).toHaveCount(0);
    await page.getByRole('button', { name: 'Actualizează detaliile', exact: true }).click();
    await expect(card.getByRole('status')).toContainText('Stoc restituit');
    await form.getByRole('spinbutton').first().fill('1');
    await form.getByLabel('Motivul cererii parțiale').fill('A doua solicitare demonstrativă');
    await form.getByLabel('Confirm produsele și cantitățile pentru această comandă').check();
    await form.getByRole('button', { name: 'Înregistrează returul parțial' }).click();
    await expect(card.getByRole('status')).toContainText('Solicitat');
    await card.getByLabel('Motivul operației pentru această cerere').fill('Cerere respinsă după verificare');
    await card.getByLabel('Confirm operația pentru această cerere', { exact: true }).check();
    await card.getByRole('button', { name: 'Respinge cererea parțială' }).click();
    await expect(card.getByRole('status')).toContainText('Respins');
    await expect(form.getByRole('spinbutton').first()).toBeEnabled();
    await page.setViewportSize({ width: 390, height: 844 });
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    await panel.screenshot({ path: '../work/partial-return-ui-mobile.png' });
  } finally { await order.archive(); }
});
