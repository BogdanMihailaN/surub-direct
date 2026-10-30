using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Contracts;
namespace SurubDirect.Ordering.Application;
public sealed class CancelAccountPreparationHandler(IAccountCarts carts, IPreparedCarts prepared)
{
    public async Task Handle(string accountId, Guid id, CancellationToken ct)
    {
        var cart = await carts.Find(accountId, ct) ?? throw new BusinessException("Coșul contului nu există.", 404);
        await prepared.Cancel(cart.OwnerHash, id, ct, accountId);
    }
}
