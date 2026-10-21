using SurubDirect.Ordering.Domain;

namespace SurubDirect.Ordering.Application;
public interface IReservationPlans
{
    Task<ReservationPlan> Save(ReservationPlan plan, CancellationToken ct);
    Task<ReservationPlan?> Read(Guid id, CancellationToken ct);
    Task<ReservationPlan> Cancel(Guid id, Guid order, CancellationToken ct);
    Task<int> Process(Func<ReservationPlan, CancellationToken, Task> reconcile, CancellationToken ct);
}
