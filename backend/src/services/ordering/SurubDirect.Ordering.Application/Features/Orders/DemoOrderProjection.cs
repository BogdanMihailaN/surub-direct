using SurubDirect.Ordering.Contracts;
using SurubDirect.Ordering.Domain;

namespace SurubDirect.Ordering.Application;
public static class DemoOrderProjection
{
    public static DemoOrderDto Map(DemoOrder order) => new(order.Id, $"DEMO-{order.Sequence:D8}", order.Status, order.CreatedAt, order.FinalizedAt, order.Currency, order.Lines.Sum(x => x.NetTotal), order.Lines.Sum(x => x.GrossTotal), CustomerDraftProjection.Map(order.Customer)!, order.Lines.Select(x => new PreparedCartLineDto(x.OfferId, x.ProductName, x.Sku, x.OfferLabel, x.Packs, x.UnitGross, x.NetTotal, x.GrossTotal)).ToArray());
}
