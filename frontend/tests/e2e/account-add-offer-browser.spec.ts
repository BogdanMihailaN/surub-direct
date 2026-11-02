import { expect, test } from '@playwright/test';
test('catalog saves directly to the account and preserves the separate browser cart', async ({
  page,
  browser,
}) => {
  const origin = 'http://localhost:4200';
  const email = 'catalog-account-' + crypto.randomUUID() + '@example.test';
  const password = 'Catalog-account-123!';
  const write = async (path: string, data: unknown) => {
    const token = (await (await page.request.get(origin + '/api/auth/csrf')).json()).token;
    return page.request.post(origin + path, { headers: { 'X-CSRF-TOKEN': token }, data });
  };
  expect((await write('/api/auth/register', { email, password })).status()).toBe(204);
  expect((await write('/api/auth/login', { email, password })).status()).toBe(200);
  const id = (await (await page.request.get(origin + '/api/auth/me')).json()).id;
  await page.goto('/');
  await page.locator('.product-card').first().click();
  const offer = page.locator('sd-add-to-cart').first();
  await offer.getByLabel('Număr ambalaje').fill('2');
  await offer.getByRole('button', { name: 'Adaugă în coș', exact: true }).click();
  await expect(page.getByRole('link', { name: 'Coș (1)', exact: true })).toBeVisible();
  const guestUrl = origin + '/api/v1/ordering/cart';
  const guest = await (await page.request.get(guestUrl)).json();
  await offer.getByLabel('Număr ambalaje').fill('3');
  await offer.getByRole('button', { name: 'Adaugă în cont', exact: true }).click();
  const confirmation = offer.getByRole('group', { name: 'Confirmare adăugare în cont' });
  await expect(confirmation).toContainText(email);
  await page.setViewportSize({ width: 390, height: 844 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await offer
    .locator('sd-add-to-account')
    .screenshot({ path: '../work/account-add-offer-mobile.png' });
  await confirmation.getByRole('button', { name: 'Confirmă cantitatea în cont' }).click();
  await expect(offer.locator('sd-add-to-account')).toContainText('Cantitatea a fost salvată');
  const accountUrl =
    origin + '/api/v1/ordering/account/cart?expectedAccountId=' + encodeURIComponent(id);
  const account = await (await page.request.get(accountUrl)).json();
  expect(account.lines).toEqual([{ ...guest.lines[0], packs: 3 }]);
  expect((await (await page.request.get(guestUrl)).json()).lines).toEqual(guest.lines);
  await page.reload();
  await offer.getByLabel('Număr ambalaje').fill('4');
  await offer.getByRole('button', { name: 'Adaugă în cont', exact: true }).click();
  await expect(confirmation).toContainText('Înlocuiește cantitatea existentă de 3');
  await confirmation.getByRole('button', { name: 'Confirmă cantitatea în cont' }).click();
  await expect(offer.locator('sd-add-to-account')).toContainText('Cantitatea a fost salvată');
  const other = await browser.newContext();
  try {
    const csrf = (await (await other.request.get(origin + '/api/auth/csrf')).json()).token;
    expect(
      (
        await other.request.post(origin + '/api/auth/login', {
          headers: { 'X-CSRF-TOKEN': csrf },
          data: { email, password },
        })
      ).status(),
    ).toBe(200);
    const otherPage = await other.newPage();
    await otherPage.goto(origin + '/cont');
    await expect(otherPage.locator('sd-account-cart sd-cart-line input')).toHaveValue('4');
    expect((await (await page.request.get(guestUrl)).json()).lines).toEqual(guest.lines);
  } finally {
    await other.close();
  }
});
