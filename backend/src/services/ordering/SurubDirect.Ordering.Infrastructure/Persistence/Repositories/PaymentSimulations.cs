using Microsoft.EntityFrameworkCore;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Domain;
namespace SurubDirect.Ordering.Infrastructure;
internal sealed class PaymentSimulations(OrderingDbContext db) : IPaymentSimulations
{
    public Task RefundReturn(Guid orderId, Guid requestId, Guid operationId, long expectedVersion, string reason, string actor, CancellationToken ct)
        => ApplyCore(orderId, operationId, expectedVersion, "ReturnRefund", reason, actor, requestId, ct);

    public Task Apply(Guid id, Guid operationId, long expectedVersion, string action, string reason, string actor, CancellationToken ct)
        => ApplyCore(id, operationId, expectedVersion, action, reason, actor, null, ct);

    private async Task ApplyCore(Guid id, Guid operationId, long expectedVersion, string action, string reason, string actor, Guid? requestId, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // Serialize with cancellation, processing and reservation reconciliation.
        _ = await db.ReservationPlans.FromSqlInterpolated($"""SELECT * FROM ordering."ReservationPlans" WHERE "Id"={id} FOR UPDATE""")
            .SingleOrDefaultAsync(ct) ?? throw new BusinessException("Comanda nu există.", 404);
        var order = await db.DemoOrders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new BusinessException("Comanda nu există.", 404);
        var previous = await db.PaymentSimulationEvents.AsNoTracking().SingleOrDefaultAsync(x => x.OrderId == id && x.OperationId == operationId, ct);
        if (previous is not null)
        {
            if (previous.Version != expectedVersion + 1 || previous.Action != action || previous.Reason != reason || previous.PartialReturnId != requestId
                || (requestId.HasValue && previous.Actor != actor))
                throw new BusinessException("Cheia operației a fost utilizată pentru altă simulare.", 409);
            return;
        }
        var payment = await db.PaymentSimulations.SingleOrDefaultAsync(x => x.OrderId == id, ct);
        if (payment is null)
        {
            payment = PaymentSimulation.Create(order);
            db.PaymentSimulations.Add(payment);
        }
        PaymentSimulationEvent entry;
        if (requestId is Guid returnId)
        {
            var request = await db.PartialReturns.AsNoTracking().SingleOrDefaultAsync(x => x.Id == returnId && x.OrderId == id, ct)
                ?? throw new BusinessException("Returul nu există pentru această comandă.", 404);
            if (await db.PaymentSimulationEvents.AnyAsync(x => x.OrderId == id && x.PartialReturnId == returnId, ct))
                throw new BusinessException("Rambursarea acestui retur a fost deja înregistrată.", 409);
            entry = payment.RefundReturn(order, request, operationId, expectedVersion, reason, actor, DateTimeOffset.UtcNow);
        }
        else
        {
            entry = payment.Apply(order.Status, operationId, expectedVersion, action, reason, actor, DateTimeOffset.UtcNow);
        }
        db.PaymentSimulationEvents.Add(entry);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
