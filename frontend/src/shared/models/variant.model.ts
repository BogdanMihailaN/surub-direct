import { TechnicalValue } from './technical-value.model';
import { Offer } from './offer.model';
export interface Variant {
  id: string;
  sku: string;
  label: string;
  diameterMm: number | null;
  lengthMm: number | null;
  material: string;
  finish: string;
  offers: Offer[];
  attributes: TechnicalValue[];
}
