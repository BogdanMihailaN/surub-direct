using Microsoft.EntityFrameworkCore;
using SurubDirect.BuildingBlocks;
using SurubDirect.Catalog.Application;
using SurubDirect.Catalog.Domain;
namespace SurubDirect.Catalog.Infrastructure;
internal sealed class ProductImageRepository(CatalogDbContext db) : IProductImageRepository
{
    public async Task<T> Change<T>(Guid productId, Func<Product, Task<T>> operation, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            // The parent row lock serializes uploads, ordering and deletion, including an empty gallery.
            var product = await db.Products.FromSqlInterpolated($"SELECT * FROM catalog.\"Products\" WHERE \"Id\" = {productId} FOR UPDATE")
                .SingleOrDefaultAsync(ct) ?? throw new BusinessException("Produsul nu există.", 404);
            await db.Entry(product).Collection(x => x.Images).LoadAsync(ct);
            await db.Entry(product).Collection(x => x.Variants).LoadAsync(ct);
            var result = await operation(product);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return result;
        }
        catch { db.ChangeTracker.Clear(); throw; }
    }
    public Task<bool> Exists(Guid productId, Guid imageId, bool includeDrafts, CancellationToken ct) =>
        db.Products.AnyAsync(p => p.Id == productId && (includeDrafts || p.Status == ProductStatus.Published) && p.Images.Any(i => i.Id == imageId), ct);
}
