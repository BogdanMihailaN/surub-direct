using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Application;
public sealed class ReplaceCartHandler(IShoppingCartRepository repository)
{
    public async Task<StoredCartDto> Handle(string ownerHash, ReplaceCart c, CancellationToken ct) => StoredCartProjection.Map(await repository.Replace(ownerHash, c.CartId, c.Version, c.Lines, ct));
}
