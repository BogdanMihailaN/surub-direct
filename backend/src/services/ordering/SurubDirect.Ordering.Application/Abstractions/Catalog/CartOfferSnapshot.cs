namespace SurubDirect.Ordering.Application;

public sealed record CartOfferSnapshot(Guid OfferId, Guid VariantId, string ProductName, string Sku, string Label, int UnitsPerPack, int MinPacks, int PackStep, long Available, decimal? Net, decimal? Gross, string? Currency);
