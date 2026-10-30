using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Contracts;
namespace SurubDirect.Ordering.Application;
public sealed class CompleteAccountOrderHandler(IAccountCarts carts, IDemoOrders orders)
{
    public async Task Handle(string accountId, Guid id, CancellationToken ct)
    {
        var cart = await carts.Find(accountId, ct) ?? throw new BusinessException("Coșul contului nu există.", 404);
        await orders.Complete(cart.OwnerHash, id, ct, accountId);
    }
}
