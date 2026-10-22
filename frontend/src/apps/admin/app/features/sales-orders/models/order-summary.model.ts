export interface OrderSummary {
  id: string;
  number: string;
  status: 'Confirming' | 'Confirmed' | 'Failed';
  customerName: string;
  currency: string;
  grossTotal: number;
  createdAt: string;
}
