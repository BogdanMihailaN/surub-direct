import { AccountSession } from './account-session';
import { CartSelection } from '../../cart/models/cart-selection.model';
import { StoredCart } from '../../cart/models/stored-cart.model';
export interface AccountOfferSelection {
  session: AccountSession;
  cart: StoredCart | null;
  line: CartSelection;
}
