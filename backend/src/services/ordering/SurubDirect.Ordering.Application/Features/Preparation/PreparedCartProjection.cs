using SurubDirect.Ordering.Domain;
using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Application;
public static class PreparedCartProjection
{
    public static PreparedCartDto Map(PreparedCart p, ReservationPlan plan, DemoOrder? order = null) => new(p.Id, plan.Status, plan.CancellationRequested, p.CheckedAt, plan.Deadline, plan.RemoteExpiresAt, p.Currency, p.Lines.Sum(x => x.NetTotal), p.Lines.Sum(x => x.GrossTotal), CustomerDraftProjection.Map(p.Customer)!, p.Lines.Select(x => new PreparedCartLineDto(x.OfferId, x.ProductName, x.Sku, x.OfferLabel, x.Packs, x.UnitGross, x.NetTotal, x.GrossTotal)).ToArray(), plan.ConfirmationRequested, order is null ? null : $"DEMO-{order.Sequence:D8}");
}
