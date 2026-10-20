export interface StockMovement {
  id: string;
  originalReceiptId: string | null;
  kind: 'Receipt' | 'Adjustment' | 'Correction';
  quantity: number;
  reference: string;
  reason: string;
  actor: string;
  createdAt: string;
}
