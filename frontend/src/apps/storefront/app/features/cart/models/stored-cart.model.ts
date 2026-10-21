import { CustomerDraft } from './customer-draft.model';
import { CartSelection } from './cart-selection.model';
export interface StoredCart {
  id: string;
  version: number;
  updatedAt: string;
  expiresAt: string;
  preparedId: string | null;
  customer: CustomerDraft | null;
  lines: CartSelection[];
}
