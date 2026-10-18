export type CatalogEditor =
  | { kind: 'product' }
  | { kind: 'variant'; variantId: string }
  | { kind: 'attributes'; variantId: string }
  | { kind: 'offer'; variantId: string; offerId: string };
