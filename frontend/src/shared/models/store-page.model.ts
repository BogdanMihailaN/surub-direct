import { StoreProduct } from './store-product.model';
export interface StorePage {
  items: StoreProduct[];
  total: number;
  page: number;
  pageSize: number;
}
