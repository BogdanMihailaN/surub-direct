export interface AddVariantRequest {
  sku: string;
  label: string;
  diameterMm: number | null;
  lengthMm: number | null;
  material: string;
  finish: string;
}
