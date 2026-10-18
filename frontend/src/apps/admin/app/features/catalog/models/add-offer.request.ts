export interface AddOfferRequest {
  variantId: string;
  code: string;
  label: string;
  unitsPerPack: number;
  minPacks: number;
  packStep: number;
}
