import { DeliveryCharge } from '@shared/delivery/delivery-charge.model';
import { OrderProcessing } from './order-processing.model';
import { OrderSummary } from './order-summary.model';
import { OrderCustomer } from './order-customer.model';
import { OrderLine } from './order-line.model';
export interface OrderDetail {
  processing: OrderProcessing;
  cancellationReason: string | null;
  cancelledBy: string | null;
  cancellationRequestedAt: string | null;
  cancelledAt: string | null;
  order: Omit<OrderSummary, 'customerName'> & {
    delivery?: DeliveryCharge | null;
    finalizedAt: string | null;
    netTotal: number;
    customer: OrderCustomer;
    lines: OrderLine[];
  };
  reservation: {
    id: string;
    orderId: string;
    status: string;
    cancellationRequested: boolean;
    attempts: number;
    lastError: string | null;
    deadline: string;
    remoteExpiresAt: string | null;
  };
}
