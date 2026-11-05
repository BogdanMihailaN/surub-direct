using Microsoft.EntityFrameworkCore;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
namespace SurubDirect.Ordering.Infrastructure;
internal sealed class OrderReturns(OrderingDbContext db) : IOrderReturns, IAccountReturnRequests
{
    public Task Change(Guid id, Guid operationId, long expectedVersion, string target, string reason, string actor, CancellationToken ct)
        => ChangeCore(id, operationId, expectedVersion, target, reason, actor, null, ct);

    public Task Request(string accountId, Guid orderId, Guid operationId, string reason, CancellationToken ct)
        => ChangeCore(orderId, operationId, 0, "Requested", reason, "customer:" + accountId, accountId, ct);

    private async Task ChangeCore(Guid id, Guid operationId, long expectedVersion, string target, string reason, string actor, string? expectedAccountId, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // Same lock order as cancellation and reservation reconciliation.
        var plan = await db.ReservationPlans.FromSqlInterpolated($"""SELECT * FROM ordering."ReservationPlans" WHERE "Id"={id} FOR UPDATE""")
            .SingleOrDefaultAsync(ct) ?? throw new BusinessException("Comanda nu există.", 404);
        var order = await db.DemoOrders.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new BusinessException("Comanda nu există.", 404);
        if (expectedAccountId is not null && order.AccountId != expectedAccountId)
            throw new BusinessException("Comanda nu este accesibilă.", 404);
        var previous = await db.ReturnChanges.SingleOrDefaultAsync(x => x.OrderId == id && x.OperationId == operationId, ct);
        if (previous is not null)
        {
            if (previous.Version != expectedVersion + 1 || previous.ToStatus != target || previous.Reason != reason
                || (expectedAccountId is not null && previous.Actor != actor))
                throw new BusinessException("Identificatorul operației a fost folosit pentru altă schimbare.", 409);
            return;
        }

        if (await db.PartialReturns.AnyAsync(x => x.OrderId == id && x.Status != "Rejected", ct))
            throw new BusinessException("Există cereri de retur pe cantități active; returul integral ar dubla selecția.", 409);
        db.ReturnChanges.Add(order.ChangeReturn(operationId, expectedVersion, target, reason, actor, DateTimeOffset.UtcNow));
        if (target == "Restoring") plan.Restore(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
