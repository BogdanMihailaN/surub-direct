using SurubDirect.Ordering.Domain;

namespace SurubDirect.Ordering.Application;
public sealed record PlanReservation(Guid Id, Guid OrderId, IReadOnlyList<ReservationItem>? Lines);
