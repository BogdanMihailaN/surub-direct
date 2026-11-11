namespace SurubDirect.Ordering.Domain;
public sealed record ReceiptRetry(Guid OperationId, long Version, string Reason, string Actor, DateTimeOffset CreatedAt);
