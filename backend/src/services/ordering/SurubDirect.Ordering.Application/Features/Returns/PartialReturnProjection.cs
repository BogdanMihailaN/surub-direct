using SurubDirect.Ordering.Contracts;
using SurubDirect.Ordering.Domain;
namespace SurubDirect.Ordering.Application;
public static class PartialReturnProjection
{
    public static PartialReturnDto Map(PartialReturnRequest r) => new(r.Id, r.OrderId, r.Status, r.Version, r.Reason, r.Actor, r.CreatedAt,
        r.Lines.Select(x => new PartialReturnLineDto(x.OfferId, x.VariantId, x.ProductName, x.Sku, x.OfferLabel, x.Packs, x.UnitsPerPack, x.Quantity)).ToArray(),
        r.DecisionId, r.DecisionReason, r.DecidedBy, r.DecidedAt);
}
