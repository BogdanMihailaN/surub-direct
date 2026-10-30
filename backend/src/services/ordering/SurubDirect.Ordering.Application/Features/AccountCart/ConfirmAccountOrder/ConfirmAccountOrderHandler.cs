using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Contracts;
namespace SurubDirect.Ordering.Application;
public sealed class ConfirmAccountOrderHandler(IAccountCarts carts, IDemoOrders orders)
{
    public async Task<DemoOrderDto> Handle(string accountId, Guid id, CancellationToken ct)
    {
        var cart = await carts.Find(accountId, ct) ?? throw new BusinessException("Coșul contului nu există.", 404);
        return await orders.Confirm(cart.OwnerHash, id, ct, accountId);
    }
}
