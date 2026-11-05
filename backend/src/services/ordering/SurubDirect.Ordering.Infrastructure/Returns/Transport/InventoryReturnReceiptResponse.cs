namespace SurubDirect.Ordering.Infrastructure;
public sealed record InventoryReturnReceiptResponse(Guid Id, Guid ReservationId, Guid OrderId, DateTimeOffset CreatedAt, InventoryReturnLine[]? Lines);
