using SurubDirect.Catalog.Contracts;
using SurubDirect.Pricing.Contracts;

namespace SurubDirect.Store.Application;
public sealed class ProductEnricher(IPricingReader prices, IStockReader inventory)
{
    public async Task<IReadOnlyList<StoreProduct>> Enrich(IReadOnlyList<ProductDto> products, CancellationToken ct)
    {
        var allPrices = await prices.GetPrices(products.SelectMany(p => p.Variants).SelectMany(v => v.Offers).Select(o => o.Id).ToArray(), ct);
        var allStock = await inventory.GetStock(products.SelectMany(p => p.Variants).Select(v => v.Id).ToArray(), ct);
        return products.Select(p => new StoreProduct(p, allPrices.Where(x => p.Variants.Any(v => v.Offers.Any(o => o.Id == x.OfferId))).ToArray(), allStock.Where(x => p.Variants.Any(v => v.Id == x.VariantId)).ToArray())).ToArray();
    }
}
