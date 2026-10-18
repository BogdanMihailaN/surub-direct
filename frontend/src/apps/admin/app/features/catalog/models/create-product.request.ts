export interface CreateProductRequest {
  name: string;
  slug: string;
  category: string;
  categoryId?: string;
  description: string;
}
