namespace SurubDirect.Catalog.Contracts;
public record TechnicalValueDto(Guid AttributeId, string Code, string Name, string Kind, string? Unit, string? TextValue, decimal? NumberValue, string Source);
