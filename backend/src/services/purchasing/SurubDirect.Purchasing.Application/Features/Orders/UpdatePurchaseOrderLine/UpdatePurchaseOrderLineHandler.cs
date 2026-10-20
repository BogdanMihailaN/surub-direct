using SurubDirect.BuildingBlocks;
using SurubDirect.Purchasing.Domain;
namespace SurubDirect.Purchasing.Application;
public sealed class UpdatePurchaseOrderLineHandler(IPurchaseOrderRepository repository)
{ public Task Handle(Guid id, Guid lineId, UpdatePurchaseOrderLine c, CancellationToken ct) => repository.Change(id, c.Version, (order, _) => { order.UpdateLine(lineId, c.Packs, c.NetCostPerPack); return Task.CompletedTask; }, ct); }
