using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Contracts;
using SurubDirect.Ordering.Domain;
namespace SurubDirect.Ordering.Application;
public sealed class RequestAccountPartialReturnHandler(IAccountPartialReturns returns)
{
    public async Task<AccountPartialReturnDto> Handle(string accountId, Guid orderId, Guid requestId, IReadOnlyList<PartialReturnSelectionDto>? lines, string? reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(accountId) || accountId.Length > 180) throw new BusinessException("Contul nu este valid.");
        return AccountPartialReturnProjection.Map(await returns.Request(accountId, orderId, requestId,
            lines?.Select(x => x is null ? null! : new PartialReturnSelection(x.OfferId, x.Packs)).ToArray(), reason, ct));
    }
}
