using SurubDirect.BuildingBlocks;
using SurubDirect.Catalog.Application;
using SurubDirect.Catalog.Contracts;
using SurubDirect.Pricing.Contracts;

namespace SurubDirect.Store.Application;
public sealed class PublishStoreProductHandler(ICatalogReader catalog, IPricingReader prices, PublishProductHandler publish)
{
    public async Task Handle(Guid id, CancellationToken ct)
    {
        var product = await catalog.Get(id, true, ct) ?? throw new BusinessException("Produsul nu există.", 404);
        var ids = product.Variants.SelectMany(v => v.Offers).Select(o => o.Id).ToArray();
        var found = await prices.GetPrices(ids, ct);
        if (ids.Length == 0 || found.Count != ids.Length)
            throw new BusinessException("Completează prețul tuturor ofertelor înainte de publicare.");
        await publish.Handle(id, ct);
    }
}
