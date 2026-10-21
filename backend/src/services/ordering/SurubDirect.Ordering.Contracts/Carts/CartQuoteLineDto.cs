namespace SurubDirect.Ordering.Contracts;
public sealed record CartQuoteLineDto(string ProductSlug, Guid OfferId, int Packs, string ProductName, string Sku, string OfferLabel, int UnitsPerPack, int MinPacks, int PackStep, long BaseUnits, decimal? UnitGross, decimal? NetTotal, decimal? GrossTotal, string? Currency, string? Issue, Guid? VariantId = null, decimal? UnitNet = null);
