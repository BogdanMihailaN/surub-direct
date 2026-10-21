namespace SurubDirect.Ordering.Contracts;
public sealed record StoredCartLineDto(string ProductSlug, Guid OfferId, int Packs);
