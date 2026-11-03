namespace SurubDirect.Ordering.Application;
public interface IShipments
{
    Task Change(Guid id, Guid operationId, long expectedVersion, string target, string reason, string actor, CancellationToken ct);
}
