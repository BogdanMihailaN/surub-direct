import { test, expect } from '@playwright/test';

test('product reserves loading space and retries a failure on the same URL', async ({ page }) => {
  const response = await page.request.get('/api/v1/products?search=SH-M8-60-ZN');
  expect(response.ok()).toBe(true);
  const slug = (await response.json()).items[0].product.slug;
  const endpoint = '**/api/v1/products/' + slug;
  let release!: () => void;
  const gate = new Promise<void>((resolve) => {
    release = resolve;
  });
  await page.route(endpoint, async (route) => {
    await gate;
    await route.fulfill({ status: 503, json: { title: 'Indisponibil temporar' } });
  });
  try {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto('/produse/' + slug);
    await expect(page.getByRole('status')).toHaveText('Se încarcă produsul…');
    await expect(page.locator('.detail')).toHaveAttribute('aria-busy', 'true');
    expect(
      await page.locator('sd-product-loading').evaluate((el) => el.getBoundingClientRect().height),
    ).toBeGreaterThan(500);
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(
      true,
    );
    await page.screenshot({ path: '../work/product-loading-mobile.png', fullPage: true });
  } finally {
    release();
  }
  await expect(page.getByRole('alert')).toBeVisible();
  await expect(page.locator('.detail')).toHaveAttribute('aria-busy', 'false');
  await expect(page.getByRole('button', { name: 'Adaugă în coș', exact: true })).toHaveCount(0);
  const url = page.url();
  await page.unroute(endpoint);
  await page.getByRole('button', { name: 'Reîncearcă', exact: true }).click();
  await expect(page.getByText('SH-M8-60-ZN', { exact: true })).toBeVisible();
  await expect(page).toHaveURL(url);
  await expect(page.locator('sd-product-loading')).toHaveCount(0);
  await expect(
    page.getByRole('button', { name: 'Adaugă în coș', exact: true }).first(),
  ).toBeVisible();
  await page.goto('/produse/no-such-product-loading-test');
  await expect(page.getByRole('alert')).toHaveText('Produsul nu există sau nu este publicat.');
  await expect(page).toHaveTitle('Produs | Șurub Direct');
  await expect(page.getByRole('button', { name: 'Adaugă în coș', exact: true })).toHaveCount(0);
});
