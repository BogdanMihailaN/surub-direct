using Microsoft.EntityFrameworkCore;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Infrastructure;
internal sealed class AdminDemoOrders(OrderingDbContext db) : IAdminDemoOrders
{
    public async Task<AdminOrderPageDto> Read(string? status, long? sequence, int page, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, ct);
        var query = db.DemoOrders.AsNoTracking().AsQueryable();
        if (status is not null)
            query = query.Where(x => x.Status == status);
        if (sequence is not null)
            query = query.Where(x => x.Sequence == sequence);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.Sequence).Skip((page - 1) * 25).Take(25).ToListAsync(ct);
        await tx.CommitAsync(ct);
        return new(items.Select(x => new AdminOrderSummaryDto(x.Id, $"DEMO-{x.Sequence:D8}", x.Status, x.Customer.FullName, x.Currency, x.Lines.Sum(l => l.GrossTotal), x.CreatedAt)).ToArray(), total, page, 25);
    }

    public async Task<AdminOrderDetailDto?> Find(Guid id, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, ct);
        var order = await db.DemoOrders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (order is null)
            return null;
        var plan = await db.ReservationPlans.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new BusinessException("Starea confirmării nu este disponibilă.", 503);
        await tx.CommitAsync(ct);
        return new(DemoOrderProjection.Map(order), ReservationPlanProjection.Map(plan));
    }
}
