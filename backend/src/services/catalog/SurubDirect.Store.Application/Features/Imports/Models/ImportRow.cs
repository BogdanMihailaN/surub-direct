namespace SurubDirect.Store.Application.Imports;
public sealed record ImportRow(int Line, string ProductName, string Slug, string CategorySlug, string Description,
    string Sku, string VariantLabel, decimal? DiameterMm, decimal? LengthMm, string Material, string Finish,
    string OfferCode, string OfferLabel, int UnitsPerPack, int MinPacks, int PackStep, decimal NetAmount, decimal TaxRate);
