using Microsoft.EntityFrameworkCore;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Domain;

namespace SurubDirect.Ordering.Infrastructure;
internal sealed class ReservationPlans(OrderingDbContext db) : IReservationPlans
{
    private Task Lock(Guid id, CancellationToken ct) => db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({id.ToString()}, 1))", ct);
    public async Task<ReservationPlan> Save(ReservationPlan plan, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await Lock(plan.Id, ct);
        var existing = await db.ReservationPlans.SingleOrDefaultAsync(x => x.Id == plan.Id, ct);
        if (existing is not null)
            existing.EnsureSame(plan);
        else
        {
            db.ReservationPlans.Add(plan);
            await db.SaveChangesAsync(ct);
        }

        await tx.CommitAsync(ct);
        return existing ?? plan;
    }

    public Task<ReservationPlan?> Read(Guid id, CancellationToken ct) => db.ReservationPlans.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<ReservationPlan> Cancel(Guid id, Guid order, CancellationToken ct)
    {
        var tombstone = ReservationPlan.CancelBeforeCreation(id, order, DateTimeOffset.UtcNow);
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await Lock(id, ct);
        var plan = await db.ReservationPlans.FromSqlInterpolated($"""SELECT * FROM ordering."ReservationPlans" WHERE "Id"={id} FOR UPDATE""").SingleOrDefaultAsync(ct) ?? tombstone;
        if (ReferenceEquals(plan, tombstone))
            db.ReservationPlans.Add(plan);
        if (plan.OrderId != order)
            throw new BusinessException("Planul aparține altei comenzi.", 409);
        plan.Cancel(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return plan;
    }

    public async Task<int> Process(Func<ReservationPlan, CancellationToken, Task> reconcile, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var now = DateTimeOffset.UtcNow;
        var list = await db.ReservationPlans.FromSqlInterpolated($"""SELECT * FROM ordering."ReservationPlans" WHERE "Status" NOT IN ('Released','Expired','Consumed') AND "NextAttemptAt"<={now} ORDER BY "NextAttemptAt","Id" LIMIT 1 FOR UPDATE SKIP LOCKED""").ToListAsync(ct);
        var plan = list.SingleOrDefault();
        if (plan is null)
        {
            await tx.CommitAsync(ct);
            return 0;
        }

        await reconcile(plan, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return 1;
    }
}
