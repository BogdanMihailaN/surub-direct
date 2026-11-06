namespace SurubDirect.Ordering.Contracts;
public sealed record AccountPartialReturnLineDto(Guid OfferId, string ProductName, string Sku, string OfferLabel, int Packs, int UnitsPerPack, int Quantity);
