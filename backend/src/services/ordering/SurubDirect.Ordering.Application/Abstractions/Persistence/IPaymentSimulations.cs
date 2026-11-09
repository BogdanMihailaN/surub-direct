namespace SurubDirect.Ordering.Application;
public interface IPaymentSimulations
{
    Task RefundReturn(Guid orderId, Guid requestId, Guid operationId, long expectedVersion, string reason, string actor, CancellationToken ct);
    Task Apply(Guid id, Guid operationId, long expectedVersion, string action, string reason, string actor, CancellationToken ct);
}
