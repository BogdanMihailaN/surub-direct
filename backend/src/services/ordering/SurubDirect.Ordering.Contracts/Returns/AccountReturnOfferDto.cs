namespace SurubDirect.Ordering.Contracts;
public sealed record AccountReturnOfferDto(Guid OfferId, string ProductName, string Sku, string OfferLabel, int PurchasedPacks, int AvailablePacks, int UnitsPerPack);
