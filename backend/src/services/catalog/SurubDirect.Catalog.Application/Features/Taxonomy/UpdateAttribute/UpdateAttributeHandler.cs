using SurubDirect.BuildingBlocks;

namespace SurubDirect.Catalog.Application;
public sealed class UpdateAttributeHandler(ITaxonomyRepository repository)
{
    public Task<Guid> Handle(Guid id, UpdateAttribute command, CancellationToken ct) => repository.Change(async () =>
    {
        var attribute = (await repository.Attributes(ct)).SingleOrDefault(x => x.Id == id) ?? throw new BusinessException("Atributul nu există.", 404);
        attribute.Update(command.Name, command.SortOrder);
        return attribute.Id;
    }, ct);
}
