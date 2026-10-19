using SurubDirect.BuildingBlocks;
using SurubDirect.Purchasing.Domain;
namespace SurubDirect.Purchasing.Application;
public sealed class CreateSupplierHandler(ISupplierRepository repository)
{
    public async Task<Guid> Handle(CreateSupplier c, CancellationToken ct)
    {
        var supplier = Supplier.Create(c.Name, c.CountryCode, c.TaxId, c.ContactName, c.Email, c.Phone, c.Address, c.Notes);
        await repository.Add(supplier, ct); return supplier.Id;
    }
}
