import { StockMovement } from './stock-movement.model';
export interface StockLedger {
  onHand: number;
  reserved: number;
  items: StockMovement[];
  total: number;
  page: number;
  pageSize: number;
}
