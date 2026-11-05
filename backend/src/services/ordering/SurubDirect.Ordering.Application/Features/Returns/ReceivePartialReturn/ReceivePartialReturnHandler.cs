namespace SurubDirect.Ordering.Application;
public sealed class ReceivePartialReturnHandler(IPartialReturnReceipts receipts)
{
    public Task Handle(Guid orderId, Guid requestId, Guid operationId, long expectedVersion, string? reason, string actor, bool resalableConfirmed, CancellationToken ct)
        => receipts.Receive(orderId, requestId, operationId, expectedVersion, reason, actor, resalableConfirmed, ct);
}
