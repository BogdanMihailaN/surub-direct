export interface CartQuoteLine {
  productSlug: string;
  offerId: string;
  packs: number;
  productName: string;
  sku: string;
  offerLabel: string;
  unitsPerPack: number;
  minPacks: number;
  packStep: number;
  baseUnits: number;
  unitGross: number | null;
  netTotal: number | null;
  grossTotal: number | null;
  currency: string | null;
  issue: string | null;
}
