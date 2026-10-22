using Microsoft.EntityFrameworkCore;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Infrastructure;
internal sealed class OrderCancellations(OrderingDbContext db) : IOrderCancellations
{
    public async Task<DemoOrderDto> Cancel(Guid id, string reason, string actor, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var plan = await db.ReservationPlans.FromSqlInterpolated($"""SELECT * FROM ordering."ReservationPlans" WHERE "Id"={id} FOR UPDATE""").SingleOrDefaultAsync(ct) ?? throw new BusinessException("Comanda nu există.", 404);
        var order = await db.DemoOrders.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new BusinessException("Comanda nu există.", 404);
        var now = DateTimeOffset.UtcNow;
        order.Cancel(reason, actor, now);
        plan.Restore(now);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return DemoOrderProjection.Map(order);
    }
}
