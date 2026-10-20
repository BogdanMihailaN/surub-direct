using SurubDirect.Inventory.Domain;
using SurubDirect.Inventory.Contracts;
namespace SurubDirect.Inventory.Application;
public interface IStockLedger
{
    Task<bool> Adjust(StockAdjustment adjustment, CancellationToken ct);
    Task<StockLedgerDto> Read(Guid variant, int page, CancellationToken ct);
}
