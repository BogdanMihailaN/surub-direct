using SurubDirect.BuildingBlocks;
using SurubDirect.Inventory.Contracts;
namespace SurubDirect.Inventory.Application;
public sealed class GetStockLedgerHandler(IStockLedger ledger, IVariantLookup catalog)
{
    public async Task<StockLedgerDto> Handle(Guid variant, int page, CancellationToken ct)
    {
        if (page < 1 || page > 100000) throw new BusinessException("Pagină invalidă.");
        if (!await catalog.VariantExists(variant, ct)) throw new BusinessException("SKU-ul nu există.", 404);
        return await ledger.Read(variant, page, ct);
    }
}
