using SurubDirect.Ordering.Domain;

namespace SurubDirect.Ordering.Application;
public sealed class ProcessReservationPlanHandler(IReservationPlans plans, IReservationInventory inventory)
{
    public Task<int> Handle(CancellationToken ct) => plans.Process(Reconcile, ct);
    private async Task Reconcile(ReservationPlan plan, CancellationToken ct)
    {
        if (plan.Deadline <= DateTimeOffset.UtcNow)
            plan.Cancel(DateTimeOffset.UtcNow);
        try
        {
            var result = plan.CancellationRequested ? await inventory.Release(plan, ct) : await inventory.Reserve(plan, ct);
            plan.Observe(result.Status, result.ExpiresAt, DateTimeOffset.UtcNow);
        }
        catch (ReservationRejectedException)
        {
            plan.Cancel(DateTimeOffset.UtcNow);
        }
        catch (Exception)when (!ct.IsCancellationRequested)
        {
            plan.Failed(DateTimeOffset.UtcNow);
        }
    }
}
