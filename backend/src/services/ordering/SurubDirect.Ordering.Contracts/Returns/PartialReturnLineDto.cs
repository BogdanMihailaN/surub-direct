namespace SurubDirect.Ordering.Contracts;
public sealed record PartialReturnLineDto(Guid OfferId, Guid VariantId, string ProductName, string Sku, string OfferLabel, int Packs, int UnitsPerPack, int Quantity);
