using Microsoft.EntityFrameworkCore;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Infrastructure;
internal sealed class AccountOrders(OrderingDbContext db, TimeProvider time) : IAccountOrders
{
    public async Task Link(string accountId, string owner, Guid id, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var cart = await db.Carts.FromSqlInterpolated($"""SELECT * FROM ordering."Carts" WHERE "OwnerHash"={owner} FOR UPDATE""").SingleOrDefaultAsync(ct)
            ?? throw new BusinessException("Comanda nu este accesibilă.", 404);
        var order = await db.DemoOrders.FromSqlInterpolated($"""SELECT * FROM ordering."DemoOrders" WHERE "Id"={id} AND "OwnerHash"={owner} FOR UPDATE""").SingleOrDefaultAsync(ct)
            ?? throw new BusinessException("Comanda nu este accesibilă.", 404);
        if (order.AccountId is not null && order.AccountId != accountId)
            throw new BusinessException("Comanda nu este accesibilă.", 404);
        if (order.CartId != cart.Id || cart.PreparedId == id || await db.PreparedCarts.AnyAsync(x => x.Id == id, ct))
            throw new BusinessException("Finalizează fluxul coșului înainte de asocierea comenzii.", 409);
        order.LinkAccount(accountId, time.GetUtcNow());
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
    public async Task<DemoOrderPageDto> Read(string accountId, int page, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, ct);
        var query = db.DemoOrders.AsNoTracking().Where(x => x.AccountId == accountId);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.Sequence).Skip((page - 1) * 25).Take(25).ToListAsync(ct);
        var ids = items.Select(x => x.Id).ToArray();
        var payments = await db.PaymentSimulations.AsNoTracking().Where(x => ids.Contains(x.OrderId)).ToDictionaryAsync(x => x.OrderId, ct);
        var result = items.Select(x => DemoOrderProjection.Map(x, payments.GetValueOrDefault(x.Id))).ToArray();
        await tx.CommitAsync(ct);
        return new(result, page, total, 25);
    }
}
