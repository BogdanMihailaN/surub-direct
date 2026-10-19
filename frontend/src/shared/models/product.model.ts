import { ProductImage } from './product-image.model';
import { Variant } from './variant.model';
export interface Product {
  id: string;
  name: string;
  slug: string;
  category: string;
  categoryId: string;
  description: string;
  status: string;
  variants: Variant[];
  images: ProductImage[];
}
