namespace SurubDirect.Ordering.Contracts;
public sealed record PendingReceiptDto(Guid RequestId, DateTimeOffset? RequestedAt, DateTimeOffset? NextAttemptAt, int Attempts);
