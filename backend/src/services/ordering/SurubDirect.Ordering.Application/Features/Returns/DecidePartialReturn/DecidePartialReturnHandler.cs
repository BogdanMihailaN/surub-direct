namespace SurubDirect.Ordering.Application;
public sealed class DecidePartialReturnHandler(IPartialReturns returns)
{
    public Task Handle(Guid orderId, Guid requestId, Guid operationId, long expectedVersion, string? target, string? reason, string actor, CancellationToken ct)
        => returns.Decide(orderId, requestId, operationId, expectedVersion, target, reason, actor, ct);
}
