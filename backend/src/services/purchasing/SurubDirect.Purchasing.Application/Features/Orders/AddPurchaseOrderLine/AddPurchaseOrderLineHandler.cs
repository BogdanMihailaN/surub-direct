using SurubDirect.BuildingBlocks;
using SurubDirect.Purchasing.Domain;
namespace SurubDirect.Purchasing.Application;
public sealed class AddPurchaseOrderLineHandler(IPurchaseOrderRepository repository, ICatalogVariants catalog)
{
    public Task Handle(Guid id, AddPurchaseOrderLine c, CancellationToken ct) => repository.Change(id, c.Version, async (order, supplier) =>
    {
        var line = order.AddLine(supplier, c.SupplierItemId, c.Packs);
        if (await catalog.Find(line.VariantId, ct) is null) throw new BusinessException("SKU-ul nu există sau produsul este arhivat.", 409);
    }, ct);
}
