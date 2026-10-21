using SurubDirect.Ordering.Domain;

namespace SurubDirect.Ordering.Application;
public sealed record SaveCartCustomer(Guid CartId, int Version, CustomerDraft? Customer);
