import { test, expect } from '@playwright/test';

test('filters reuse taxonomy within a catalog visit and re-entry refreshes it', async ({
  page,
}) => {
  let taxonomyRequests = 0;
  const searches: string[] = [];
  await page.route('**/api/v1/taxonomy', (route) => {
    taxonomyRequests++;
    return route.fulfill({ json: { categories: [], attributes: [] } });
  });
  await page.route('**/api/v1/products?**', (route) => {
    searches.push(new URL(route.request().url()).searchParams.get('search') ?? '');
    return route.fulfill({ json: { items: [], total: 0, page: 1, pageSize: 12 } });
  });
  await page.goto('/');
  await expect(page.getByText('Nu am găsit produse pentru aceste filtre.')).toBeVisible();
  for (const search of ['M8', 'M10']) {
    await page.getByLabel('Caută produse').fill(search);
    await page.getByRole('button', { name: 'Caută', exact: false }).click();
    await expect.poll(() => searches.at(-1)).toBe(search);
    await expect(page.getByText('Nu am găsit produse pentru aceste filtre.')).toBeVisible();
  }
  expect(taxonomyRequests).toBe(1);
  await page.reload();
  await expect(page.getByText('Nu am găsit produse pentru aceste filtre.')).toBeVisible();
  expect(taxonomyRequests).toBe(2);
});

test('failed taxonomy is retried and explicit product retry refreshes taxonomy', async ({
  page,
}) => {
  let taxonomyRequests = 0;
  let failProducts = false;
  await page.route('**/api/v1/taxonomy', (route) => {
    taxonomyRequests++;
    return taxonomyRequests === 1
      ? route.fulfill({ status: 503, json: { title: 'Indisponibil temporar' } })
      : route.fulfill({ json: { categories: [], attributes: [] } });
  });
  await page.route('**/api/v1/products?**', (route) =>
    failProducts
      ? route.fulfill({ status: 503, json: { title: 'Indisponibil temporar' } })
      : route.fulfill({ json: { items: [], total: 0, page: 1, pageSize: 12 } }),
  );
  await page.goto('/');
  await page.getByRole('button', { name: 'Reîncearcă' }).click();
  await expect(page.getByText('Nu am găsit produse pentru aceste filtre.')).toBeVisible();
  expect(taxonomyRequests).toBe(2);
  failProducts = true;
  await page.getByLabel('Caută produse').fill('M8');
  await page.getByRole('button', { name: 'Caută', exact: false }).click();
  await expect(page.getByRole('button', { name: 'Reîncearcă' })).toBeVisible();
  expect(taxonomyRequests).toBe(2);
  failProducts = false;
  await page.getByRole('button', { name: 'Reîncearcă' }).click();
  await expect(page.getByText('Nu am găsit produse pentru aceste filtre.')).toBeVisible();
  expect(taxonomyRequests).toBe(3);
});
