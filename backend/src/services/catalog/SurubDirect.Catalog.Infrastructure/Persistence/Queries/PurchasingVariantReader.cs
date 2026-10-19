using Microsoft.EntityFrameworkCore;
using SurubDirect.Catalog.Contracts;
using SurubDirect.Catalog.Domain;
namespace SurubDirect.Catalog.Infrastructure;
internal sealed class PurchasingVariantReader(CatalogDbContext db) : IPurchasingVariantReader
{
    public Task<PurchasingVariantDto?> Find(Guid id, CancellationToken ct) =>
        (from v in db.Variants.AsNoTracking()
         join p in db.Products.AsNoTracking() on v.ProductId equals p.Id
         where v.Id == id && p.Status != ProductStatus.Archived
         select new PurchasingVariantDto(v.Id, v.Sku, v.Label, p.Name)).SingleOrDefaultAsync(ct);

    public async Task<IReadOnlyList<PurchasingVariantDto>> Search(string search, CancellationToken ct)
    {
        var term = search.ToUpperInvariant();
        return await (from v in db.Variants.AsNoTracking()
                      join p in db.Products.AsNoTracking() on v.ProductId equals p.Id
                      where p.Status != ProductStatus.Archived &&
                          (v.Sku.ToUpper().Contains(term) || p.Name.ToUpper().Contains(term) || v.Label.ToUpper().Contains(term))
                      orderby v.Sku, v.Id
                      select new PurchasingVariantDto(v.Id, v.Sku, v.Label, p.Name)).Take(25).ToListAsync(ct);
    }
}
