namespace SurubDirect.Inventory.Contracts;
public sealed record ReservationDto(Guid Id, Guid OrderId, string Status, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, DateTimeOffset? EndedAt, IReadOnlyList<ReservationLineDto> Lines);
