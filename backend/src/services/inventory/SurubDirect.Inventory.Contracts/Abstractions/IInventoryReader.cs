namespace SurubDirect.Inventory.Contracts;
public interface IInventoryReader
{
    Task<IReadOnlyList<StockDto>> GetStock(IReadOnlyList<Guid> ids, CancellationToken ct);
}
