namespace SurubDirect.Ordering.Application;
public sealed class RetryPartialReturnReceiptHandler(IPartialReturnReceipts receipts)
{
    public Task Handle(Guid orderId, Guid requestId, Guid operationId, long expectedVersion, string? reason, string actor, CancellationToken ct)
        => receipts.Retry(orderId, requestId, operationId, expectedVersion, reason, actor, ct);
}
