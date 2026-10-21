using SurubDirect.Ordering.Domain;

namespace SurubDirect.Ordering.Application;
public interface IShoppingCartRepository
{
    Task<ShoppingCart?> Find(string ownerHash, CancellationToken ct);
    Task<ShoppingCart> Create(string ownerHash, CancellationToken ct);
    Task<ShoppingCart> Replace(string ownerHash, Guid cartId, int version, IReadOnlyList<CartSelection>? lines, CancellationToken ct);
}
