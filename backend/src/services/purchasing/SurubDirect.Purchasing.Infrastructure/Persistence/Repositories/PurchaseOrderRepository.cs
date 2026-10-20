using Microsoft.EntityFrameworkCore;
using SurubDirect.BuildingBlocks;
using SurubDirect.Purchasing.Application;
using SurubDirect.Purchasing.Contracts;
using SurubDirect.Purchasing.Domain;
namespace SurubDirect.Purchasing.Infrastructure;

internal sealed class PurchaseOrderRepository(PurchasingDbContext db) : IPurchaseOrderRepository
{
    private async Task<Supplier> LockSupplier(Guid id, CancellationToken ct)
    {
        var supplier = await db.Suppliers.FromSqlInterpolated($"SELECT * FROM purchasing.\"Suppliers\" WHERE \"Id\" = {id} FOR UPDATE").SingleOrDefaultAsync(ct)
            ?? throw new BusinessException("Furnizorul nu există.", 404);
        await db.Entry(supplier).Collection(x => x.Items).LoadAsync(ct); return supplier;
    }
    public async Task<Guid> Create(Guid supplierId, string currency, string? notes, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var supplier = await LockSupplier(supplierId, ct); var order = PurchaseOrder.Create(supplier, currency, notes);
            db.PurchaseOrders.Add(order); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return order.Id;
        }
        catch { db.ChangeTracker.Clear(); throw; }
    }
    public async Task Change(Guid id, int version, Func<PurchaseOrder, Supplier, Task> change, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var supplierId = await db.PurchaseOrders.AsNoTracking().Where(x => x.Id == id).Select(x => (Guid?)x.SupplierId).SingleOrDefaultAsync(ct)
                ?? throw new BusinessException("Comanda nu există.", 404);
            // Consistent lock order with supplier administration: supplier first, then order.
            var supplier = await LockSupplier(supplierId, ct);
            var order = await db.PurchaseOrders.FromSqlInterpolated($"SELECT * FROM purchasing.\"PurchaseOrders\" WHERE \"Id\" = {id} FOR UPDATE").SingleAsync(ct);
            await db.Entry(order).Collection(x => x.Lines).LoadAsync(ct);
            await db.Entry(order).Collection(x => x.Receipts).LoadAsync(ct);
            await db.Entry(order).Collection(x => x.Corrections).LoadAsync(ct);
            order.EnsureVersion(version); await change(order, supplier);
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        }
        catch { db.ChangeTracker.Clear(); throw; }
    }
    public async Task<PurchaseOrderDto?> Get(Guid id, CancellationToken ct)
    {
        var order = await db.PurchaseOrders.AsNoTracking().Include(x => x.Lines).Include(x => x.Receipts).Include(x => x.Corrections).AsSplitQuery().SingleOrDefaultAsync(x => x.Id == id, ct);
        return order is null ? null : PurchaseOrderProjection.Map(order);
    }
    public async Task<PurchaseOrderPage> List(string? search, PurchaseOrderStatus? status, int page, CancellationToken ct)
    {
        var query = db.PurchaseOrders.AsNoTracking();
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (!string.IsNullOrEmpty(search))
        {
            var term = search.ToUpperInvariant(); var digits = term.StartsWith("CA-") ? term[3..] : term; long.TryParse(digits, out var number);
            query = query.Where(x => x.Number == number || x.SupplierName.ToUpper().Contains(term) || x.SupplierTaxId.Contains(term) || x.Lines.Any(l => l.Sku.ToUpper().Contains(term)));
        }
        var total = await query.CountAsync(ct);
        var orders = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Skip((page - 1) * 25).Take(25).Include(x => x.Lines).Include(x => x.Receipts).Include(x => x.Corrections).AsSplitQuery().ToListAsync(ct);
        return new(orders.Select(PurchaseOrderProjection.Map).ToArray(), total, page, 25);
    }
}
