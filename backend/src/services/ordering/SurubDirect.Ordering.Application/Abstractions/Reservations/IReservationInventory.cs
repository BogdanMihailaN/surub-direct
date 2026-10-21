using SurubDirect.Ordering.Domain;

namespace SurubDirect.Ordering.Application;
public interface IReservationInventory
{
    Task<ReservationObservation> Reserve(ReservationPlan plan, CancellationToken ct);
    Task<ReservationObservation> Release(ReservationPlan plan, CancellationToken ct);
}
