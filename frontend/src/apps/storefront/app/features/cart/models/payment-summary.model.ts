export interface PaymentSummary {
  status: 'NotStarted' | 'Pending' | 'Paid' | 'Failed' | 'Refunded';
  amount: number;
  currency: string;
}
