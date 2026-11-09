using SurubDirect.Ordering.Contracts;
using SurubDirect.Ordering.Domain;

namespace SurubDirect.Ordering.Application;
public static class DemoOrderProjection
{
    public static DemoOrderDto Map(DemoOrder order, PaymentSimulation? payment) => new(order.Id, $"DEMO-{order.Sequence:D8}", order.Status, order.CreatedAt, order.FinalizedAt, order.Currency, order.Lines.Sum(x => x.NetTotal) + (order.Delivery?.NetAmount ?? 0), order.Lines.Sum(x => x.GrossTotal) + (order.Delivery?.GrossAmount ?? 0), CustomerDraftProjection.Map(order.Customer)!, order.Lines.Select(x => new PreparedCartLineDto(x.OfferId, x.ProductName, x.Sku, x.OfferLabel, x.Packs, x.UnitGross, x.NetTotal, x.GrossTotal)).ToArray(), new PaymentSummaryDto(payment?.Status ?? "NotStarted", payment?.Amount ?? order.Lines.Sum(x => x.GrossTotal) + (order.Delivery?.GrossAmount ?? 0), payment?.Currency ?? order.Currency, payment?.RefundedAmount ?? 0), new ShipmentSummaryDto(order.ShipmentStatus, order.ShipmentReference), new ReturnSummaryDto(order.ReturnStatus, order.ReturnRestoredAt), DeliveryChargeProjection.Map(order.Delivery));
}
