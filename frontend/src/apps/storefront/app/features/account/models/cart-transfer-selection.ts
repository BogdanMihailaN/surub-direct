import { StoredCart } from '../../cart/models/stored-cart.model';
export interface CartTransferSelection {
  source: StoredCart;
  target: StoredCart;
}
