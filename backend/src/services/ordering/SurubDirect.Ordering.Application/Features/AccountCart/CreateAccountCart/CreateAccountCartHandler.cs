using SurubDirect.Ordering.Contracts;
namespace SurubDirect.Ordering.Application;
public sealed class CreateAccountCartHandler(IAccountCarts carts)
{
    public async Task<StoredCartDto> Handle(string accountId, CancellationToken ct)
    {
        return StoredCartProjection.Map(await carts.Create(accountId, ct));
    }
}
