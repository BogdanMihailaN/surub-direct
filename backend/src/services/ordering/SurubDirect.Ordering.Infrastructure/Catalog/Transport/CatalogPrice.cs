namespace SurubDirect.Ordering.Infrastructure;

internal sealed record CatalogPrice(Guid OfferId, decimal NetAmount, decimal GrossAmount, string Currency);
