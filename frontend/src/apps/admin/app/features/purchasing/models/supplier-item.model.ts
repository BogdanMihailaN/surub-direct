import { SupplierItemDetails } from './supplier-item-details.model';
export interface SupplierItem extends SupplierItemDetails {
  id: string;
  variantId: string;
  sku: string;
  variantLabel: string;
  updatedAt: string;
}
