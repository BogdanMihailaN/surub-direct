using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Contracts;
namespace SurubDirect.Ordering.Application;
public sealed class PrepareAccountCartHandler(IAccountCarts carts, IPreparedCarts prepared, QuoteCartHandler quote)
{
    public async Task<PreparedCartDto> Handle(string accountId, PrepareCart command, CancellationToken ct)
    {
        var cart = await carts.Find(accountId, ct) ?? throw new BusinessException("Coșul contului nu există.", 404);
        return await new CartPreparation(prepared, quote).Handle(cart, command, ct);
    }
}
