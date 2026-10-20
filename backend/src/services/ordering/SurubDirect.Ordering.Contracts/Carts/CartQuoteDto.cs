namespace SurubDirect.Ordering.Contracts;

public sealed record CartQuoteDto(IReadOnlyList<CartQuoteLineDto> Lines, string? Currency, decimal? NetTotal, decimal? GrossTotal, DateTimeOffset CheckedAt, bool CheckoutEnabled = false);
