import { OrderSummary } from './order-summary.model';
import { OrderCustomer } from './order-customer.model';
import { OrderLine } from './order-line.model';
export interface OrderDetail {
  order: Omit<OrderSummary, 'customerName'> & {
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
