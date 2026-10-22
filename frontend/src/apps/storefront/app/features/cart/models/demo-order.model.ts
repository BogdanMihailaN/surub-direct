import { PreparedCart } from './prepared-cart.model';
export interface DemoOrder {
  id: string;
  number: string;
  status: 'Confirming' | 'Confirmed' | 'Failed';
  createdAt: string;
  finalizedAt: string | null;
  currency: string;
  netTotal: number;
  grossTotal: number;
  customer: PreparedCart['customer'];
  lines: PreparedCart['lines'];
}
