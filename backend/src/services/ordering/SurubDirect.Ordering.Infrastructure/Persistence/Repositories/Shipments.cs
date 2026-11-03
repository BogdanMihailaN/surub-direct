using Microsoft.EntityFrameworkCore;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
namespace SurubDirect.Ordering.Infrastructure;
internal sealed class Shipments(OrderingDbContext db) : IShipments
{
    public async Task Change(Guid id, Guid operationId, long expectedVersion, string target, string reason, string actor, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // Same lock order as cancellation and reservation reconciliation.
        _ = await db.ReservationPlans.FromSqlInterpolated($"""SELECT * FROM ordering."ReservationPlans" WHERE "Id"={id} FOR UPDATE""")
            .SingleOrDefaultAsync(ct) ?? throw new BusinessException("Comanda nu există.", 404);
        var order = await db.DemoOrders.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new BusinessException("Comanda nu există.", 404);
        var previous = await db.ShipmentChanges.SingleOrDefaultAsync(x => x.OrderId == id && x.OperationId == operationId, ct);
        if (previous is not null)
        {
            if (previous.Version != expectedVersion + 1 || previous.ToStatus != target || previous.Reason != reason)
                throw new BusinessException("Identificatorul operației a fost folosit pentru altă schimbare.", 409);
            return;
        }
        var payment = await db.PaymentSimulations.AsNoTracking().SingleOrDefaultAsync(x => x.OrderId == id, ct);
        db.ShipmentChanges.Add(order.ChangeShipment(operationId, expectedVersion, target, reason, actor, payment?.Status, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
