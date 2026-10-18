export interface TechnicalValue {
  attributeId: string;
  source: string;
  code: string;
  name: string;
  kind: 'Text' | 'Number';
  unit: string | null;
  textValue: string | null;
  numberValue: number | null;
}
