using SurubDirect.BuildingBlocks;

namespace SurubDirect.Catalog.Application;
public sealed class UpdateProductHandler(ICatalogRepository repository, ITaxonomyRepository taxonomy)
{
    public async Task Handle(Guid id, UpdateProduct command, CancellationToken ct)
    {
        var product = await repository.Get(id, ct) ?? throw new BusinessException("Produsul nu există.", 404);
        product.EnsureEditable();
        var category = await taxonomy.ResolveCategory(command.CategoryId, command.Category, ct);
        product.UpdateDetails(command.Name, category.Name, command.Description);
        product.AssignCategory(category);
        await repository.Save(ct);
    }
}
