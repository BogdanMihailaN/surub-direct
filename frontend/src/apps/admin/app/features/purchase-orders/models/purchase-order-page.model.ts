import { PurchaseOrder } from './purchase-order.model';
export interface PurchaseOrderPage {
  items: PurchaseOrder[];
  total: number;
  page: number;
  pageSize: number;
}
