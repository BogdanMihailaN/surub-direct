using SurubDirect.BuildingBlocks;
using SurubDirect.Purchasing.Domain;
namespace SurubDirect.Purchasing.Application;
public sealed class CreatePurchaseOrderHandler(IPurchaseOrderRepository repository)
{ public Task<Guid> Handle(CreatePurchaseOrder c, CancellationToken ct) => repository.Create(c.SupplierId, c.Currency, c.Notes, ct); }
