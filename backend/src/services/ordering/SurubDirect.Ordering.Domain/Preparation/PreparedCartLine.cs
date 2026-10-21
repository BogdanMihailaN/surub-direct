namespace SurubDirect.Ordering.Domain;
public sealed record PreparedCartLine(Guid OfferId, Guid VariantId, string ProductName, string Sku, string OfferLabel, int Packs, int UnitsPerPack, decimal UnitNet, decimal UnitGross, decimal NetTotal, decimal GrossTotal);
