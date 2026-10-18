using SurubDirect.BuildingBlocks;
using SurubDirect.Inventory.Domain;

namespace SurubDirect.Inventory.Application;
public sealed class ReceiveStockHandler(IInventoryRepository repository, IVariantLookup catalog)
{
    public async Task<bool> Handle(Guid variant, ReceiveStock c, string actor, CancellationToken ct)
    {
        if (!await catalog.VariantExists(variant, ct))
            throw new BusinessException("SKU-ul nu există.", 404);
        return await repository.Receive(StockReceipt.Create(c.RequestId, variant, c.Quantity, c.Reference, actor), ct);
    }
}
