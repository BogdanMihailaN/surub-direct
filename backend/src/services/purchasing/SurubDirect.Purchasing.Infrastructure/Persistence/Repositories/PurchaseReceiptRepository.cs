using Microsoft.EntityFrameworkCore;
using Npgsql;
using SurubDirect.BuildingBlocks;
using SurubDirect.Purchasing.Application;
using SurubDirect.Purchasing.Domain;
namespace SurubDirect.Purchasing.Infrastructure;

internal sealed class PurchaseReceiptRepository(PurchasingDbContext db) : IPurchaseReceiptRepository
{
    private async Task<PurchaseOrder> Lock(Guid id, CancellationToken ct)
    {
        // Same supplier -> order lock order as other Purchasing mutations.
        var supplierId = await db.PurchaseOrders.AsNoTracking().Where(o => o.Id == id).Select(o => (Guid?)o.SupplierId).SingleOrDefaultAsync(ct)
            ?? throw new BusinessException("Comanda nu există.", 404);
        await db.Suppliers.FromSqlInterpolated($"SELECT * FROM purchasing.\"Suppliers\" WHERE \"Id\" = {supplierId} FOR UPDATE").SingleAsync(ct);
        var order = await db.PurchaseOrders.FromSqlInterpolated($"SELECT * FROM purchasing.\"PurchaseOrders\" WHERE \"Id\" = {id} FOR UPDATE").SingleAsync(ct);
        await db.Entry(order).Collection(o => o.Lines).LoadAsync(ct);
        await db.Entry(order).Collection(o => o.Receipts).LoadAsync(ct);
        await db.Entry(order).Collection(o => o.Corrections).LoadAsync(ct);
        return order;
    }
    public async Task<PurchaseReceipt> Reserve(Guid orderId, ReceivePurchaseOrder c, string actor, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var order = await Lock(orderId, ct);
            var old = await db.PurchaseReceipts.AsNoTracking().SingleOrDefaultAsync(r => r.Id == c.RequestId, ct);
            if (old is not null) { old.EnsureSame(orderId, c.LineId, c.Quantity, c.Reference); await tx.CommitAsync(ct); return old; }
            order.EnsureVersion(c.Version);
            var receipt = order.Receive(c.RequestId, c.LineId, c.Quantity, c.Reference, actor);
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return receipt;
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: "23505" })
        { db.ChangeTracker.Clear(); throw new BusinessException("Recepția există deja. Reîncarcă datele.", 409); }
        catch { db.ChangeTracker.Clear(); throw; }
    }
    public async Task<PurchaseReceipt> Get(Guid orderId, Guid receiptId, CancellationToken ct) =>
        await db.PurchaseReceipts.AsNoTracking().SingleOrDefaultAsync(r => r.Id == receiptId && r.OrderId == orderId, ct)
            ?? throw new BusinessException("Recepția nu există în această comandă.", 404);
    public async Task Complete(Guid orderId, Guid receiptId, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try { var order = await Lock(orderId, ct); order.CompleteReceipt(receiptId); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); }
        catch { db.ChangeTracker.Clear(); throw; }
    }
}
