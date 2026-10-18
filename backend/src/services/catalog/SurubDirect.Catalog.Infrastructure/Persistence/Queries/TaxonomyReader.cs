using Microsoft.EntityFrameworkCore;
using SurubDirect.Catalog.Contracts;
using SurubDirect.Catalog.Domain;

namespace SurubDirect.Catalog.Infrastructure;
internal sealed class TaxonomyReader(CatalogDbContext db) : ITaxonomyReader
{
    public async Task<TaxonomyDto> Get(CancellationToken ct, bool includeEmpty = false)
    {
        var taxonomy = await TaxonomyProjection.Load(db, ct);
        var visible = includeEmpty ? taxonomy.Categories.Select(x => x.Id).ToHashSet()
            : (await db.Products.Where(x => x.Status == ProductStatus.Published).Select(x => x.CategoryId).Distinct().ToListAsync(ct)).ToHashSet();
        foreach (var id in visible.ToArray())
        {
            var parent = taxonomy.Categories.Single(x => x.Id == id).ParentId;
            while (parent.HasValue) { visible.Add(parent.Value); parent = taxonomy.Categories.Single(x => x.Id == parent.Value).ParentId; }
        }
        return new(taxonomy.Categories.Where(x => visible.Contains(x.Id)).OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new CategoryDto(x.Id, x.Name, x.Slug, x.ParentId, x.SortOrder, x.Attributes.Select(a => a.AttributeId).ToArray())).ToArray(),
            taxonomy.Attributes.Select(x => new TechnicalAttributeDto(x.Id, x.Code, x.Name, x.Kind.ToString(), x.Source.ToString(), x.Unit, x.SortOrder)).ToArray());
    }
}
