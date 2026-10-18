export interface CategoryWriteRequest {
  name: string;
  slug: string;
  parentId: string | null;
  sortOrder: number;
  attributeIds: string[];
}
