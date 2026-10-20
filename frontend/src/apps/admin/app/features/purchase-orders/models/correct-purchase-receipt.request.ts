export interface CorrectPurchaseReceiptRequest {
  requestId: string;
  receiptId: string;
  quantity: number;
  reference: string;
  reason: string;
}
