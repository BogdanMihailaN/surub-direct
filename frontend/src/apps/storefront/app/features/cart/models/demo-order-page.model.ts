import { DemoOrder } from './demo-order.model';
export interface DemoOrderPage {
  items: DemoOrder[];
  page: number;
  total: number;
  pageSize: number;
}
