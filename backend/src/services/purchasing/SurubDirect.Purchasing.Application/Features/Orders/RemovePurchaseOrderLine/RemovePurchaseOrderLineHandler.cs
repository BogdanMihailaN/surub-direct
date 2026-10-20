using SurubDirect.BuildingBlocks;
using SurubDirect.Purchasing.Domain;
namespace SurubDirect.Purchasing.Application;
public sealed class RemovePurchaseOrderLineHandler(IPurchaseOrderRepository repository)
{ public Task Handle(Guid id, Guid lineId, RemovePurchaseOrderLine c, CancellationToken ct) => repository.Change(id, c.Version, (order, _) => { order.RemoveLine(lineId); return Task.CompletedTask; }, ct); }
