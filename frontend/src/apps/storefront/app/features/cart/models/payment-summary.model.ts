export interface PaymentSummary {
  status: 'NotStarted' | 'Pending' | 'Paid' | 'Failed' | 'Refunded' | 'PartiallyRefunded';
  amount: number;
  refundedAmount: number;
  currency: string;
}
