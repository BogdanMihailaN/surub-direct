using Microsoft.EntityFrameworkCore;
using SurubDirect.BuildingBlocks;
using SurubDirect.Catalog.Application;
using SurubDirect.Catalog.Domain;

namespace SurubDirect.Catalog.Infrastructure;
internal sealed class TaxonomyRepository(CatalogDbContext db) : ITaxonomyRepository
{
    public Task<List<Category>> Categories(CancellationToken ct) => db.Categories.Include(x => x.Attributes).ToListAsync(ct);
    public Task<List<TechnicalAttribute>> Attributes(CancellationToken ct) => db.TechnicalAttributes.ToListAsync(ct);
    public void Add(Category category) => db.Categories.Add(category);
    public void Add(TechnicalAttribute attribute) => db.TechnicalAttributes.Add(attribute);
    public async Task<Category> ResolveCategory(Guid? id, string legacyName, CancellationToken ct)
    {
        if (id is not null)
            return await db.Categories.FindAsync([id.Value], ct) ?? throw new BusinessException("Categoria nu există.", 404);
        // Compatibility with existing imports, bootstrap and clients that still send a category name.
        var name = Guard.Text(legacyName, "Categorie", 80);
        var normalized = name.ToUpperInvariant();
        var existing = db.Categories.Local.FirstOrDefault(x => x.NormalizedName == normalized)
            ?? await db.Categories.SingleOrDefaultAsync(x => x.NormalizedName == normalized, ct);
        if (existing is not null) return existing;
        var category = Category.Create(name, "categorie-" + Guid.NewGuid().ToString("N"), null, 0);
        category.SetAttributes(await db.TechnicalAttributes.Where(x => x.Source != AttributeSource.Custom).Select(x => x.Id).ToListAsync(ct));
        Add(category);
        return category;
    }
    public async Task<T> Change<T>(Func<Task<T>> operation, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            // Serialize hierarchy and assignment changes so concurrent moves cannot introduce a cycle.
            await db.Database.ExecuteSqlRawAsync("LOCK TABLE catalog.\"Categories\" IN SHARE ROW EXCLUSIVE MODE", ct);
            var result = await operation();
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return result;
        }
        catch
        {
            db.ChangeTracker.Clear();
            throw;
        }
    }
}
