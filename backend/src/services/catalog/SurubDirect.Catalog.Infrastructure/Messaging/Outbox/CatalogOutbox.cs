using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.Catalog.Application;

namespace SurubDirect.Catalog.Infrastructure;
internal sealed class CatalogOutbox(CatalogDbContext db) : ICatalogOutbox
{
    public async Task<int> ProcessBatch(CancellationToken ct)
    {
        // Local audit projection: lock + projection + completion share one DB transaction.
        // External effects added later require per-consumer deduplication and provider idempotency.
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var rows = await db.Outbox.FromSqlRaw("SELECT * FROM catalog.\"Outbox\" WHERE \"ProcessedAt\" IS NULL ORDER BY \"OccurredAt\" LIMIT 25 FOR UPDATE SKIP LOCKED").ToListAsync(ct);
        foreach (var row in rows)
        {
            if (!await db.Activities.AnyAsync(a => a.EventId == row.Id, ct))
                db.Activities.Add(new() { EventId = row.Id, ProductId = row.ProductId, RecordedAt = DateTimeOffset.UtcNow });
            row.ProcessedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return rows.Count;
    }
}
