using Microsoft.EntityFrameworkCore;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Domain;
namespace SurubDirect.Ordering.Infrastructure;
internal sealed class PaymentSimulations(OrderingDbContext db) : IPaymentSimulations
{
    public async Task Apply(Guid id, Guid operationId, long expectedVersion, string action, string reason, string actor, CancellationToken ct)
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
            if (previous.Version != expectedVersion + 1 || previous.Action != action || previous.Reason != reason)
                throw new BusinessException("Cheia operației a fost utilizată pentru altă simulare.", 409);
            return;
        }
        var payment = await db.PaymentSimulations.SingleOrDefaultAsync(x => x.OrderId == id, ct);
        if (payment is null)
        {
            payment = PaymentSimulation.Create(order);
            db.PaymentSimulations.Add(payment);
        }
        db.PaymentSimulationEvents.Add(payment.Apply(order.Status, operationId, expectedVersion, action, reason, actor, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
