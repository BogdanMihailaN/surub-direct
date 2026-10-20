using SurubDirect.BuildingBlocks;
using SurubDirect.Purchasing.Domain;
namespace SurubDirect.Purchasing.Application;
public sealed class CancelPurchaseOrderHandler(IPurchaseOrderRepository repository)
{ public Task Handle(Guid id, CancelPurchaseOrder c, CancellationToken ct) => repository.Change(id, c.Version, (order, _) => { order.Cancel(c.Reason); return Task.CompletedTask; }, ct); }
