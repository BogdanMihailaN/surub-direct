using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Application;
public sealed class GetReservationPlanHandler(IReservationPlans plans)
{
    public async Task<ReservationPlanDto> Handle(Guid id, CancellationToken ct) => ReservationPlanProjection.Map(await plans.Read(id, ct) ?? throw new BusinessException("Planul nu există.", 404));
}
