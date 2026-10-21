using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Application;
public sealed class GetCartHandler(IShoppingCartRepository repository)
{
    public async Task<StoredCartDto?> Handle(string ownerHash, CancellationToken ct)
    {
        var c = await repository.Find(ownerHash, ct);
        if (c is null)
            return null;
        c.EnsureActive(DateTimeOffset.UtcNow);
        return StoredCartProjection.Map(c);
    }
}
