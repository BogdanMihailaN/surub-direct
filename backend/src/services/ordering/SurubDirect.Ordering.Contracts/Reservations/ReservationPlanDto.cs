namespace SurubDirect.Ordering.Contracts;
public sealed record ReservationPlanDto(Guid Id, Guid OrderId, string Status, bool CancellationRequested, int Attempts, string? LastError, DateTimeOffset Deadline, DateTimeOffset? RemoteExpiresAt);
