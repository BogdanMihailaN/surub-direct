using SurubDirect.Ordering.Contracts;
namespace SurubDirect.Ordering.Application;
public sealed class ImportGuestCartHandler(IAccountCarts carts)
{
    public async Task<StoredCartDto> Handle(string accountId, string guestHash, ImportGuestCart command, CancellationToken ct)
    {
        return StoredCartProjection.Map(await carts.Import(accountId, guestHash, command, ct));
    }
}
