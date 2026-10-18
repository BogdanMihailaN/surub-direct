using SurubDirect.Inventory.Contracts;
using SurubDirect.Store.Application;

namespace SurubDirect.Tests;
public sealed class TestStockReader(IInventoryReader inventory) : IStockReader
{
    public async Task<IReadOnlyList<StockSummary>> GetStock(IReadOnlyList<Guid> ids, CancellationToken ct) => (await inventory.GetStock(ids, ct)).Select(x => new StockSummary(x.VariantId, x.OnHand, x.Reserved, x.Available)).ToArray();
}
