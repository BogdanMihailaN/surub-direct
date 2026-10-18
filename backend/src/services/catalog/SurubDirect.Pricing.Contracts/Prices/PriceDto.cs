namespace SurubDirect.Pricing.Contracts;
public record PriceDto(Guid OfferId, decimal NetAmount, decimal TaxRate, decimal GrossAmount, string Currency);
