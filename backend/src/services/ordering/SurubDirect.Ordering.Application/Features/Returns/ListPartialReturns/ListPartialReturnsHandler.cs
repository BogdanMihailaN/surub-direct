using SurubDirect.Ordering.Contracts;
namespace SurubDirect.Ordering.Application;
public sealed class ListPartialReturnsHandler(IPartialReturns returns)
{
    public async Task<IReadOnlyList<PartialReturnDto>> Handle(Guid orderId, CancellationToken ct)
        => (await returns.Read(orderId, ct)).Select(PartialReturnProjection.Map).ToArray();
}
