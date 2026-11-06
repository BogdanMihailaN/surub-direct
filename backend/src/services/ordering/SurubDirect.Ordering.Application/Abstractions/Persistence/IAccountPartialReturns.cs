using SurubDirect.Ordering.Contracts;
using SurubDirect.Ordering.Domain;
namespace SurubDirect.Ordering.Application;
public interface IAccountPartialReturns
{
    Task<PartialReturnRequest> Request(string accountId, Guid orderId, Guid requestId, IReadOnlyList<PartialReturnSelection>? lines, string? reason, CancellationToken ct);
    Task<AccountPartialReturnsDto> Read(string accountId, Guid orderId, CancellationToken ct);
}
