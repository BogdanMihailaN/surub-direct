namespace SurubDirect.Catalog.Contracts;
public record ProductImageDto(Guid Id, string AltText, Guid? VariantId, int Width, int Height, int SortOrder, bool IsPrimary);
