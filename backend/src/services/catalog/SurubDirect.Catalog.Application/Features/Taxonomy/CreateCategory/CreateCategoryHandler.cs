using SurubDirect.BuildingBlocks;
using SurubDirect.Catalog.Domain;

namespace SurubDirect.Catalog.Application;
public sealed class CreateCategoryHandler(ITaxonomyRepository repository)
{
    public Task<Guid> Handle(CreateCategory command, CancellationToken ct) => repository.Change(async () =>
    {
        var categories = await repository.Categories(ct);
        var attributes = await repository.Attributes(ct);
        if (command.AttributeIds is null || command.AttributeIds.Count > 50 || command.AttributeIds.Any(id => attributes.All(a => a.Id != id)))
            throw new BusinessException("Lista atributelor categoriei nu este validă.");
        var category = Category.Create(command.Name, command.Slug, command.ParentId, command.SortOrder);
        categories.Add(category);
        repository.Add(category);
        category.SetAttributes(command.AttributeIds);
        CategoryHierarchy.Validate(categories);
        return category.Id;
    }, ct);
}
