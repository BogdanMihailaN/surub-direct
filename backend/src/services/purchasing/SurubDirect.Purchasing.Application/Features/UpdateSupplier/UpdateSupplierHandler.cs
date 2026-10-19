using SurubDirect.BuildingBlocks;
using SurubDirect.Purchasing.Domain;
namespace SurubDirect.Purchasing.Application;
public sealed class UpdateSupplierHandler(ISupplierRepository repository)
{
    public Task Handle(Guid id, UpdateSupplier c, CancellationToken ct) => repository.Change(id,
        s => s.Update(c.Name, c.CountryCode, c.TaxId, c.ContactName, c.Email, c.Phone, c.Address, c.Notes, c.IsActive), ct);
}
