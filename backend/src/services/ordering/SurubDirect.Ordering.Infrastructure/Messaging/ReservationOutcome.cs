namespace SurubDirect.Ordering.Infrastructure;
public sealed record ReservationOutcome(Guid EventId, Guid ReservationId, Guid OrderId, string Status, DateTimeOffset ExpiresAt, DateTimeOffset OccurredAt, IReadOnlyList<ReservationOutcomeLine> Lines);
