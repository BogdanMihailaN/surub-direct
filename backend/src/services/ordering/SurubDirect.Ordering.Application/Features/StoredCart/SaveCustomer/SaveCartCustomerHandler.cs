using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Application;
public sealed class SaveCartCustomerHandler(IShoppingCartRepository repository)
{
    public async Task<StoredCartDto> Handle(string ownerHash, SaveCartCustomer command, CancellationToken ct) => StoredCartProjection.Map(await repository.SaveCustomer(ownerHash, command.CartId, command.Version, command.Customer, ct));
}
