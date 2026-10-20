export interface PurchaseOrderLine {
  id: string;
  supplierItemId: string;
  variantId: string;
  sku: string;
  label: string;
  supplierCode: string;
  packLabel: string;
  unitsPerPack: number;
  packs: number;
  baseUnits: number;
  netCostPerPack: number;
  netTotal: number;
}
