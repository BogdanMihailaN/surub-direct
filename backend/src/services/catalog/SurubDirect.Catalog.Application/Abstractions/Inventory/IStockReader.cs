namespace SurubDirect.Catalog.Application;
public interface IStockReader
{
    Task<IReadOnlyList<StockSummary>> GetStock(IReadOnlyList<Guid> ids, CancellationToken ct);
}
