using SurubDirect.Ordering.Domain;
using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Application;
public sealed class PlanReservationHandler(IReservationPlans plans)
{
    public async Task<ReservationPlanDto> Handle(PlanReservation command, CancellationToken ct) => ReservationPlanProjection.Map(await plans.Save(ReservationPlan.Create(command.Id, command.OrderId, command.Lines, DateTimeOffset.UtcNow), ct));
}
