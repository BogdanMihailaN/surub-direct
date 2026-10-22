using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Application;
public sealed class GetDemoOrdersHandler(IDemoOrders orders)
{
    public Task<DemoOrderPageDto> Handle(string owner, int page, CancellationToken ct)
    {
        if (page is < 1 or > 100000)
            throw new BusinessException("Pagina nu este validă.");
        return orders.Read(owner, page, ct);
    }
}
