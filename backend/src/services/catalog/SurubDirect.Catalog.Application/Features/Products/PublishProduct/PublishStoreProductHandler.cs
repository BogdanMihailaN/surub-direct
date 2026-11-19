using SurubDirect.BuildingBlocks;
using SurubDirect.Catalog.Contracts.Pricing;
using SurubDirect.Catalog.Domain;

namespace SurubDirect.Catalog.Application;

public sealed class PublishStoreProductHandler(ICatalogRepository repository, IPricingReader prices)
{
    public Task Handle(Guid id, CancellationToken ct) => repository.Change(id, async product =>
    {
        if (product.Status == ProductStatus.Published)
            return false;

        var offerIds = product.Variants.SelectMany(v => v.Offers).Select(o => o.Id).ToArray();
        var found = await prices.GetPrices(offerIds, ct);
        if (offerIds.Length == 0 || found.Count != offerIds.Length)
            throw new BusinessException("Completează prețul tuturor ofertelor înainte de publicare.");

        product.Publish();
        repository.RecordPublication(product);
        return true;
    }, ct);
}
