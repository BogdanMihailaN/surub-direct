using SurubDirect.Ordering.Contracts;
using SurubDirect.Ordering.Domain;
namespace SurubDirect.Ordering.Application;
public sealed class RequestPartialReturnHandler(IPartialReturns returns)
{
    public async Task<PartialReturnDto> Handle(Guid orderId, Guid requestId, IReadOnlyList<PartialReturnSelectionDto>? lines, string? reason, string actor, CancellationToken ct)
        => PartialReturnProjection.Map(await returns.Request(orderId, requestId, lines?.Select(x => x is null ? null! : new PartialReturnSelection(x.OfferId, x.Packs)).ToArray(), reason, actor, ct));
}
