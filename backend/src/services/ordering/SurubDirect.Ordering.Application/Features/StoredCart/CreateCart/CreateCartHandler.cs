using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Application;
public sealed class CreateCartHandler(IShoppingCartRepository repository)
{
    public async Task<StoredCartDto> Handle(string ownerHash, CancellationToken ct) => StoredCartProjection.Map(await repository.Create(ownerHash, ct));
}
