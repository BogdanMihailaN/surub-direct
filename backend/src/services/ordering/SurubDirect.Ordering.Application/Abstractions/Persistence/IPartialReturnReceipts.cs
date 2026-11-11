using SurubDirect.Ordering.Domain;
namespace SurubDirect.Ordering.Application;
public interface IPartialReturnReceipts
{
    Task Retry(Guid orderId, Guid requestId, Guid operationId, long expectedVersion, string? reason, string actor, CancellationToken ct);
    Task Receive(Guid orderId, Guid requestId, Guid operationId, long expectedVersion, string? reason, string actor, bool resalableConfirmed, CancellationToken ct);
    Task<int> Process(Func<PartialReturnRequest, CancellationToken, Task> reconcile, CancellationToken ct);
}
