export interface AttributeWriteRequest {
  code: string;
  name: string;
  kind: 'Text' | 'Number';
  unit: string | null;
  sortOrder: number;
}
