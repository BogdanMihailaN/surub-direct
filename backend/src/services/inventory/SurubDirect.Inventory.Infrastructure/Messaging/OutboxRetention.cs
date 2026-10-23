using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SurubDirect.Inventory.Application;
using SurubDirect.Inventory.Contracts;

namespace SurubDirect.Inventory.Infrastructure;
internal sealed class OutboxRetention(InventoryDbContext db, TimeProvider clock) : IOutboxRetention
{
    public async Task<OutboxRetentionResult> Run(int retentionDays, int batchSize, bool dryRun, CancellationToken ct)
    {
        if (retentionDays is < 30 or > 3650)
            throw new ArgumentOutOfRangeException(nameof(retentionDays));
        if (batchSize is < 1 or > 1000)
            throw new ArgumentOutOfRangeException(nameof(batchSize));
        var now = clock.GetUtcNow();
        var cutoff = now.AddDays(-retentionDays);
        if (dryRun)
        {
            var count = await db.ReservationOutbox.Where(x => x.PublishedAt != null && x.PublishedAt <= cutoff && x.PayloadRemovedAt == null).OrderBy(x => x.PublishedAt).ThenBy(x => x.Id).Take(batchSize).CountAsync(ct);
            return new(count, 0, true);
        }

        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var rows = await db.ReservationOutbox.FromSqlInterpolated($"""
            SELECT * FROM inventory."ReservationOutbox"
            WHERE "PublishedAt" IS NOT NULL AND "PublishedAt" <= {cutoff} AND "PayloadRemovedAt" IS NULL
            ORDER BY "PublishedAt", "Id" LIMIT {batchSize} FOR UPDATE SKIP LOCKED
            """).ToListAsync(ct);
        foreach (var row in rows)
        {
            row.PayloadHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(row.Payload)));
            row.Payload = "{}";
            row.PayloadRemovedAt = now;
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new(rows.Count, rows.Count, false);
    }
}
