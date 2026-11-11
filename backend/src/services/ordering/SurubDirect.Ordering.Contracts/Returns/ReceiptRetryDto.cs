namespace SurubDirect.Ordering.Contracts;
public sealed record ReceiptRetryDto(Guid OperationId, long Version, string Reason, string Actor, DateTimeOffset CreatedAt);
