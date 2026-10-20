namespace SurubDirect.Ordering.Infrastructure;

internal sealed record CatalogResponse(CatalogProduct Product, IReadOnlyList<CatalogPrice> Prices, IReadOnlyList<CatalogStock> Stock);
