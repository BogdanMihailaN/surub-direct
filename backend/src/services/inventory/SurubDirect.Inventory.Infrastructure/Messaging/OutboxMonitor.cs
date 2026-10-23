using Microsoft.EntityFrameworkCore;
using SurubDirect.Inventory.Application;
using SurubDirect.Inventory.Contracts;

namespace SurubDirect.Inventory.Infrastructure;
internal sealed class OutboxMonitor(InventoryDbContext db) : IOutboxMonitor
{
    public async Task<OutboxStatusDto> Read(CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, ct);
        var pending = db.ReservationOutbox.Where(x => x.PublishedAt == null);
        var result = new OutboxStatusDto(await pending.CountAsync(ct), await pending.MinAsync(x => (DateTimeOffset? )x.OccurredAt, ct), await db.ReservationOutbox.MaxAsync(x => x.PublishedAt, ct));
        await tx.CommitAsync(ct);
        return result;
    }
}
