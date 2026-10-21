using Microsoft.EntityFrameworkCore;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Contracts;
using SurubDirect.Ordering.Domain;

namespace SurubDirect.Ordering.Infrastructure;
internal sealed class PreparedCarts(OrderingDbContext db) : IPreparedCarts
{
    public async Task<PreparedCartDto?> Read(string owner, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, ct);
        var cart = await db.Carts.AsNoTracking().SingleOrDefaultAsync(x => x.OwnerHash == owner, ct) ?? throw new BusinessException("Coșul nu este accesibil.", 401);
        cart.EnsureActive(DateTimeOffset.UtcNow);
        if (cart.PreparedId is not Guid id)
            return null;
        var snapshot = await db.PreparedCarts.AsNoTracking().SingleAsync(x => x.Id == id, ct);
        var plan = await db.ReservationPlans.AsNoTracking().SingleAsync(x => x.Id == id, ct);
        await tx.CommitAsync(ct);
        return PreparedCartProjection.Map(snapshot, plan);
    }

    public async Task<PreparedCartDto> Save(string owner, PreparedCart snapshot, ReservationPlan plan, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var cart = await db.Carts.FromSqlInterpolated($"""SELECT * FROM ordering."Carts" WHERE "OwnerHash"={owner} FOR UPDATE""").SingleOrDefaultAsync(ct) ?? throw new BusinessException("Coșul nu este accesibil.", 401);
        cart.EnsureActive(DateTimeOffset.UtcNow);
        if (cart.Id != snapshot.CartId)
            throw new BusinessException("Coșul s-a modificat.", 409);
        if (cart.PreparedId == snapshot.Id)
        {
            var existing = await db.PreparedCarts.AsNoTracking().SingleAsync(x => x.Id == snapshot.Id, ct);
            var current = await db.ReservationPlans.AsNoTracking().SingleAsync(x => x.Id == snapshot.Id, ct);
            await tx.CommitAsync(ct);
            return PreparedCartProjection.Map(existing, current);
        }

        if (await db.ReservationPlans.AnyAsync(x => x.Id == snapshot.Id, ct))
            throw new BusinessException("Cheia pregătirii este deja folosită.", 409);
        cart.AttachPreparation(snapshot.Id, snapshot.SourceVersion, DateTimeOffset.UtcNow);
        db.PreparedCarts.Add(snapshot);
        db.ReservationPlans.Add(plan);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return PreparedCartProjection.Map(snapshot, plan);
    }

    public async Task Cancel(string owner, Guid id, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var cart = await db.Carts.FromSqlInterpolated($"""SELECT * FROM ordering."Carts" WHERE "OwnerHash"={owner} FOR UPDATE""").SingleOrDefaultAsync(ct) ?? throw new BusinessException("Coșul nu este accesibil.", 401);
        if (cart.PreparedId is null)
        {
            await tx.CommitAsync(ct);
            return;
        }

        if (cart.PreparedId != id)
            throw new BusinessException("Pregătirea coșului s-a modificat.", 409);
        var plan = await db.ReservationPlans.FromSqlInterpolated($"""SELECT * FROM ordering."ReservationPlans" WHERE "Id"={id} FOR UPDATE""").SingleAsync(ct);
        if (plan.Terminal)
        {
            db.PreparedCarts.Remove(await db.PreparedCarts.SingleAsync(x => x.Id == id, ct));
            cart.DetachPreparation(DateTimeOffset.UtcNow);
        }
        else
            plan.Cancel(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
