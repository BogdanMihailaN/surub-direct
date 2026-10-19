using System.Net;
using System.Text.Json;
using SurubDirect.BuildingBlocks;
using SurubDirect.Purchasing.Application;
namespace SurubDirect.Purchasing.Api.Clients;
public sealed class CatalogVariantsClient(IHttpClientFactory clients) : ICatalogVariants
{
    public async Task<CatalogVariant?> Find(Guid id, CancellationToken ct)
    {
        var variant = await Read<CatalogVariant>($"/internal/purchasing-variants/{id}", true, ct);
        if (variant is not null && (!Valid(variant) || variant.Id != id)) throw Unavailable();
        return variant;
    }
    public async Task<IReadOnlyList<CatalogVariant>> Search(string search, CancellationToken ct)
    {
        var variants = await Read<CatalogVariant[]>("/internal/purchasing-variants?search=" + Uri.EscapeDataString(search), false, ct) ?? throw Unavailable();
        if (variants.Length > 25 || variants.Any(v => !Valid(v))) throw Unavailable();
        return variants;
    }
    private static bool Valid(CatalogVariant? v) => v is not null && v.Id != Guid.Empty &&
        !string.IsNullOrWhiteSpace(v.Sku) && v.Sku.Length <= 80 && !string.IsNullOrWhiteSpace(v.Label) && v.Label.Length <= 160 && !string.IsNullOrWhiteSpace(v.ProductName);
    private async Task<T?> Read<T>(string path, bool allowMissing, CancellationToken ct) where T : class
    {
        try
        {
            using var response = await clients.CreateClient("catalog").GetAsync(path, ct);
            if (allowMissing && response.StatusCode == HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(ct) ?? throw Unavailable();
        }
        catch (HttpRequestException) { throw Unavailable(); }
        catch (JsonException) { throw Unavailable(); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw Unavailable(); }
    }
    private static BusinessException Unavailable() => new("Catalogul nu poate fi verificat momentan. Reîncearcă asocierea SKU-ului.", 503);
}
