export interface PurchaseReceiptCorrection {
  id: string;
  receiptId: string;
  lineId: string;
  quantity: number;
  reference: string;
  reason: string;
  actor: string;
  createdAt: string;
  appliedAt: string | null;
}
