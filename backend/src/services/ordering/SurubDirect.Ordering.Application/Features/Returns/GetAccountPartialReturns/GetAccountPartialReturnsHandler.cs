using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Contracts;
namespace SurubDirect.Ordering.Application;
public sealed class GetAccountPartialReturnsHandler(IAccountPartialReturns returns)
{
    public Task<AccountPartialReturnsDto> Handle(string accountId, Guid orderId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(accountId) || accountId.Length > 180) throw new BusinessException("Contul nu este valid.");
        return returns.Read(accountId, orderId, ct);
    }
}
