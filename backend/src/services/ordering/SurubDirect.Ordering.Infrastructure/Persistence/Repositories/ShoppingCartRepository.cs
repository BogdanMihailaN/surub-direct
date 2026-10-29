using Microsoft.EntityFrameworkCore;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Domain;

namespace SurubDirect.Ordering.Infrastructure;
internal sealed class ShoppingCartRepository(OrderingDbContext db) : IShoppingCartRepository
{
    public Task<ShoppingCart?> Find(string hash, CancellationToken ct) => db.Carts.AsNoTracking().SingleOrDefaultAsync(c => c.OwnerHash == hash && c.AccountId == null, ct);
    public async Task<ShoppingCart> Create(string hash, CancellationToken ct)
    {
        var cart = ShoppingCart.Create(hash, DateTimeOffset.UtcNow);
        db.Carts.Add(cart);
        await db.SaveChangesAsync(ct);
        return cart;
    }

    public async Task<ShoppingCart> Replace(string hash, Guid id, int version, IReadOnlyList<CartSelection>? lines, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var cart = await db.Carts.FromSqlInterpolated($"""SELECT * FROM ordering."Carts" WHERE "OwnerHash" = {hash} AND "AccountId" IS NULL FOR UPDATE""").SingleOrDefaultAsync(ct) ?? throw new BusinessException("Coșul nu mai este accesibil. Reîncarcă pagina.", 401);
        cart.Replace(id, version, lines, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return cart;
    }

    public async Task<ShoppingCart> SaveCustomer(string hash, Guid id, int version, CustomerDraft? customer, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var cart = await db.Carts.FromSqlInterpolated($"""SELECT * FROM ordering."Carts" WHERE "OwnerHash" = {hash} AND "AccountId" IS NULL FOR UPDATE""").SingleOrDefaultAsync(ct) ?? throw new BusinessException("Coșul nu mai este accesibil. Reîncarcă pagina.", 401);
        cart.SaveCustomer(id, version, customer, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return cart;
    }
}
