export interface AdjustStockRequest {
  requestId: string;
  quantity: number;
  expectedOnHand: number;
  expectedReserved: number;
  reference: string;
  reason: string;
}
