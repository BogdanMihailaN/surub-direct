import { CartSelection } from '../models/cart-selection.model';
export const CART_STORAGE_KEY = 'surub-direct.cart.v1';
export function parseCart(value: string | null): CartSelection[] {
  if (!value) return [];
  const data: unknown = JSON.parse(value);
  if (!Array.isArray(data) || data.length > 50) throw new Error('Invalid cart');
  const items: CartSelection[] = data.map((item: unknown) => {
    if (typeof item !== 'object' || !item) throw new Error('Invalid line');
    const x = item as Record<string, unknown>;
    if (
      typeof x['productSlug'] !== 'string' ||
      !/^[-a-zA-Z0-9]{1,200}$/.test(x['productSlug']) ||
      typeof x['offerId'] !== 'string' ||
      !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(x['offerId']) ||
      typeof x['packs'] !== 'number' ||
      !Number.isInteger(x['packs']) ||
      x['packs'] < 1 ||
      x['packs'] > 2147483647
    )
      throw new Error('Invalid line');
    return { productSlug: x['productSlug'], offerId: x['offerId'], packs: x['packs'] };
  });
  if (new Set(items.map((x) => x.offerId)).size !== items.length)
    throw new Error('Duplicate lines');
  return items;
}
