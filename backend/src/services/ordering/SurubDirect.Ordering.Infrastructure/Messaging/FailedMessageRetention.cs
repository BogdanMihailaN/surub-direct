using Microsoft.EntityFrameworkCore;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Infrastructure;
internal sealed class FailedMessageRetention(OrderingDbContext db, TimeProvider clock) : IFailedMessageRetention
{
    public async Task<FailedMessageRetentionResult> Run(int retentionDays, int batchSize, bool dryRun, CancellationToken ct)
    {
        if (retentionDays is < 30 or > 3650)
            throw new ArgumentOutOfRangeException(nameof(retentionDays));
        if (batchSize is < 1 or > 1000)
            throw new ArgumentOutOfRangeException(nameof(batchSize));
        var now = clock.GetUtcNow();
        var cutoff = now.AddDays(-retentionDays);
        // Inbox identity AND body hash must match. Broker confirmation alone is insufficient.
        var candidates = db.FailedReservationMessages.FromSqlInterpolated($"""
            SELECT f.* FROM ordering."FailedReservationMessages" f
            WHERE f."CanReplay" AND f."ReplayRequestId" IS NOT NULL AND f."RequestedAt" IS NOT NULL
              AND f."PublishedAt" <= {cutoff} AND f."CapturedAt" <= {cutoff}
              AND f."PayloadRemovedAt" IS NULL
              AND f."PayloadHash" = upper(encode(sha256(f."Payload"), 'hex'))
              AND EXISTS (SELECT 1 FROM ordering."ReservationInbox" i
                WHERE i."Id" = f."EventId" AND i."PayloadHash" = f."PayloadHash" AND i."ReceivedAt" <= {cutoff})
            ORDER BY f."PublishedAt", f."Id" LIMIT {batchSize}
            """);
        if (dryRun)
            return new(await candidates.AsNoTracking().CountAsync(ct), 0, true);
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // Lock only failed-message rows. Inbox deduplication entries are immutable and retained.
        var rows = await db.FailedReservationMessages.FromSqlInterpolated($"""
            SELECT f.* FROM ordering."FailedReservationMessages" f
            WHERE f."CanReplay" AND f."ReplayRequestId" IS NOT NULL AND f."RequestedAt" IS NOT NULL
              AND f."PublishedAt" <= {cutoff} AND f."CapturedAt" <= {cutoff}
              AND f."PayloadRemovedAt" IS NULL
              AND f."PayloadHash" = upper(encode(sha256(f."Payload"), 'hex'))
              AND EXISTS (SELECT 1 FROM ordering."ReservationInbox" i
                WHERE i."Id" = f."EventId" AND i."PayloadHash" = f."PayloadHash" AND i."ReceivedAt" <= {cutoff})
            ORDER BY f."PublishedAt", f."Id" LIMIT {batchSize} FOR UPDATE OF f SKIP LOCKED
            """).ToListAsync(ct);
        foreach (var row in rows)
        {
            row.Payload = [];
            row.PayloadRemovedAt = now;
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new(rows.Count, rows.Count, false);
    }
}
