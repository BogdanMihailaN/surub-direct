using SurubDirect.Catalog.Contracts;

namespace SurubDirect.Store.Application;
public sealed class GetProductHandler(ICatalogReader catalog, ProductEnricher enricher)
{
    public async Task<StoreProduct?> Handle(string slug, CancellationToken ct)
    {
        var product = await catalog.GetBySlug(slug, ct);
        return product is null ? null : (await enricher.Enrich([product], ct))[0];
    }
}
