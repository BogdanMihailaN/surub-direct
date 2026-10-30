using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Application;
public interface IDemoOrders
{
    Task<DemoOrderDto> Confirm(string owner, Guid preparationId, CancellationToken ct, string? accountId = null);
    Task<DemoOrderPageDto> Read(string owner, int page, CancellationToken ct);
    Task Complete(string owner, Guid preparationId, CancellationToken ct, string? accountId = null);
}
