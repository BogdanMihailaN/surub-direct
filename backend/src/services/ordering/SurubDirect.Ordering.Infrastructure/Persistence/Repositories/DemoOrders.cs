using Microsoft.EntityFrameworkCore;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Contracts;
using SurubDirect.Ordering.Domain;

namespace SurubDirect.Ordering.Infrastructure;
internal sealed class DemoOrders(OrderingDbContext db) : IDemoOrders
{
    public async Task<DemoOrderDto> Confirm(string owner, Guid id, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var cart = await db.Carts.FromSqlInterpolated($"""SELECT * FROM ordering."Carts" WHERE "OwnerHash"={owner} FOR UPDATE""").SingleOrDefaultAsync(ct) ?? throw new BusinessException("Coșul nu este accesibil.", 401);
        var existing = await db.DemoOrders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.OwnerHash == owner, ct);
        if (existing is not null)
        {
            await tx.CommitAsync(ct);
            return DemoOrderProjection.Map(existing);
        }

        cart.EnsureActive(DateTimeOffset.UtcNow);
        if (cart.PreparedId != id)
            throw new BusinessException("Pregătirea coșului s-a modificat.", 409);
        var plan = await db.ReservationPlans.FromSqlInterpolated($"""SELECT * FROM ordering."ReservationPlans" WHERE "Id"={id} FOR UPDATE""").SingleAsync(ct);
        plan.Confirm(DateTimeOffset.UtcNow);
        var snapshot = await db.PreparedCarts.AsNoTracking().SingleAsync(x => x.Id == id, ct);
        var order = DemoOrder.Create(owner, snapshot, DateTimeOffset.UtcNow);
        db.DemoOrders.Add(order);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return DemoOrderProjection.Map(order);
    }

    public async Task<DemoOrderPageDto> Read(string owner, int page, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, ct);
        var query = db.DemoOrders.AsNoTracking().Where(x => x.OwnerHash == owner);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.Sequence).Skip((page - 1) * 25).Take(25).ToListAsync(ct);
        await tx.CommitAsync(ct);
        return new(items.Select(DemoOrderProjection.Map).ToArray(), page, total, 25);
    }

    public async Task Complete(string owner, Guid id, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var cart = await db.Carts.FromSqlInterpolated($"""SELECT * FROM ordering."Carts" WHERE "OwnerHash"={owner} FOR UPDATE""").SingleOrDefaultAsync(ct) ?? throw new BusinessException("Coșul nu este accesibil.", 401);
        var order = await db.DemoOrders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.OwnerHash == owner, ct) ?? throw new BusinessException("Comanda nu este accesibilă.", 404);
        if (order.Status == "Confirming")
            throw new BusinessException("Confirmarea este încă în curs.", 409);
        if (cart.PreparedId is null)
        {
            await tx.CommitAsync(ct);
            return;
        }

        if (cart.PreparedId != id)
            throw new BusinessException("Coșul are altă pregătire.", 409);
        cart.DetachPreparation(DateTimeOffset.UtcNow);
        if (order.Status == "Confirmed")
            cart.Replace(cart.Id, cart.Version, [], DateTimeOffset.UtcNow);
        db.PreparedCarts.Remove(await db.PreparedCarts.SingleAsync(x => x.Id == id, ct));
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
