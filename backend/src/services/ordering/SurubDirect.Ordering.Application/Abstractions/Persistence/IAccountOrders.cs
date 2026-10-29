using SurubDirect.Ordering.Contracts;
namespace SurubDirect.Ordering.Application;
public interface IAccountOrders
{
    Task Link(string accountId, string owner, Guid id, CancellationToken ct);
    Task<DemoOrderPageDto> Read(string accountId, int page, CancellationToken ct);
}
