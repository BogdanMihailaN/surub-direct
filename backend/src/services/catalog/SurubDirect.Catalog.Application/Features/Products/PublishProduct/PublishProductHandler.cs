using SurubDirect.BuildingBlocks;
using SurubDirect.Catalog.Domain;

namespace SurubDirect.Catalog.Application;
public sealed class PublishProductHandler(ICatalogRepository repository)
{
    public async Task Handle(Guid productId, CancellationToken ct)
    {
        var product = await repository.Get(productId, ct) ?? throw new BusinessException("Produsul nu există.", 404);
        if (product.Status == ProductStatus.Published)
            return;
        product.Publish();
        repository.RecordPublication(product);
        await repository.Save(ct);
    }
}
