using Microsoft.EntityFrameworkCore;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Domain;
namespace SurubDirect.Ordering.Infrastructure;
internal sealed class PartialReturnReceipts(OrderingDbContext db, TimeProvider clock) : IPartialReturnReceipts
{
    public async Task Retry(Guid orderId, Guid requestId, Guid operationId, long expectedVersion, string? reason, string actor, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // Same request lock as the worker: no Inventory call or order lock is needed.
        // Read owned audit rows in a fresh statement after the parent lock is acquired.
        // A joined query can otherwise retain the pre-wait snapshot of a concurrent retry.
        var request = await db.PartialReturns.FromSqlInterpolated($"""SELECT * FROM ordering."PartialReturns" WHERE "Id"={requestId} AND "OrderId"={orderId} FOR UPDATE""").AsSplitQuery().SingleOrDefaultAsync(ct)
            ?? throw new BusinessException("Cererea nu există pentru această comandă.", 404);
        request.RetryReceipt(operationId, expectedVersion, reason, actor, clock.GetUtcNow());
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
    public async Task Receive(Guid orderId, Guid requestId, Guid operationId, long expectedVersion, string? reason, string actor, bool resalableConfirmed, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({requestId.ToString()}, 2))", ct);
        _ = await db.ReservationPlans.FromSqlInterpolated($"""SELECT * FROM ordering."ReservationPlans" WHERE "Id"={orderId} FOR UPDATE""").SingleOrDefaultAsync(ct)
            ?? throw new BusinessException("Comanda nu există.", 404);
        var request = await db.PartialReturns.FromSqlInterpolated($"""SELECT * FROM ordering."PartialReturns" WHERE "Id"={requestId} AND "OrderId"={orderId} FOR UPDATE""").SingleOrDefaultAsync(ct)
            ?? throw new BusinessException("Cererea nu există pentru această comandă.", 404);
        request.Receive(operationId, expectedVersion, reason, actor, resalableConfirmed, clock.GetUtcNow());
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task<int> Process(Func<PartialReturnRequest, CancellationToken, Task> reconcile, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var now = clock.GetUtcNow();
        // Workers lock only the request; they never acquire an order/plan lock afterward.
        var candidates = await db.PartialReturns.FromSqlInterpolated($"""SELECT * FROM ordering."PartialReturns" WHERE "Status"='Restoring' AND "NextReceiptAttemptAt"<={now} ORDER BY "NextReceiptAttemptAt","Id" LIMIT 1 FOR UPDATE SKIP LOCKED""").ToListAsync(ct);
        var request = candidates.SingleOrDefault();
        if (request is null) { await tx.CommitAsync(ct); return 0; }
        await reconcile(request, ct);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return 1;
    }
}
