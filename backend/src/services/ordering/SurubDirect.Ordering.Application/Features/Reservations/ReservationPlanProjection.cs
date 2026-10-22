using SurubDirect.Ordering.Domain;
using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Application;
public static class ReservationPlanProjection
{
    public static ReservationPlanDto Map(ReservationPlan p) => new(p.Id, p.OrderId, p.Status, p.CancellationRequested, p.Attempts, p.LastError, p.Deadline, p.RemoteExpiresAt);
}
