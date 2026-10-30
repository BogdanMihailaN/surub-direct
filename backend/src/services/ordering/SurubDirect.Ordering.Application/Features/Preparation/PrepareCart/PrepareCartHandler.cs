using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Contracts;
namespace SurubDirect.Ordering.Application;
public sealed class PrepareCartHandler(IShoppingCartRepository carts, IPreparedCarts prepared, QuoteCartHandler quote)
{
    public async Task<PreparedCartDto> Handle(string owner, PrepareCart command, CancellationToken ct)
    {
        var cart = await carts.Find(owner, ct) ?? throw new BusinessException("Coșul nu este accesibil.", 401);
        return await new CartPreparation(prepared, quote).Handle(cart, command, ct);
    }
}
