using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Application;
public interface IOrderCancellations
{
    Task<DemoOrderDto> Cancel(Guid id, string reason, string actor, CancellationToken ct);
}
