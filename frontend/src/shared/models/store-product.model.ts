import { Product } from './product.model';
import { Price } from './price.model';
import { Stock } from './stock.model';
export interface StoreProduct {
  product: Product;
  prices: Price[];
  stock: Stock[];
}
