using Microsoft.EntityFrameworkCore;
using SurubDirect.BuildingBlocks;
using SurubDirect.Inventory.Application;
using SurubDirect.Inventory.Contracts;
using SurubDirect.Inventory.Domain;

namespace SurubDirect.Inventory.Infrastructure;
internal sealed class InventoryRepository(InventoryDbContext db) : IInventoryRepository, IInventoryReader
{
    public async Task<bool> Receive(StockReceipt r, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // ON CONFLICT waits for an in-flight transaction using the same request id.
        var added = await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO inventory.\"Receipts\" (\"Id\",\"VariantId\",\"Quantity\",\"Reference\",\"Actor\",\"CreatedAt\") VALUES ({r.Id},{r.VariantId},{r.Quantity},{r.Reference},{r.Actor},{r.CreatedAt}) ON CONFLICT (\"Id\") DO NOTHING", ct);
        if (added == 0)
        {
            var old = await db.Receipts.AsNoTracking().SingleAsync(x => x.Id == r.Id, ct);
            if (old.VariantId != r.VariantId || old.Quantity != r.Quantity || old.Reference != r.Reference)
                throw new BusinessException("Cheia a fost folosită pentru o altă recepție.", 409);
            await tx.CommitAsync(ct);
            return false;
        }

        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO inventory.\"Balances\" (\"VariantId\",\"OnHand\",\"Reserved\") VALUES ({r.VariantId},{r.Quantity},0) ON CONFLICT (\"VariantId\") DO UPDATE SET \"OnHand\" = inventory.\"Balances\".\"OnHand\" + EXCLUDED.\"OnHand\"", ct);
        await tx.CommitAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<StockDto>> GetStock(IReadOnlyList<Guid> ids, CancellationToken ct) => await db.Balances.AsNoTracking().Where(x => ids.Contains(x.VariantId)).Select(x => new StockDto(x.VariantId, x.OnHand, x.Reserved, x.OnHand - x.Reserved)).ToListAsync(ct);
}
