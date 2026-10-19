import { test, expect } from '@playwright/test';
import { adminWrite, loginAdmin } from './support/admin-session';
const origin = 'http://localhost:4201';

test('product images can be uploaded, arranged, edited, published and removed', async ({
  page,
}) => {
  const token = Date.now().toString();
  const sku = 'IMG-' + token;
  const errors: string[] = [];
  page.on('pageerror', (error) => errors.push(error.message));
  await loginAdmin(page.request, origin);
  const create = await adminWrite(page.request, origin, '/api/v1/admin/products', {
    name: 'Imagini test ' + token,
    slug: 'images-' + token,
    category: 'Verificare automată',
    description: 'Produs demonstrativ pentru verificarea galeriei.',
  });
  expect(create.ok()).toBeTruthy();
  const id = (await create.json()).id;
  const path = '/api/v1/admin/products/' + id;
  const variant = await adminWrite(page.request, origin, path + '/variants', {
    sku,
    label: 'M8',
    diameterMm: 8,
    lengthMm: 40,
    material: 'Oțel',
    finish: 'Zincat',
  });
  expect(variant.ok()).toBeTruthy();
  const variantId = (await variant.json()).id;
  const offer = await adminWrite(
    page.request,
    origin,
    path + '/variants/' + variantId + '/offers',
    {
      code: sku + '-BOX',
      label: 'Cutie',
      unitsPerPack: 100,
      minPacks: 1,
      packStep: 1,
    },
  );
  expect(offer.ok()).toBeTruthy();
  expect(
    (
      await adminWrite(
        page.request,
        origin,
        '/api/v1/admin/offers/' + (await offer.json()).id + '/price',
        { netAmount: 40, taxRate: 0.21 },
        'PUT',
      )
    ).ok(),
  ).toBeTruthy();
  try {
    await page.goto(origin);
    await page.getByLabel('Caută produs sau SKU').fill(sku);
    await page.getByRole('button', { name: 'Aplică filtrele' }).click();
    await expect(page.locator('.product-list button')).toHaveCount(1);
    await page.locator('.product-list button').click();
    const bytes = Buffer.from(
      await page.evaluate(() => {
        const canvas = document.createElement('canvas');
        canvas.width = 900;
        canvas.height = 600;
        const ctx = canvas.getContext('2d')!;
        ctx.fillStyle = '#edf1e8';
        ctx.fillRect(0, 0, 900, 600);
        ctx.fillStyle = '#ba5d16';
        ctx.fillRect(200, 180, 500, 240);
        return canvas.toDataURL('image/png').split(',')[1];
      }),
      'base64',
    );
    const panel = page.locator('sd-product-images-panel');
    const upload = page.locator('sd-image-upload-form');
    await upload
      .getByLabel('Fișier imagine')
      .setInputFiles({ name: 'test.png', mimeType: 'image/png', buffer: bytes });
    await upload
      .getByLabel('Text alternativ pentru imaginea nouă')
      .fill('Vedere laterală demonstrativă');
    await upload.getByRole('button', { name: 'Încarcă imaginea' }).click();
    await expect(panel.getByText('Imaginea a fost încărcată.', { exact: true })).toBeVisible();
    await expect(page.locator('sd-image-editor')).toHaveCount(1);
    let product = (await (await page.request.get(origin + path)).json()).product;
    const firstId = product.images[0].id;
    const publicImage = origin + '/api/v1/products/' + id + '/images/' + firstId + '/full';
    expect((await page.request.get(publicImage)).status()).toBe(404);
    const preview = await page.request.get(origin + path + '/images/' + firstId + '/thumbnail');
    expect(preview.status()).toBe(200);
    expect(preview.headers()['content-type']).toContain('image/webp');
    expect(preview.headers()['cache-control']).toBe('no-store');
    await upload
      .getByLabel('Fișier imagine')
      .setInputFiles({ name: 'other.png', mimeType: 'image/png', buffer: bytes });
    await upload.getByLabel('Text alternativ pentru imaginea nouă').fill('Detaliu demonstrativ M8');
    await upload
      .getByRole('combobox', { name: 'Varianta pentru imaginea nouă' })
      .selectOption(variantId);
    await upload.getByRole('button', { name: 'Încarcă imaginea' }).click();
    await expect(page.locator('sd-image-editor')).toHaveCount(2);
    const second = page.locator('sd-image-editor').nth(1);
    await second.getByRole('button', { name: 'Setează principală' }).click();
    await expect(panel.getByText('Imaginea principală a fost schimbată.')).toBeVisible();
    await second.getByRole('button', { name: 'Mută imaginea înainte' }).click();
    await expect(panel.getByText('Ordinea imaginilor a fost salvată.')).toBeVisible();
    const first = page.locator('sd-image-editor').first();
    await first.getByLabel('Text alternativ', { exact: true }).fill('Detaliu M8 actualizat');
    await first.getByRole('button', { name: 'Salvează detaliile' }).click();
    await expect(panel.getByText('Detaliile imaginii au fost salvate.')).toBeVisible();
    product = (await (await page.request.get(origin + path)).json()).product;
    expect(product.images[0]).toMatchObject({
      isPrimary: true,
      altText: 'Detaliu M8 actualizat',
      variantId,
    });
    expect(product.images[1].id).toBe(firstId);
    const secondId = product.images[0].id;
    const csrf = await (await page.request.get(origin + '/api/auth/csrf')).json();
    const bad = await page.request.post(origin + path + '/images?altText=Invalid', {
      data: Buffer.from('<svg><script>alert(1)</script></svg>'),
      headers: { 'X-CSRF-TOKEN': csrf.token, 'Content-Type': 'image/png' },
    });
    expect(bad.status()).toBe(400);
    expect(
      (
        await adminWrite(
          page.request,
          origin,
          path + '/images/order',
          { imageIds: [firstId, firstId], primaryId: firstId },
          'PUT',
        )
      ).status(),
    ).toBe(400);
    expect(
      (
        await adminWrite(
          page.request,
          origin,
          path + '/images/' + firstId,
          { altText: 'Wrong', variantId: '00000000-0000-0000-0000-000000000001' },
          'PUT',
        )
      ).status(),
    ).toBe(404);
    await panel.screenshot({ path: '../work/images-admin-desktop.png' });
    await page.setViewportSize({ width: 390, height: 844 });
    expect(
      await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
    ).toBeTruthy();
    await panel.screenshot({ path: '../work/images-admin-mobile.png' });
    await page.setViewportSize({ width: 1440, height: 1000 });
    expect((await adminWrite(page.request, origin, path + '/publish')).ok()).toBeTruthy();
    const shop = await page.context().newPage();
    await shop.goto('http://localhost:4200/produse/images-' + token);
    await expect(shop.locator('.main-image img')).toHaveAttribute('alt', 'Detaliu M8 actualizat');
    await expect(shop.locator('.thumbnails button')).toHaveCount(2);
    await expect(shop.locator('.main-image img')).toHaveJSProperty('naturalWidth', 900);
    await shop.getByRole('button', { name: 'Vezi imaginea 2:', exact: false }).click();
    await expect(shop.locator('.main-image img')).toHaveAttribute(
      'alt',
      'Vedere laterală demonstrativă',
    );
    await shop.screenshot({ path: '../work/images-storefront-desktop.png', fullPage: true });
    await shop.setViewportSize({ width: 390, height: 844 });
    expect(
      await shop.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
    ).toBeTruthy();
    await shop.screenshot({ path: '../work/images-storefront-mobile.png', fullPage: true });
    await shop.goto('http://localhost:4200/?q=' + sku);
    await expect(shop.locator('.product-card .photo')).toHaveAttribute(
      'alt',
      'Detaliu M8 actualizat',
    );
    await first.getByRole('button', { name: 'Elimină imaginea' }).click();
    await first.getByRole('button', { name: 'Renunță', exact: true }).click();
    await expect(page.locator('sd-image-editor')).toHaveCount(2);
    await first.getByRole('button', { name: 'Elimină imaginea' }).click();
    await first.getByRole('button', { name: 'Confirmă eliminarea' }).click();
    await expect(page.locator('sd-image-editor')).toHaveCount(1);
    expect(
      (
        await page.request.get(origin + '/api/v1/products/' + id + '/images/' + secondId + '/full')
      ).status(),
    ).toBe(404);
    expect((await adminWrite(page.request, origin, path + '/archive')).ok()).toBeTruthy();
    expect((await page.request.get(publicImage)).status()).toBe(404);
    expect(
      (await adminWrite(page.request, origin, path + '/images/' + firstId, {}, 'DELETE')).status(),
    ).toBe(409);
    expect((await page.request.get(origin + path + '/images/' + firstId + '/full')).status()).toBe(
      200,
    );
    expect(errors).toEqual([]);
    await shop.close();
  } finally {
    await adminWrite(page.request, origin, path + '/archive');
    await adminWrite(page.request, origin, path + '/restore');
    const remaining = (await (await page.request.get(origin + path)).json()).product.images;
    for (const image of remaining)
      await adminWrite(page.request, origin, path + '/images/' + image.id, {}, 'DELETE');
    await adminWrite(page.request, origin, path + '/archive');
  }
});

test('image endpoints enforce authentication and CSRF through gateway and direct service', async ({
  request,
}) => {
  const id = '00000000-0000-0000-0000-000000000001';
  const path = '/api/v1/admin/products/' + id + '/images';
  for (const status of [401, 400]) {
    if (status === 400) await loginAdmin(request, 'http://127.0.0.1:5099');
    for (const port of [5099, 5101]) {
      const origin = 'http://127.0.0.1:' + port;
      expect(
        (
          await request.post(origin + path + '?altText=Test', { data: Buffer.from('image') })
        ).status(),
      ).toBe(status);
      expect(
        (
          await request.put(origin + path + '/' + id, {
            data: { altText: 'Test', variantId: null },
          })
        ).status(),
      ).toBe(status);
      expect(
        (
          await request.put(origin + path + '/order', { data: { imageIds: [id], primaryId: id } })
        ).status(),
      ).toBe(status);
      expect((await request.delete(origin + path + '/' + id)).status()).toBe(status);
      if (status === 401)
        expect((await request.get(origin + path + '/' + id + '/full')).status()).toBe(401);
    }
  }
});
