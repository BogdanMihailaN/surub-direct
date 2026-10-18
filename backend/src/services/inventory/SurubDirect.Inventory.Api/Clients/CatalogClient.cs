using System.Net;
using SurubDirect.BuildingBlocks;
using SurubDirect.Inventory.Application;

namespace SurubDirect.Inventory.Api.Clients;
public sealed class CatalogClient(IHttpClientFactory clients) : IVariantLookup
{
    public async Task<bool> VariantExists(Guid id, CancellationToken ct)
    {
        try
        {
            using var response = await clients.CreateClient("catalog").GetAsync($"/internal/variants/{id}", ct);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return false;
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (HttpRequestException)
        {
            throw new BusinessException("Catalogul nu poate fi verificat momentan.", 503);
        }
        catch (OperationCanceledException)when (!ct.IsCancellationRequested)
        {
            throw new BusinessException("Catalogul nu poate fi verificat momentan.", 503);
        }
    }
}
