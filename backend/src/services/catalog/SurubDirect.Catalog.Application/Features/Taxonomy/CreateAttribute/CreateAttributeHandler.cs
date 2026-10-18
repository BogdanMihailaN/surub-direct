using SurubDirect.BuildingBlocks;
using SurubDirect.Catalog.Domain;

namespace SurubDirect.Catalog.Application;
public sealed class CreateAttributeHandler(ITaxonomyRepository repository)
{
    public Task<Guid> Handle(CreateAttribute command, CancellationToken ct) => repository.Change(() =>
    {
        if (command.Kind is not ("Text" or "Number")) throw new BusinessException("Tip de atribut invalid.");
        var attribute = TechnicalAttribute.Create(command.Code, command.Name, Enum.Parse<AttributeKind>(command.Kind), command.Unit, command.SortOrder);
        repository.Add(attribute);
        return Task.FromResult(attribute.Id);
    }, ct);
}
