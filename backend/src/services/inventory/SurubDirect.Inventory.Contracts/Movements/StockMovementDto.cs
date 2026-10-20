namespace SurubDirect.Inventory.Contracts;
public sealed record StockMovementDto(Guid Id, string Kind, int Quantity, string Reference, string Reason, string Actor, DateTimeOffset CreatedAt);
