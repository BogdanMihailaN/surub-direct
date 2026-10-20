namespace SurubDirect.Ordering.Infrastructure;

internal sealed record CatalogVariant(Guid Id, string Sku, IReadOnlyList<CatalogOffer> Offers);
