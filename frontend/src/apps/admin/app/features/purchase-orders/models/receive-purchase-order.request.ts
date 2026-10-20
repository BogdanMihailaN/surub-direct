export interface ReceivePurchaseOrderRequest {
  requestId: string;
  lineId: string;
  quantity: number;
  reference: string;
}
