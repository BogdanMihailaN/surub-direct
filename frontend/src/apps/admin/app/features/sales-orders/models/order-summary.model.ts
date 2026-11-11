export interface OrderSummary {
  id: string;
  number: string;
  status: 'Confirming' | 'Confirmed' | 'Failed' | 'Cancelling' | 'Cancelled';
  paymentStatus: string;
  pendingReceipts: { requestId: string; requestedAt: string; nextAttemptAt: string; attempts: number; needsAttention: boolean }[];
  returnStatus: string;
  customerName: string;
  currency: string;
  grossTotal: number;
  createdAt: string;
}
