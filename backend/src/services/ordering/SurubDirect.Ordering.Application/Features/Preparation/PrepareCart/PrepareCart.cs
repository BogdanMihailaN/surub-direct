namespace SurubDirect.Ordering.Application;
public sealed record PrepareCart(Guid RequestId, Guid CartId, int Version);
