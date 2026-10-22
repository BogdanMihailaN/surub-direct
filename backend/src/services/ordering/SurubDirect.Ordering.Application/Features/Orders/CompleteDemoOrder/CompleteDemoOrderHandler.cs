using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Application;
public sealed class CompleteDemoOrderHandler(IDemoOrders orders)
{
    public Task Handle(string owner, Guid id, CancellationToken ct) => orders.Complete(owner, id, ct);
}
