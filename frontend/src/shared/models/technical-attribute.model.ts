export interface TechnicalAttribute {
  id: string;
  code: string;
  name: string;
  kind: 'Text' | 'Number';
  source: 'Custom' | 'Diameter' | 'Length' | 'Material' | 'Finish';
  unit: string | null;
  sortOrder: number;
}
