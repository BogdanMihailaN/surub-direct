using Microsoft.EntityFrameworkCore;
using SurubDirect.BuildingBlocks;
using SurubDirect.Inventory.Application;
using SurubDirect.Inventory.Domain;

namespace SurubDirect.Inventory.Infrastructure;
internal sealed class StockReservations(InventoryDbContext db, TimeProvider clock) : IStockReservations
{
    private Task Lock(Guid id, CancellationToken ct) => db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({id.ToString()}, 0))", ct);
    public async Task<StockReservation> Reserve(StockReservation candidate, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await Lock(candidate.Id, ct);
        var existing = await db.Reservations.SingleOrDefaultAsync(x => x.Id == candidate.Id, ct);
        if (existing is not null)
        {
            existing.EnsureSame(candidate);
            if (existing.Status == ReservationStatus.Active && existing.ExpiresAt <= clock.GetUtcNow())
                await End(existing, ct);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return existing;
        }

        if (candidate.ExpiresAt <= clock.GetUtcNow())
            throw new BusinessException("Cererea de rezervare a expirat.", 409);
        // All multi-SKU writers acquire balance locks in the same deterministic order.
        foreach (var line in candidate.Lines.OrderBy(x => x.VariantId))
        {
            var changed = await db.Database.ExecuteSqlInterpolatedAsync($"""UPDATE inventory."Balances" SET "Reserved" = "Reserved" + {line.Quantity} WHERE "VariantId" = {line.VariantId} AND "OnHand" - "Reserved" >= {line.Quantity}""", ct);
            if (changed != 1)
                throw new BusinessException("Stoc insuficient pentru rezervare. Niciun articol nu a fost rezervat.", 409);
        }

        if (candidate.ExpiresAt <= clock.GetUtcNow())
            throw new BusinessException("Cererea de rezervare a expirat.", 409);
        db.Reservations.Add(candidate);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return candidate;
    }

    public async Task<StockReservation> Consume(Guid id, Guid orderId, CancellationToken ct)
    {
        if (id == Guid.Empty || orderId == Guid.Empty)
            throw new BusinessException("Identificator rezervare sau comandă invalid.");
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await Lock(id, ct);
        var current = await db.Reservations.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new BusinessException("Rezervarea nu există.", 404);
        current.EnsureOrder(orderId);
        if (current.Status == ReservationStatus.Active)
        {
            // Lock all balances before checking expiry: waiting for a writer can cross the deadline.
            foreach (var line in current.Lines.OrderBy(x => x.VariantId))
                _ = await db.Balances.FromSqlInterpolated($"""SELECT * FROM inventory."Balances" WHERE "VariantId"={line.VariantId} FOR UPDATE""").AsNoTracking().SingleAsync(ct);
            var now = clock.GetUtcNow();
            if (current.ExpiresAt <= now)
                await End(current, ct);
            else
            {
                current.Consume(now);
                foreach (var line in current.Lines.OrderBy(x => x.VariantId))
                {
                    var changed = await db.Database.ExecuteSqlInterpolatedAsync($"""UPDATE inventory."Balances" SET "OnHand" = "OnHand" - {line.Quantity}, "Reserved" = "Reserved" - {line.Quantity} WHERE "VariantId"={line.VariantId} AND "OnHand">={line.Quantity} AND "Reserved">={line.Quantity}""", ct);
                    if (changed != 1)
                        throw new InvalidOperationException("Reservation balance invariant violated.");
                    db.Consumptions.Add(StockConsumption.From(current, line));
                }
            }

            await db.SaveChangesAsync(ct);
        }

        await tx.CommitAsync(ct);
        return current;
    }

    public async Task<StockReservation> Release(Guid id, Guid orderId, CancellationToken ct)
    {
        var cancellation = StockReservation.CancelBeforeCreation(id, orderId, clock.GetUtcNow());
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await Lock(id, ct);
        var current = await db.Reservations.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (current is null)
        {
            current = cancellation;
            db.Reservations.Add(current);
        }
        else
        {
            current.EnsureOrder(orderId);
            await End(current, ct);
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return current;
    }

    public async Task<StockReservation?> Read(Guid id, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await Lock(id, ct);
        var current = await db.Reservations.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (current is not null && current.Status == ReservationStatus.Active && current.ExpiresAt <= clock.GetUtcNow())
        {
            await End(current, ct);
            await db.SaveChangesAsync(ct);
        }

        await tx.CommitAsync(ct);
        return current;
    }

    private async Task End(StockReservation current, CancellationToken ct)
    {
        if (!current.End(clock.GetUtcNow()))
            return;
        foreach (var line in current.Lines.OrderBy(x => x.VariantId))
        {
            var changed = await db.Database.ExecuteSqlInterpolatedAsync($"""UPDATE inventory."Balances" SET "Reserved" = "Reserved" - {line.Quantity} WHERE "VariantId" = {line.VariantId} AND "Reserved" >= {line.Quantity}""", ct);
            if (changed != 1)
                throw new InvalidOperationException("Reservation balance invariant violated.");
        }
    }

    public async Task<int> Expire(int limit, CancellationToken ct)
    {
        if (limit is < 1 or > 1000)
            throw new ArgumentOutOfRangeException(nameof(limit));
        var now = clock.GetUtcNow();
        var ids = await db.Reservations.AsNoTracking().Where(x => x.Status == ReservationStatus.Active && x.ExpiresAt <= now).OrderBy(x => x.ExpiresAt).ThenBy(x => x.Id).Take(limit).Select(x => x.Id).ToListAsync(ct);
        foreach (var id in ids)
            await Read(id, ct);
        return ids.Count;
    }
}
