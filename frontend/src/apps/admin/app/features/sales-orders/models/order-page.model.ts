import { OrderSummary } from './order-summary.model';
export interface OrderPage {
  items: OrderSummary[];
  total: number;
  page: number;
  pageSize: number;
}
