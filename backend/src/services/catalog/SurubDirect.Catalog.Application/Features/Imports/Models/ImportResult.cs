namespace SurubDirect.Catalog.Application.Imports;
public sealed record ImportResult(IReadOnlyList<Guid> ProductIds, int VariantCount, int OfferCount);
