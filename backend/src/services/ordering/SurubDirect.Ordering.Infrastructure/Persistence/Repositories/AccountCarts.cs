using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Domain;

namespace SurubDirect.Ordering.Infrastructure;
internal sealed class AccountCarts(OrderingDbContext db) : IAccountCarts
{
    public Task<ShoppingCart?> Find(string accountId, CancellationToken ct) =>
        db.Carts.AsNoTracking().SingleOrDefaultAsync(x => x.AccountId == accountId, ct);

    public async Task<ShoppingCart> Create(string accountId, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // Serialize first creation even when no cart row exists yet.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({"account-cart:" + accountId}, 0))", ct);
        var cart = await db.Carts.FromSqlInterpolated($"""SELECT * FROM ordering."Carts" WHERE "AccountId" = {accountId} FOR UPDATE""").SingleOrDefaultAsync(ct);
        if (cart is null)
        {
            cart = ShoppingCart.CreateForAccount(accountId, Convert.ToHexString(RandomNumberGenerator.GetBytes(32)), DateTimeOffset.UtcNow);
            db.Carts.Add(cart);
        }
        else cart.RestartExpired(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return cart;
    }

    public async Task<ShoppingCart> Replace(string accountId, ReplaceCart command, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var cart = await Lock(accountId, ct);
        cart.Replace(command.CartId, command.Version, command.Lines, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return cart;
    }

    public async Task<ShoppingCart> SaveCustomer(string accountId, SaveCartCustomer command, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var cart = await Lock(accountId, ct);
        cart.SaveCustomer(command.CartId, command.Version, command.Customer, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return cart;
    }

    public async Task<ShoppingCart> Import(string accountId, string guestHash, ImportGuestCart command, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // Guest row first, matching the preparation/order lock order. Account carts never serve as sources.
        var source = await db.Carts.FromSqlInterpolated($"""SELECT * FROM ordering."Carts" WHERE "OwnerHash" = {guestHash} AND "AccountId" IS NULL FOR UPDATE""").SingleOrDefaultAsync(ct)
            ?? throw new BusinessException("Coșul anonim nu mai este accesibil.", 404);
        var target = await Lock(accountId, ct);
        var now = DateTimeOffset.UtcNow;
        source.EnsureActive(now);
        target.EnsureActive(now);
        if (source.Id != command.CartId || source.Version != command.Version || source.PreparedId is not null || source.Lines.Count == 0)
            throw new BusinessException("Coșul anonim s-a modificat sau are o pregătire activă. Reîncarcă înainte de transfer.", 409);
        if (target.Id != command.TargetCartId || target.Version != command.TargetVersion || target.Lines.Count != 0 || target.Customer is not null || target.PreparedId is not null)
            throw new BusinessException("Coșul contului s-a modificat sau nu este gol. Transferul nu suprascrie date existente.", 409);
        target.Replace(target.Id, target.Version, source.Lines, now);
        if (command.IncludeCustomer && source.Customer is not null)
            target.SaveCustomer(target.Id, target.Version, source.Customer, now);
        source.Replace(source.Id, source.Version, [], now);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return target;
    }

    private async Task<ShoppingCart> Lock(string accountId, CancellationToken ct) =>
        await db.Carts.FromSqlInterpolated($"""SELECT * FROM ordering."Carts" WHERE "AccountId" = {accountId} FOR UPDATE""").SingleOrDefaultAsync(ct)
        ?? throw new BusinessException("Coșul contului nu există. Creează un coș înainte de a continua.", 404);
}
