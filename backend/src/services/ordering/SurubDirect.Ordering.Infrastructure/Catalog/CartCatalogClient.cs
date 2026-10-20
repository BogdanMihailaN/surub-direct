using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
namespace SurubDirect.Ordering.Infrastructure;

public sealed class CartCatalogClient(IHttpClientFactory clients) : ICartCatalog
{
    public async Task<IReadOnlyList<CartOfferSnapshot>?> Read(string slug, CancellationToken ct)
    {
        try
        {
            using var response = await clients.CreateClient("cart-catalog").GetAsync("/api/v1/products/" + Uri.EscapeDataString(slug), ct);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadFromJsonAsync<CatalogResponse>(ct);
            if (data?.Product?.Variants is null || data.Prices is null || data.Stock is null || data.Product.Slug != slug || string.IsNullOrWhiteSpace(data.Product.Name)) throw Unavailable();
            if (data.Product.Variants.Any(v => v is null) || data.Prices.Any(p => p is null) || data.Stock.Any(s => s is null)) throw Unavailable();
            var result = new List<CartOfferSnapshot>();
            foreach (var variant in data.Product.Variants)
            {
                var stock = data.Stock.SingleOrDefault(s => s.VariantId == variant.Id);
                if (stock is null || stock.Available is null || stock.Available < 0 || variant.Offers is null || variant.Offers.Any(o => o is null) || string.IsNullOrWhiteSpace(variant.Sku) || variant.Id == Guid.Empty) throw Unavailable();
                foreach (var offer in variant.Offers)
                {
                    if (offer.Id == Guid.Empty || offer.UnitsPerPack < 1 || offer.MinPacks < 1 || offer.PackStep < 1 || string.IsNullOrWhiteSpace(offer.Label)) throw Unavailable();
                    var price = data.Prices.SingleOrDefault(p => p.OfferId == offer.Id);
                    if (price is not null && (price.NetAmount <= 0 || price.NetAmount > 1000000 || price.GrossAmount < 0 || price.GrossAmount > 2000000 || string.IsNullOrWhiteSpace(price.Currency))) throw Unavailable();
                    result.Add(new(offer.Id, variant.Id, data.Product.Name, variant.Sku, offer.Label, offer.UnitsPerPack, offer.MinPacks, offer.PackStep, stock.Available.Value, price?.NetAmount, price?.GrossAmount, price?.Currency));
                }
            }
            if (result.Select(o => o.OfferId).Distinct().Count() != result.Count) throw Unavailable();
            return result;
        }
        catch (HttpRequestException) { throw Unavailable(); }
        catch (JsonException) { throw Unavailable(); }
        catch (InvalidOperationException) { throw Unavailable(); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw Unavailable(); }
    }
    private static BusinessException Unavailable() => new("Nu putem verifica acum prețurile și stocul. Reîncearcă recalcularea coșului.", 503);
}
