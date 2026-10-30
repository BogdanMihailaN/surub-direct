using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Contracts;
namespace SurubDirect.Ordering.Application;
public sealed class GetAccountPreparationHandler(IAccountCarts carts, IPreparedCarts prepared)
{
    public async Task<PreparedCartDto?> Handle(string accountId, CancellationToken ct)
    {
        var cart = await carts.Find(accountId, ct) ?? throw new BusinessException("Coșul contului nu există.", 404);
        return await prepared.Read(cart.OwnerHash, ct, accountId);
    }
}
