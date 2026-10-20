using SurubDirect.BuildingBlocks;
using SurubDirect.Inventory.Domain;
namespace SurubDirect.Inventory.Application;
public sealed class AdjustStockHandler(IStockLedger ledger, IVariantLookup catalog)
{
    public async Task<bool> Handle(Guid variant, AdjustStock c, string actor, CancellationToken ct)
    {
        var adjustment = StockAdjustment.Create(c.RequestId, variant, c.Quantity, c.ExpectedOnHand, c.ExpectedReserved, c.Reference, c.Reason, actor);
        if (!await catalog.VariantExists(variant, ct)) throw new BusinessException("SKU-ul nu există.", 404);
        return await ledger.Adjust(adjustment, ct);
    }
}
