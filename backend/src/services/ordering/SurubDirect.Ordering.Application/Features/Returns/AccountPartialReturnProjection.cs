using SurubDirect.Ordering.Contracts;
using SurubDirect.Ordering.Domain;
namespace SurubDirect.Ordering.Application;
public static class AccountPartialReturnProjection
{
    public static AccountPartialReturnDto Map(PartialReturnRequest r) => new(r.Id, r.Status, r.CreatedAt, r.InventoryReceivedAt,
        r.Lines.Select(x => new AccountPartialReturnLineDto(x.OfferId, x.ProductName, x.Sku, x.OfferLabel, x.Packs, x.UnitsPerPack, x.Quantity)).ToArray());
}
