namespace SurubDirect.Ordering.Infrastructure;
internal sealed record InventoryReservationResponse(Guid Id, Guid OrderId, string Status, DateTimeOffset ExpiresAt, InventoryReservationLine[]? Lines);
