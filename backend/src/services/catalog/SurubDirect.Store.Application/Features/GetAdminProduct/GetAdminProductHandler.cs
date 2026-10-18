using SurubDirect.Catalog.Contracts;

namespace SurubDirect.Store.Application;
public sealed class GetAdminProductHandler(ICatalogReader catalog, ProductEnricher enricher)
{
    public async Task<StoreProduct?> Handle(Guid id, CancellationToken ct)
    {
        var product = await catalog.Get(id, true, ct);
        return product is null ? null : (await enricher.Enrich([product], ct))[0];
    }
}
