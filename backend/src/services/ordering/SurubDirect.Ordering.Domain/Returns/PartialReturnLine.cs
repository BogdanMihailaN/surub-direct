namespace SurubDirect.Ordering.Domain;
public sealed record PartialReturnLine(Guid OfferId, Guid VariantId, string ProductName, string Sku, string OfferLabel, int Packs, int UnitsPerPack, int Quantity);
