namespace SurubDirect.Ordering.Application;
public interface IPaymentSimulations
{
    Task Apply(Guid id, Guid operationId, long expectedVersion, string action, string reason, string actor, CancellationToken ct);
}
