using SurubDirect.Ordering.Domain;
namespace SurubDirect.Ordering.Application;
public interface IPartialReturns
{
    Task<PartialReturnRequest> Request(Guid orderId, Guid requestId, IReadOnlyList<PartialReturnSelection>? lines, string? reason, string actor, CancellationToken ct);
    Task<IReadOnlyList<PartialReturnRequest>> Read(Guid orderId, CancellationToken ct);
    Task Decide(Guid orderId, Guid requestId, Guid operationId, long expectedVersion, string? target, string? reason, string actor, CancellationToken ct);
}
