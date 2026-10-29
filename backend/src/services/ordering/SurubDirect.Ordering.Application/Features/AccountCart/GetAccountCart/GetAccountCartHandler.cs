using SurubDirect.Ordering.Contracts;
namespace SurubDirect.Ordering.Application;
public sealed class GetAccountCartHandler(IAccountCarts carts)
{
    public async Task<StoredCartDto?> Handle(string accountId, CancellationToken ct)
    {
        var cart = await carts.Find(accountId, ct);
        cart?.EnsureActive(DateTimeOffset.UtcNow);
        return cart is null ? null : StoredCartProjection.Map(cart);
    }
}
