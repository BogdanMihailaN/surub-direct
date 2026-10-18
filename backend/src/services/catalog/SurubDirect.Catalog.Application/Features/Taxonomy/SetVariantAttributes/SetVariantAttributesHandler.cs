using SurubDirect.BuildingBlocks;
using SurubDirect.Catalog.Domain;

namespace SurubDirect.Catalog.Application;
public sealed class SetVariantAttributesHandler(ICatalogRepository products, ITaxonomyRepository taxonomy)
{
    public Task<Guid> Handle(Guid productId, Guid variantId, IReadOnlyList<SetVariantAttributeValue> command, CancellationToken ct) => taxonomy.Change(async () =>
    {
        if (command is null || command.Count > 50) throw new BusinessException("Maximum 50 de atribute per variantă.");
        var product = await products.Get(productId, ct) ?? throw new BusinessException("Produsul nu există.", 404);
        product.EnsureEditable();
        var variant = product.Variants.SingleOrDefault(x => x.Id == variantId) ?? throw new BusinessException("Varianta nu există în acest produs.", 404);
        var categories = await taxonomy.Categories(ct);
        var ids = new HashSet<Guid>();
        Guid? categoryId = product.CategoryId;
        while (categoryId is { } id)
        {
            var category = categories.Single(x => x.Id == id);
            ids.UnionWith(category.Attributes.Select(x => x.AttributeId));
            categoryId = category.ParentId;
        }
        var definitions = (await taxonomy.Attributes(ct)).Where(x => ids.Contains(x.Id) && x.Source == AttributeSource.Custom).ToDictionary(x => x.Id);
        var values = command.Select(value =>
        {
            if (!definitions.TryGetValue(value.AttributeId, out var definition)) throw new BusinessException("Atributul nu este configurat pentru categoria produsului.");
            return VariantAttributeValue.Create(variantId, definition, value.TextValue, value.NumberValue);
        }).ToArray();
        variant.SetAttributes(definitions.Keys.ToArray(), values);
        return variant.Id;
    }, ct);
}
