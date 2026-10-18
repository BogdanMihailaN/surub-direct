namespace SurubDirect.Catalog.Contracts;
public record OfferDto(Guid Id, string Code, string Label, int UnitsPerPack, int MinPacks, int PackStep);
