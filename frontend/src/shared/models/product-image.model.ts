export interface ProductImage {
  id: string;
  altText: string;
  variantId: string | null;
  width: number;
  height: number;
  sortOrder: number;
  isPrimary: boolean;
}
