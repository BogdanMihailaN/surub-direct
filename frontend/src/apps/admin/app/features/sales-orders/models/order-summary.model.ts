export interface OrderSummary {
  id: string;
  number: string;
  status: 'Confirming' | 'Confirmed' | 'Failed' | 'Cancelling' | 'Cancelled';
  returnStatus: string;
  customerName: string;
  currency: string;
  grossTotal: number;
  createdAt: string;
}
