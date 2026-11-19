using System.Text.Json;
using SurubDirect.BuildingBlocks;
using SurubDirect.Catalog.Application;

namespace SurubDirect.Catalog.Api.Clients;
public sealed class InventoryClient(IHttpClientFactory clients) : IStockReader
{
    private const int MaximumBatchSize = 5000;

    public async Task<IReadOnlyList<StockSummary>> GetStock(IReadOnlyList<Guid> ids, CancellationToken ct)
    {
        if (ids.Count == 0)
            return[];
        try
        {
            var client = clients.CreateClient("inventory");
            var stock = new List<StockSummary>();
            foreach (var batch in ids.Distinct().Chunk(MaximumBatchSize))
            {
                using var response = await client.PostAsJsonAsync("/internal/stock/query", new { variantIds = batch }, ct);
                response.EnsureSuccessStatusCode();
                var items = await response.Content.ReadFromJsonAsync<InventoryStockRow[]>(ct);
                if (items is null || items.Length != batch.Length || items.Any(x => x is null ||
                    x.OnHand < 0 || x.Reserved < 0 || x.Available < 0 || x.Available != x.OnHand - x.Reserved) ||
                    !batch.ToHashSet().SetEquals(items.Select(x => x.VariantId)))
                    throw new BusinessException("Răspuns invalid de la serviciul de stoc.", 503);
                stock.AddRange(items.Select(x => new StockSummary(x.VariantId, x.OnHand, x.Reserved, x.Available)));
            }
            return stock;
        }
        catch (Exception error) when (error is HttpRequestException or JsonException)
        {
            throw new BusinessException("Stocul nu poate fi verificat momentan. Reîncearcă.", 503);
        }
        catch (OperationCanceledException)when (!ct.IsCancellationRequested)
        {
            throw new BusinessException("Stocul nu poate fi verificat momentan. Reîncearcă.", 503);
        }
    }
}
