using SurubDirect.BuildingBlocks;
using SurubDirect.Purchasing.Domain;
namespace SurubDirect.Purchasing.Application;
public sealed class AddSupplierItemHandler(ISupplierRepository repository, ICatalogVariants catalog)
{
    public async Task<Guid> Handle(Guid supplierId, AddSupplierItem c, CancellationToken ct)
    {
        var variant = await catalog.Find(c.VariantId, ct) ?? throw new BusinessException("SKU-ul nu există sau produsul este arhivat.", 404);
        Guid id = default;
        await repository.Change(supplierId, supplier => id = supplier.AddItem(variant.Id, variant.Sku, variant.Label,
            c.SupplierCode, c.PackLabel, c.UnitsPerPack, c.NetCostPerPack, c.Currency).Id, ct);
        return id;
    }
}
