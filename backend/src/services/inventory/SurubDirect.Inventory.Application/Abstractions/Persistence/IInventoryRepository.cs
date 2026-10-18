using SurubDirect.Inventory.Domain;

namespace SurubDirect.Inventory.Application;
public interface IInventoryRepository
{
    Task<bool> Receive(StockReceipt receipt, CancellationToken ct);
}
