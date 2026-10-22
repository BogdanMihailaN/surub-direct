using System.Data;
using Microsoft.EntityFrameworkCore;
using SurubDirect.Inventory.Application;
using SurubDirect.Inventory.Contracts;
using SurubDirect.Inventory.Domain;

namespace SurubDirect.Inventory.Infrastructure;
internal sealed class StockLedger(InventoryDbContext db) : IStockLedger
{
    public async Task<bool> Adjust(StockAdjustment a, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var inserted = await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO inventory.\"Adjustments\" (\"Id\",\"VariantId\",\"Quantity\",\"ExpectedOnHand\",\"ExpectedReserved\",\"Reference\",\"Reason\",\"Actor\",\"CreatedAt\") VALUES ({a.Id},{a.VariantId},{a.Quantity},{a.ExpectedOnHand},{a.ExpectedReserved},{a.Reference},{a.Reason},{a.Actor},{a.CreatedAt}) ON CONFLICT (\"Id\") DO NOTHING", ct);
        if (inserted == 0)
        {
            (await db.Adjustments.AsNoTracking().SingleAsync(x => x.Id == a.Id, ct)).EnsureSame(a);
            await tx.CommitAsync(ct);
            return false;
        }

        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO inventory.\"Balances\" (\"VariantId\",\"OnHand\",\"Reserved\") VALUES ({a.VariantId},0,0) ON CONFLICT (\"VariantId\") DO NOTHING", ct);
        var balance = await db.Balances.FromSqlInterpolated($"SELECT * FROM inventory.\"Balances\" WHERE \"VariantId\" = {a.VariantId} FOR UPDATE").AsNoTracking().SingleAsync(ct);
        var result = a.Apply(balance.OnHand, balance.Reserved);
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE inventory.\"Balances\" SET \"OnHand\" = {result} WHERE \"VariantId\" = {a.VariantId}", ct);
        await tx.CommitAsync(ct);
        return true;
    }

    public async Task<StockLedgerDto> Read(Guid variant, int page, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var receipts = db.Receipts.Where(x => x.VariantId == variant).Select(x => new { x.Id, Kind = "Receipt", x.Quantity, x.Reference, Reason = "", x.Actor, x.CreatedAt, OriginalReceiptId = (Guid? )null });
        var adjustments = db.Adjustments.Where(x => x.VariantId == variant).Select(x => new { x.Id, Kind = "Adjustment", x.Quantity, x.Reference, x.Reason, x.Actor, x.CreatedAt, OriginalReceiptId = (Guid? )null });
        var corrections = db.ReceiptCorrections.Where(x => x.VariantId == variant).Select(x => new { x.Id, Kind = "Correction", Quantity = -x.Quantity, x.Reference, x.Reason, x.Actor, x.CreatedAt, OriginalReceiptId = (Guid? )x.ReceiptId });
        var consumptions = db.Consumptions.Where(x => x.VariantId == variant).Select(x => new { Id = x.ReservationId, Kind = "Consumption", Quantity = -x.Quantity, Reference = x.OrderId.ToString(), Reason = "Consum rezervare", Actor = "Ordering", x.CreatedAt, OriginalReceiptId = (Guid? )null });
        var restorations = db.Restorations.Where(x => x.VariantId == variant).Select(x => new { Id = x.ReservationId, Kind = "Restoration", Quantity = x.Quantity, Reference = x.OrderId.ToString(), Reason = "Restituire comandă anulată", Actor = "Ordering", x.CreatedAt, OriginalReceiptId = (Guid? )null });
        var query = receipts.Concat(adjustments).Concat(corrections).Concat(consumptions).Concat(restorations);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Kind).ThenBy(x => x.Id).Skip((page - 1) * 25).Take(25).ToListAsync(ct);
        var balance = await db.Balances.AsNoTracking().SingleOrDefaultAsync(x => x.VariantId == variant, ct);
        await tx.CommitAsync(ct);
        return new(balance?.OnHand ?? 0, balance?.Reserved ?? 0, items.Select(x => new StockMovementDto(x.Id, x.Kind, x.Quantity, x.Reference, x.Reason, x.Actor, x.CreatedAt, x.OriginalReceiptId)).ToArray(), total, page, 25);
    }
}
