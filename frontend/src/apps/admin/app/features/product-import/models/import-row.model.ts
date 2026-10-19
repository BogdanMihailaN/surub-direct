export interface ImportRow {
  line: number;
  productName: string;
  slug: string;
  categorySlug: string;
  description: string;
  sku: string;
  variantLabel: string;
  diameterMm: number | null;
  lengthMm: number | null;
  material: string;
  finish: string;
  offerCode: string;
  offerLabel: string;
  unitsPerPack: number;
  minPacks: number;
  packStep: number;
  netAmount: number;
  taxRate: number;
}
