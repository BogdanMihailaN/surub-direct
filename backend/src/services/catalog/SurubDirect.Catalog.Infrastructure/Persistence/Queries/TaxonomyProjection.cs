using Microsoft.EntityFrameworkCore;
using SurubDirect.Catalog.Contracts;
using SurubDirect.Catalog.Domain;

namespace SurubDirect.Catalog.Infrastructure;
internal sealed class TaxonomyProjection(IReadOnlyList<Category> categories, IReadOnlyList<TechnicalAttribute> attributes)
{
    public IReadOnlyList<Category> Categories { get; } = categories;
    public IReadOnlyList<TechnicalAttribute> Attributes { get; } = attributes;
    public static async Task<TaxonomyProjection> Load(CatalogDbContext db, CancellationToken ct) => new(
        await db.Categories.Include(x => x.Attributes).AsNoTracking().ToListAsync(ct),
        await db.TechnicalAttributes.AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync(ct));
    public IReadOnlyList<TechnicalAttribute> Effective(Guid categoryId)
    {
        var ids = new HashSet<Guid>();
        var seen = new HashSet<Guid>();
        Guid? current = categoryId;
        while (current is { } id && seen.Add(id))
        {
            var category = Categories.Single(x => x.Id == id);
            ids.UnionWith(category.Attributes.Select(x => x.AttributeId));
            current = category.ParentId;
        }
        return Attributes.Where(x => ids.Contains(x.Id)).ToArray();
    }
    public Guid[] Descendants(Guid categoryId)
    {
        var ids = new HashSet<Guid> { categoryId };
        while (true)
        {
            var added = Categories.Where(x => x.ParentId.HasValue && ids.Contains(x.ParentId.Value)).Select(x => x.Id).Where(x => !ids.Contains(x)).ToArray();
            if (added.Length == 0) return ids.ToArray();
            ids.UnionWith(added);
        }
    }
    public ProductDto Map(Product p) => new(p.Id, p.Name, p.Slug, p.Classification!.Name, p.Description, p.Status.ToString(),
        p.Variants.OrderBy(v => v.Sku).Select(v => new VariantDto(v.Id, v.Sku, v.Label, v.DiameterMm, v.LengthMm, v.Material, v.Finish,
            v.Offers.OrderBy(o => o.UnitsPerPack).Select(o => new OfferDto(o.Id, o.Code, o.Label, o.UnitsPerPack, o.MinPacks, o.PackStep)).ToArray(),
            Effective(p.CategoryId).Select(a => Value(v, a)).Where(x => x.TextValue is not null || x.NumberValue is not null).ToArray())).ToArray(), p.CategoryId);
    private static TechnicalValueDto Value(Variant v, TechnicalAttribute a)
    {
        var stored = v.AttributeValues.SingleOrDefault(x => x.AttributeId == a.Id);
        var text = a.Source switch { AttributeSource.Material => v.Material, AttributeSource.Finish => v.Finish, _ => stored?.TextValue };
        var number = a.Source switch { AttributeSource.Diameter => v.DiameterMm, AttributeSource.Length => v.LengthMm, _ => stored?.NumberValue };
        return new(a.Id, a.Code, a.Name, a.Kind.ToString(), a.Unit, text, number, a.Source.ToString());
    }
}
