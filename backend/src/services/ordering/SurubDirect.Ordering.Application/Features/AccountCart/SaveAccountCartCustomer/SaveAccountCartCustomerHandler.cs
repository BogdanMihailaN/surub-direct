using SurubDirect.Ordering.Contracts;
namespace SurubDirect.Ordering.Application;
public sealed class SaveAccountCartCustomerHandler(IAccountCarts carts)
{
    public async Task<StoredCartDto> Handle(string accountId, SaveCartCustomer command, CancellationToken ct)
    {
        return StoredCartProjection.Map(await carts.SaveCustomer(accountId, command, ct));
    }
}
