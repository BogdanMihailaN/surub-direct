using SurubDirect.BuildingBlocks;
using SurubDirect.Store.Application;

namespace SurubDirect.Catalog.Api.Clients;
public sealed class InventoryClient(IHttpClientFactory clients) : IStockReader
{
    public async Task<IReadOnlyList<StockSummary>> GetStock(IReadOnlyList<Guid> ids, CancellationToken ct)
    {
        if (ids.Count == 0)
            return[];
        try
        {
            using var response = await clients.CreateClient("inventory").PostAsJsonAsync("/internal/stock/query", new { variantIds = ids }, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<StockSummary[]>(ct) ?? throw new BusinessException("Răspuns invalid de la serviciul de stoc.", 503);
        }
        catch (HttpRequestException)
        {
            throw new BusinessException("Stocul nu poate fi verificat momentan. Reîncearcă.", 503);
        }
        catch (OperationCanceledException)when (!ct.IsCancellationRequested)
        {
            throw new BusinessException("Stocul nu poate fi verificat momentan. Reîncearcă.", 503);
        }
    }
}
