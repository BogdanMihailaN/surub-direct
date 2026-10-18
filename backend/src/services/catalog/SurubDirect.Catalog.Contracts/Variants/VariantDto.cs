namespace SurubDirect.Catalog.Contracts;
public record VariantDto(Guid Id, string Sku, string Label, decimal? DiameterMm, decimal? LengthMm, string Material, string Finish, IReadOnlyList<OfferDto> Offers, IReadOnlyList<TechnicalValueDto>? Attributes = null);
