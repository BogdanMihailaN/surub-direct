import { Supplier } from './supplier.model';
export interface SupplierPage {
  items: Supplier[];
  total: number;
  page: number;
  pageSize: number;
}
