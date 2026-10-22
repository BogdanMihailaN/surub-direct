using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Application;
public sealed class CancelDemoOrderHandler(IOrderCancellations orders)
{
    public Task<DemoOrderDto> Handle(Guid id, string? reason, string actor, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new BusinessException("Motivul anulării este obligatoriu.");
        return orders.Cancel(id, reason, actor, ct);
    }
}
