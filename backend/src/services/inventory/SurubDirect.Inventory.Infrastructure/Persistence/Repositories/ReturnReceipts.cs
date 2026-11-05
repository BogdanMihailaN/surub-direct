using Microsoft.EntityFrameworkCore;
using SurubDirect.BuildingBlocks;
using SurubDirect.Inventory.Application;
using SurubDirect.Inventory.Domain;
namespace SurubDirect.Inventory.Infrastructure;
internal sealed class ReturnReceipts(InventoryDbContext db) : IReturnReceipts
{
    public async Task<ReturnReceipt> Receive(ReturnReceipt candidate, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // Receipt lock deduplicates globally; reservation lock serializes totals with full restoration.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({candidate.Id.ToString()}, 1))", ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({candidate.ReservationId.ToString()}, 0))", ct);
        var previous = await db.ReturnReceipts.SingleOrDefaultAsync(x => x.Id == candidate.Id, ct);
        if (previous is not null)
        {
            previous.EnsureSame(candidate);
            await tx.CommitAsync(ct);
            return previous;
        }
        var reservation = await db.Reservations.SingleOrDefaultAsync(x => x.Id == candidate.ReservationId, ct)
            ?? throw new BusinessException("Rezervarea nu există.", 404);
        reservation.EnsureOrder(candidate.OrderId);
        if (reservation.Status != ReservationStatus.Consumed)
            throw new BusinessException("Recepția returului necesită consumul original, fără restituire integrală.", 409);
        foreach (var line in candidate.Lines)
        {
            var original = await db.Consumptions.SingleOrDefaultAsync(x => x.ReservationId == candidate.ReservationId && x.VariantId == line.VariantId, ct);
            var reserved = reservation.Lines.SingleOrDefault(x => x.VariantId == line.VariantId);
            if (original is null || reserved is null || original.OrderId != candidate.OrderId || original.Quantity != reserved.Quantity)
                throw new BusinessException("SKU-ul nu are un consum original valid pentru această comandă.", 409);
            var received = await db.ReturnReceipts.Where(x => x.ReservationId == candidate.ReservationId)
                .SelectMany(x => x.Lines).Where(x => x.VariantId == line.VariantId).SumAsync(x => (long)x.Quantity, ct);
            if (received + line.Quantity > original.Quantity)
                throw new BusinessException("Cantitatea cumulată returnată depășește consumul original.", 409);
            var changed = await db.Database.ExecuteSqlInterpolatedAsync($"""UPDATE inventory."Balances" SET "OnHand"="OnHand"+{line.Quantity} WHERE "VariantId"={line.VariantId} AND "OnHand" <= {int.MaxValue - line.Quantity}""", ct);
            if (changed != 1)
                throw new BusinessException("Soldul nu permite recepția returului.", 409);
        }
        db.ReturnReceipts.Add(candidate);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return candidate;
    }
}
