namespace SurubDirect.Ordering.Application;
public sealed record ImportGuestCart(Guid CartId, int Version, Guid TargetCartId, int TargetVersion, bool IncludeCustomer);
