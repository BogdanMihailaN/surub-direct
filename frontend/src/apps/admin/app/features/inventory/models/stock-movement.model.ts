export interface StockMovement {
  id: string;
  kind: 'Receipt' | 'Adjustment';
  quantity: number;
  reference: string;
  reason: string;
  actor: string;
  createdAt: string;
}
