using SurubDirect.Purchasing.Domain;
using SurubDirect.Purchasing.Contracts;
namespace SurubDirect.Purchasing.Application;
public interface IPurchaseOrderRepository
{
    Task<Guid> Create(Guid supplierId, string currency, string? notes, CancellationToken ct);
    Task Change(Guid id, int version, Func<PurchaseOrder, Supplier, Task> change, CancellationToken ct);
    Task<PurchaseOrderDto?> Get(Guid id, CancellationToken ct);
    Task<PurchaseOrderPage> List(string? search, PurchaseOrderStatus? status, int page, CancellationToken ct);
}
