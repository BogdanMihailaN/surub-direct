using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Application;
public sealed class ConfirmDemoOrderHandler(IDemoOrders orders)
{
    public Task<DemoOrderDto> Handle(string owner, Guid id, CancellationToken ct) => orders.Confirm(owner, id, ct);
}
