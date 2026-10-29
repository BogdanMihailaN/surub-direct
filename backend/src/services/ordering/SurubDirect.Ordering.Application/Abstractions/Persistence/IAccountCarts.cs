using SurubDirect.Ordering.Domain;
namespace SurubDirect.Ordering.Application;
public interface IAccountCarts
{
    Task<ShoppingCart?> Find(string accountId, CancellationToken ct);
    Task<ShoppingCart> Create(string accountId, CancellationToken ct);
    Task<ShoppingCart> Replace(string accountId, ReplaceCart command, CancellationToken ct);
    Task<ShoppingCart> SaveCustomer(string accountId, SaveCartCustomer command, CancellationToken ct);
    Task<ShoppingCart> Import(string accountId, string guestHash, ImportGuestCart command, CancellationToken ct);
}
