import { CartSelection } from './cart-selection.model';
export interface StoredCart {
  id: string;
  version: number;
  updatedAt: string;
  expiresAt: string;
  lines: CartSelection[];
}
