using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.Catalog.Application;
using SurubDirect.Catalog.Domain;

namespace SurubDirect.Catalog.Infrastructure;
internal sealed class CatalogRepository(CatalogDbContext db) : ICatalogRepository
{
    private IQueryable<Product> Query => db.Products.Include(x => x.Classification).Include(x => x.Variants).ThenInclude(x => x.AttributeValues).Include(x => x.Variants).ThenInclude(x => x.Offers).AsSplitQuery();

    public Task<Product?> Get(Guid id, CancellationToken ct) => Query.SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task Add(Product p, CancellationToken ct) => await db.Products.AddAsync(p, ct);
    public async Task Save(CancellationToken ct) => await db.SaveChangesAsync(ct);
    public void RecordPublication(Product p) => db.Outbox.Add(new() { ProductId = p.Id });
}
