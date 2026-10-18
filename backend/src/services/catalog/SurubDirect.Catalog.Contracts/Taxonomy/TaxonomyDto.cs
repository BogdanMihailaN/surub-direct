namespace SurubDirect.Catalog.Contracts;
public record TaxonomyDto(IReadOnlyList<CategoryDto> Categories, IReadOnlyList<TechnicalAttributeDto> Attributes);
