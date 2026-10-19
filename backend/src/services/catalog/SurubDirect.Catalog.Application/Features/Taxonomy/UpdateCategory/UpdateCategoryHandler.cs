using SurubDirect.BuildingBlocks;
using SurubDirect.Catalog.Domain;

namespace SurubDirect.Catalog.Application;
public sealed class UpdateCategoryHandler(ITaxonomyRepository repository)
{
    public Task<Guid> Handle(Guid id, UpdateCategory command, CancellationToken ct) => repository.Change(async () =>
    {
        var categories = await repository.Categories(ct);
        var attributes = await repository.Attributes(ct);
        if (command.AttributeIds is null || command.AttributeIds.Count > 50 || command.AttributeIds.Any(id => attributes.All(a => a.Id != id)))
            throw new BusinessException("Lista atributelor categoriei nu este validă.");
        var category = categories.SingleOrDefault(x => x.Id == id) ?? throw new BusinessException("Categoria nu există.", 404);
        category.Update(command.Name, command.ParentId, command.SortOrder);
        category.SetAttributes(command.AttributeIds);
        CategoryHierarchy.Validate(categories);
        return category.Id;
    }, ct);
}
