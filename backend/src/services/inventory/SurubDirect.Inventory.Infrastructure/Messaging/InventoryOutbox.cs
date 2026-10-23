using System.Text;
using Microsoft.EntityFrameworkCore;

namespace SurubDirect.Inventory.Infrastructure;
public sealed class InventoryOutbox(InventoryDbContext db)
{
    public async Task<int> Process(Func<Guid, byte[], CancellationToken, Task> publish, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var row = await db.ReservationOutbox.FromSqlRaw("""SELECT * FROM inventory."ReservationOutbox" WHERE "PublishedAt" IS NULL ORDER BY "OccurredAt","Id" LIMIT 1 FOR UPDATE SKIP LOCKED""").SingleOrDefaultAsync(ct);
        if (row is null)
            return 0;
        await publish(row.Id, Encoding.UTF8.GetBytes(row.Payload), ct);
        row.PublishedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return 1;
    }
}
