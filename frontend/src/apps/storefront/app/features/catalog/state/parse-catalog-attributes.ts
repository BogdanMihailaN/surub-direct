import { AttributeFilter } from '@shared/models';

const attributeId = /^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i;

export function parseCatalogAttributes(raw: string | null): AttributeFilter[] | null {
  if (raw === null || raw === '') return [];
  try {
    const values: unknown = JSON.parse(raw);
    if (!Array.isArray(values) || !values.every(isAttributeFilter)) return null;
    return values;
  } catch {
    return null;
  }
}

function isAttributeFilter(value: unknown): value is AttributeFilter {
  if (value === null || typeof value !== 'object') return false;
  const filter = value as Partial<AttributeFilter>;
  return (
    typeof filter.attributeId === 'string' &&
    attributeId.test(filter.attributeId) &&
    (filter.text == null || typeof filter.text === 'string') &&
    (filter.minimum == null || Number.isFinite(filter.minimum)) &&
    (filter.maximum == null || Number.isFinite(filter.maximum))
  );
}
