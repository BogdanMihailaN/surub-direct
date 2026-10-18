namespace SurubDirect.Store.Application;
public interface IStockReader
{
    Task<IReadOnlyList<StockSummary>> GetStock(IReadOnlyList<Guid> ids, CancellationToken ct);
}
