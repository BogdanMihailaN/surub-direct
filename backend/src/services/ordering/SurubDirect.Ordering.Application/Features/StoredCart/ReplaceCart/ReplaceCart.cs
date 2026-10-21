using SurubDirect.Ordering.Domain;

namespace SurubDirect.Ordering.Application;
public sealed record ReplaceCart(Guid CartId, int Version, IReadOnlyList<CartSelection>? Lines);
