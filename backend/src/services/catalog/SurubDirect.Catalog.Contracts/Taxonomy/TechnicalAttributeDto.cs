namespace SurubDirect.Catalog.Contracts;
public record TechnicalAttributeDto(Guid Id, string Code, string Name, string Kind, string Source, string? Unit, int SortOrder);
