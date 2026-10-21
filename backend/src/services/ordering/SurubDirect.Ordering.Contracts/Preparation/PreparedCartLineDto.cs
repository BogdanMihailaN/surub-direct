namespace SurubDirect.Ordering.Contracts;
public sealed record PreparedCartLineDto(Guid OfferId, string ProductName, string Sku, string OfferLabel, int Packs, decimal UnitGross, decimal NetTotal, decimal GrossTotal);
