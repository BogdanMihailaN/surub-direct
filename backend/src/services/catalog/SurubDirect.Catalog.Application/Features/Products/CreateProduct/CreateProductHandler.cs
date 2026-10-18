using SurubDirect.Catalog.Domain;

namespace SurubDirect.Catalog.Application;
public sealed class CreateProductHandler(ICatalogRepository repository, ITaxonomyRepository taxonomy)
{
    public async Task<Guid> Handle(CreateProduct command, CancellationToken ct)
    {
        var category = await taxonomy.ResolveCategory(command.CategoryId, command.Category, ct);
        var product = Product.Create(command.Name, command.Slug, category.Name, command.Description);
        product.AssignCategory(category);
        await repository.Add(product, ct);
        await repository.Save(ct);
        return product.Id;
    }
}
