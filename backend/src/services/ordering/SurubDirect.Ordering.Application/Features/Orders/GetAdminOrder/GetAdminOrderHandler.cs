using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Application;
public sealed class GetAdminOrderHandler(IAdminDemoOrders orders)
{
    public async Task<AdminOrderDetailDto> Handle(Guid id, CancellationToken ct) => await orders.Find(id, ct) ?? throw new BusinessException("Comanda nu există.", 404);
}
