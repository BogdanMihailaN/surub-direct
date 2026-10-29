using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Contracts;
namespace SurubDirect.Ordering.Application;
public sealed class GetAccountOrdersHandler(IAccountOrders orders)
{
    public Task<DemoOrderPageDto> Handle(string accountId, int page, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(accountId) || accountId.Length > 450 || page is < 1 or > 100000)
            throw new BusinessException("Datele cererii nu sunt valide.");
        return orders.Read(accountId, page, ct);
    }
}
