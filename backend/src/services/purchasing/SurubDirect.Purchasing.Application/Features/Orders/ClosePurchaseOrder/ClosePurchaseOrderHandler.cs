namespace SurubDirect.Purchasing.Application;

public sealed class ClosePurchaseOrderHandler(IPurchaseOrderRepository repository)
{
    public Task Handle(Guid id, ClosePurchaseOrder command, string actor, CancellationToken ct) => repository.Change(id, command.Version, (order, _) => { order.Close(command.Reason, actor); return Task.CompletedTask; }, ct);
}
