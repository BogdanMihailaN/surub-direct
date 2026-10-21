using SurubDirect.Ordering.Domain;
using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Application;
internal static class StoredCartProjection
{
    public static StoredCartDto Map(ShoppingCart c) => new(c.Id, c.Version, c.UpdatedAt, c.ExpiresAt, c.Lines.Select(l => new StoredCartLineDto(l.ProductSlug, l.OfferId, l.Packs)).ToArray(), CustomerDraftProjection.Map(c.Customer), c.PreparedId);
}
