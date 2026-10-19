using Microsoft.EntityFrameworkCore;
using SurubDirect.Catalog.Contracts;
using SurubDirect.Catalog.Domain;

namespace SurubDirect.Catalog.Infrastructure;
internal sealed class CatalogReader(CatalogDbContext db) : ICatalogReader
{
    private IQueryable<Product> Query => db.Products.Include(x => x.Classification)
        .Include(x => x.Variants).ThenInclude(x => x.Offers)
        .Include(x => x.Variants).ThenInclude(x => x.AttributeValues).AsSplitQuery().AsNoTracking();

    public async Task<CatalogPage> List(CatalogFilter f, bool drafts, CancellationToken ct)
    {
        var taxonomy = await TaxonomyProjection.Load(db, ct);
        var q = Query.Where(x => drafts || x.Status == ProductStatus.Published);
        if (f.Status is not null) { var status = Enum.Parse<ProductStatus>(f.Status); q = q.Where(x => x.Status == status); }
        var categoryId = f.CategoryId;
        if (categoryId is null && !string.IsNullOrWhiteSpace(f.Category))
            categoryId = taxonomy.Categories.SingleOrDefault(x => x.Name == f.Category)?.Id ?? Guid.Empty;
        if (categoryId is { } id) { var ids = taxonomy.Descendants(id); q = q.Where(x => ids.Contains(x.CategoryId)); }
        var allowed = categoryId.HasValue && taxonomy.Categories.Any(x => x.Id == categoryId)
            ? taxonomy.Effective(categoryId.Value) : taxonomy.Attributes.Where(x => x.Source != AttributeSource.Custom).ToArray();
        var variants = VariantAttributeFilters.Apply(db.Variants.AsQueryable(), f.Attributes, allowed);
        variants = variants.Where(v => (f.Diameter == null || v.DiameterMm == f.Diameter) && (f.Length == null || v.LengthMm == f.Length));
        var search = string.IsNullOrWhiteSpace(f.Search) ? null : Product.Normalize(f.Search);
        var raw = f.Search?.Trim().ToLowerInvariant() ?? "";
        var hasVariantFilters = f.Diameter != null || f.Length != null || f.Attributes?.Count > 0;
        if (search != null || hasVariantFilters)
        {
            var matchingIds = variants.Select(v => v.Id);
            q = q.Where(p => (!hasVariantFilters && search != null && (p.Name.ToLower().Contains(raw) || p.Slug.Replace("-", "").Contains(search)))
                || p.Variants.Any(v => matchingIds.Contains(v.Id) && (search == null || v.SearchText.Contains(search) || p.Name.ToLower().Contains(raw) || p.Slug.Replace("-", "").Contains(search))));
        }
        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(x => x.Name).ThenBy(x => x.Id).Skip((f.Page - 1) * f.PageSize).Take(f.PageSize).ToListAsync(ct);
        return new(items.Select(taxonomy.Map).ToArray(), total, f.Page, f.PageSize);
    }
    public async Task<ProductDto?> Get(Guid id, bool drafts, CancellationToken ct)
    {
        var p = await Query.SingleOrDefaultAsync(x => x.Id == id && (drafts || x.Status == ProductStatus.Published), ct);
        return p == null ? null : (await TaxonomyProjection.Load(db, ct)).Map(p);
    }
    public async Task<ProductDto?> GetBySlug(string slug, CancellationToken ct)
    {
        var p = await Query.SingleOrDefaultAsync(x => x.Slug == slug && x.Status == ProductStatus.Published, ct);
        return p == null ? null : (await TaxonomyProjection.Load(db, ct)).Map(p);
    }
    public async Task<IReadOnlyList<string>> Categories(CancellationToken ct) => await db.Products.Where(x => x.Status == ProductStatus.Published).Select(x => x.Classification!.Name).Distinct().OrderBy(x => x).ToListAsync(ct);
    public Task<bool> VariantExists(Guid id, CancellationToken ct) => db.Variants.AnyAsync(x => x.Id == id, ct);
    public Task<bool> OfferExists(Guid id, CancellationToken ct) => db.Offers.AnyAsync(x => x.Id == id, ct);
}
