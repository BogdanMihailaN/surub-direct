export interface PurchaseReceipt {
  id: string;
  lineId: string;
  variantId: string;
  quantity: number;
  reference: string;
  actor: string;
  createdAt: string;
  appliedAt: string | null;
}
