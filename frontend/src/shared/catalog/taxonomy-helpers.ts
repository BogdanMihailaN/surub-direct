import { Category, Taxonomy } from '../models';

export function categoryOptions(
  categories: Category[],
): { category: Category; path: string; depth: number }[] {
  const result: { category: Category; path: string; depth: number }[] = [];
  const visit = (parentId: string | null, path: string, depth: number) => {
    for (const category of categories
      .filter((c) => c.parentId === parentId)
      .sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name, 'ro'))) {
      const fullPath = path ? path + ' / ' + category.name : category.name;
      result.push({ category, path: fullPath, depth });
      if (depth < 5) visit(category.id, fullPath, depth + 1);
    }
  };
  visit(null, '', 0);
  return result;
}
export function effectiveAttributes(taxonomy: Taxonomy, categoryId: string) {
  const ids = new Set<string>();
  const visited = new Set<string>();
  let category = taxonomy.categories.find((c) => c.id === categoryId);
  while (category && !visited.has(category.id)) {
    visited.add(category.id);
    category.attributeIds.forEach((id) => ids.add(id));
    category = taxonomy.categories.find((c) => c.id === category?.parentId);
  }
  return taxonomy.attributes
    .filter((a) => ids.has(a.id))
    .sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name, 'ro'));
}
