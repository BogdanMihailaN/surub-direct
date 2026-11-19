using Microsoft.EntityFrameworkCore;
using SurubDirect.BuildingBlocks;
using SurubDirect.Catalog.Application;
using SurubDirect.Catalog.Domain;

namespace SurubDirect.Catalog.Infrastructure;
internal sealed class CatalogRepository(CatalogDbContext db) : ICatalogRepository
{
    private IQueryable<Product> Query => db.Products.Include(x => x.Images).Include(x => x.Classification).Include(x => x.Variants).ThenInclude(x => x.AttributeValues).Include(x => x.Variants).ThenInclude(x => x.Offers).AsSplitQuery();

    public Task<Product?> Get(Guid id, CancellationToken ct) => Query.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<Guid?> FindIdBySlug(string slug, CancellationToken ct) => db.Products.AsNoTracking()
        .Where(x => x.Slug == slug).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);

    public async Task<T> Change<T>(Guid id, Func<Product, Task<T>> action, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Read the aggregate after acquiring the parent lock, including its collections.
        var locked = await db.Database.SqlQuery<Guid>(
            $"""SELECT "Id" AS "Value" FROM catalog."Products" WHERE "Id" = {id} FOR UPDATE""").ToListAsync(ct);
        if (locked.Count == 0)
            throw new BusinessException("Produsul nu există.", 404);
        db.ChangeTracker.Clear();
        var product = await Query.SingleAsync(x => x.Id == id, ct);
        var result = await action(product);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return result;
    }

    public async Task Add(Product p, CancellationToken ct) => await db.Products.AddAsync(p, ct);
    public async Task Save(CancellationToken ct) => await db.SaveChangesAsync(ct);
    public void RecordPublication(Product p) => db.Outbox.Add(new() { ProductId = p.Id });
}
