namespace SurubDirect.Ordering.Infrastructure;

internal sealed record CatalogProduct(string Name, string Slug, IReadOnlyList<CatalogVariant> Variants);
