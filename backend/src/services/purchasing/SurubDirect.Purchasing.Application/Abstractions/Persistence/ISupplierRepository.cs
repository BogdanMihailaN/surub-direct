using SurubDirect.Purchasing.Domain;
using SurubDirect.Purchasing.Contracts;
namespace SurubDirect.Purchasing.Application;
public interface ISupplierRepository
{
    Task Add(Supplier supplier, CancellationToken ct);
    Task Change(Guid id, Action<Supplier> change, CancellationToken ct);
    Task<SupplierDto?> Get(Guid id, CancellationToken ct);
    Task<SupplierPage> List(string? search, bool? active, int page, CancellationToken ct);
}
