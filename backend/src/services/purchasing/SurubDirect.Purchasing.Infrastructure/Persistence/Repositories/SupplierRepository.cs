using Microsoft.EntityFrameworkCore;
using SurubDirect.BuildingBlocks;
using SurubDirect.Purchasing.Application;
using SurubDirect.Purchasing.Contracts;
using SurubDirect.Purchasing.Domain;
namespace SurubDirect.Purchasing.Infrastructure;
internal sealed class SupplierRepository(PurchasingDbContext db) : ISupplierRepository
{
    public async Task Add(Supplier supplier, CancellationToken ct) { db.Suppliers.Add(supplier); await db.SaveChangesAsync(ct); }
    public async Task Change(Guid id, Action<Supplier> change, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var supplier = await db.Suppliers.FromSqlInterpolated($"SELECT * FROM purchasing.\"Suppliers\" WHERE \"Id\" = {id} FOR UPDATE").SingleOrDefaultAsync(ct)
                ?? throw new BusinessException("Furnizorul nu există.", 404);
            await db.Entry(supplier).Collection(x => x.Items).LoadAsync(ct);
            change(supplier); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        }
        catch { db.ChangeTracker.Clear(); throw; }
    }
    public async Task<SupplierDto?> Get(Guid id, CancellationToken ct)
    {
        var supplier = await db.Suppliers.AsNoTracking().Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id, ct);
        return supplier is null ? null : SupplierProjection.Map(supplier);
    }
    public async Task<SupplierPage> List(string? search, bool? active, int page, CancellationToken ct)
    {
        var query = db.Suppliers.AsNoTracking();
        if (active.HasValue) query = query.Where(x => x.IsActive == active.Value);
        if (!string.IsNullOrEmpty(search))
        {
            var term = search.ToUpperInvariant();
            query = query.Where(x => x.Name.ToUpper().Contains(term) || x.TaxId.Contains(term) || x.Items.Any(i => i.Sku.ToUpper().Contains(term) || i.SupplierCode.Contains(term)));
        }
        var total = await query.CountAsync(ct);
        var suppliers = await query.OrderBy(x => x.Name).ThenBy(x => x.Id).Skip((page - 1) * 25).Take(25).ToListAsync(ct);
        return new(suppliers.Select(SupplierProjection.Map).ToArray(), total, page, 25);
    }
}
