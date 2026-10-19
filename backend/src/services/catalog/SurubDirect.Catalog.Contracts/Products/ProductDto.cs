namespace SurubDirect.Catalog.Contracts;
public record ProductDto(Guid Id, string Name, string Slug, string Category, string Description, string Status, IReadOnlyList<VariantDto> Variants, Guid CategoryId = default, IReadOnlyList<ProductImageDto>? Images = null);
