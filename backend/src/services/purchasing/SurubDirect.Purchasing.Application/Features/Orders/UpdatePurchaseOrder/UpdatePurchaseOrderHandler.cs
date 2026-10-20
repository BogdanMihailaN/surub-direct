using SurubDirect.BuildingBlocks;
using SurubDirect.Purchasing.Domain;
namespace SurubDirect.Purchasing.Application;
public sealed class UpdatePurchaseOrderHandler(IPurchaseOrderRepository repository)
{ public Task Handle(Guid id, UpdatePurchaseOrder c, CancellationToken ct) => repository.Change(id, c.Version, (order, _) => { order.UpdateNotes(c.Notes); return Task.CompletedTask; }, ct); }
