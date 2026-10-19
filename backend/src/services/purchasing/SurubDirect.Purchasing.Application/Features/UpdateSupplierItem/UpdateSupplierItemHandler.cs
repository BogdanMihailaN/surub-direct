using SurubDirect.BuildingBlocks;
using SurubDirect.Purchasing.Domain;
namespace SurubDirect.Purchasing.Application;
public sealed class UpdateSupplierItemHandler(ISupplierRepository repository)
{
    public Task Handle(Guid supplierId, Guid id, UpdateSupplierItem c, CancellationToken ct) => repository.Change(supplierId,
        s => s.UpdateItem(id, c.SupplierCode, c.PackLabel, c.UnitsPerPack, c.NetCostPerPack, c.Currency, c.IsActive), ct);
}
