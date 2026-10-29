using SurubDirect.Ordering.Contracts;
namespace SurubDirect.Ordering.Application;
public sealed class ReplaceAccountCartHandler(IAccountCarts carts)
{
    public async Task<StoredCartDto> Handle(string accountId, ReplaceCart command, CancellationToken ct)
    {
        return StoredCartProjection.Map(await carts.Replace(accountId, command, ct));
    }
}
