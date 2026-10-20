using SurubDirect.Ordering.Domain;
namespace SurubDirect.Ordering.Application;

public sealed record QuoteCart(IReadOnlyList<CartSelection>? Lines);
