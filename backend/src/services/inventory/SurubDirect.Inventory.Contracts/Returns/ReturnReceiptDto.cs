namespace SurubDirect.Inventory.Contracts;
public sealed record ReturnReceiptDto(Guid Id, Guid ReservationId, Guid OrderId, DateTimeOffset CreatedAt, IReadOnlyList<ReturnReceiptLineDto> Lines);
