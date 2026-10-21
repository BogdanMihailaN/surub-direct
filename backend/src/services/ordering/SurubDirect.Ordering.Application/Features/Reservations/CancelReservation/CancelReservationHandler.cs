using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Application;
public sealed class CancelReservationHandler(IReservationPlans plans)
{
    public async Task<ReservationPlanDto> Handle(Guid id, Guid orderId, CancellationToken ct) => ReservationPlanProjection.Map(await plans.Cancel(id, orderId, ct));
}
