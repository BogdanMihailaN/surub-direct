import { CustomerDraft } from './customer-draft.model';
import { CartSelection } from './cart-selection.model';
export interface StoredCart {
  id: string;
  version: number;
  updatedAt: string;
  expiresAt: string;
  customer: CustomerDraft | null;
  lines: CartSelection[];
}
