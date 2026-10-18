import { Category } from './category.model';
import { TechnicalAttribute } from './technical-attribute.model';
export interface Taxonomy {
  categories: Category[];
  attributes: TechnicalAttribute[];
}
