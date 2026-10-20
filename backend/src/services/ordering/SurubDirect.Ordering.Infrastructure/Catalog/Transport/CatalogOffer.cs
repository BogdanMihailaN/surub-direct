namespace SurubDirect.Ordering.Infrastructure;

internal sealed record CatalogOffer(Guid Id, string Label, int UnitsPerPack, int MinPacks, int PackStep);
