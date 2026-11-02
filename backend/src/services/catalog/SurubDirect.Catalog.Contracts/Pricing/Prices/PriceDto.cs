namespace SurubDirect.Catalog.Contracts.Pricing;
public record PriceDto(Guid OfferId, decimal NetAmount, decimal TaxRate, decimal GrossAmount, string Currency);
