export interface OrderLine {
  offerId: string;
  productName: string;
  sku: string;
  offerLabel: string;
  packs: number;
  unitGross: number;
  netTotal: number;
  grossTotal: number;
}
