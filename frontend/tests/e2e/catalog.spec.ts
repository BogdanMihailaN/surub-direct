import { test, expect } from '@playwright/test';
test('catalog, dimensional filters and product details', async ({ page }) => {
  const errors: string[] = [];
  page.on('pageerror', (e) => errors.push(e.message));
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Catalog produse.' })).toBeVisible();
  await expect(page.locator('.product-card')).toHaveCount(12);
  await page.getByLabel('Diametru (mm)').fill('8');
  await page.getByLabel('Lungime (mm)').fill('60');
  await page.getByRole('button', { name: 'Aplică filtrele' }).click();
  await expect(page.locator('.product-card')).toHaveCount(1);
  await page.locator('.product-card').click();
  await expect(page.getByRole('heading', { name: 'Variante și ambalaje' })).toBeVisible();
  await expect(page.getByText('SH-M8-60-ZN', { exact: true })).toBeVisible();
  expect(errors).toEqual([]);
});
test('mobile layout stays within viewport', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/');
  await expect(page.locator('.product-card').first()).toBeVisible();
  expect(
    await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth),
  ).toBeTruthy();
});
test('unknown SKU does not silently return the full catalog', async ({ page }) => {
  await page.goto('/?q=NO-SUCH-SKU-XYZ');
  await expect(page.getByText('Nu am găsit produse pentru aceste filtre.')).toBeVisible();
});

test('a failed catalog request can be retried without changing the URL', async ({ page }) => {
  await page.goto('/');
  await expect(page.locator('.product-card').first()).toBeVisible();
  await page.route('**/api/v1/products?**', (route) =>
    route.fulfill({
      status: 503,
      contentType: 'application/problem+json',
      body: JSON.stringify({ title: 'Indisponibil temporar' }),
    }),
  );
  await page.getByLabel('Caută produse').fill('SH-M8-60-ZN');
  await page.getByRole('button', { name: 'Caută', exact: false }).click();
  await expect(page.getByRole('button', { name: 'Reîncearcă' })).toBeVisible();
  const url = page.url();
  await page.unroute('**/api/v1/products?**');
  await page.getByRole('button', { name: 'Reîncearcă' }).click();
  await expect(page.locator('.product-card')).toHaveCount(1);
  await expect(page).toHaveURL(url);
});
