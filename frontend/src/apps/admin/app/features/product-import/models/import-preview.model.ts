import { ImportRow } from './import-row.model';
export interface ImportPreview {
  rows: ImportRow[];
  errors: { line: number; message: string }[];
  productCount: number;
  variantCount: number;
  offerCount: number;
  canImport: boolean;
}
