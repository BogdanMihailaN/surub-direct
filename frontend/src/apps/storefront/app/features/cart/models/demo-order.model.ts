import { PaymentSummary } from './payment-summary.model';
import { DeliveryCharge } from '@shared/delivery/delivery-charge.model';
import { PreparedCart } from './prepared-cart.model';
export interface DemoOrder {
  id: string;
  number: string;
  status: 'Confirming' | 'Confirmed' | 'Failed' | 'Cancelling' | 'Cancelled';
  createdAt: string;
  finalizedAt: string | null;
  delivery?: DeliveryCharge | null;
  paymentSummary?: PaymentSummary;
  return?: { status: string; restoredAt: string | null };
  shipment?: { status: string; reference: string | null };
  currency: string;
  netTotal: number;
  grossTotal: number;
  customer: PreparedCart['customer'];
  lines: PreparedCart['lines'];
}
