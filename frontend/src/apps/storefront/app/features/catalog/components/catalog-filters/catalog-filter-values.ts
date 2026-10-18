import { AttributeFilter } from '@shared/models';
export interface CatalogFilterValues {
  categoryId: string;
  diameter: string;
  length: string;
  attributes: AttributeFilter[];
}
