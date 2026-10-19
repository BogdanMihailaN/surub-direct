import { SupplierDetails } from './supplier-details.model';
import { SupplierItem } from './supplier-item.model';
export interface Supplier extends SupplierDetails {
  id: string;
  items: SupplierItem[];
}
