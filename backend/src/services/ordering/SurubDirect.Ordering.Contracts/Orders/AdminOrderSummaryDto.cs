namespace SurubDirect.Ordering.Contracts;
public sealed record AdminOrderSummaryDto(Guid Id, string Number, string Status, string CustomerName, string Currency, decimal GrossTotal, DateTimeOffset CreatedAt);
