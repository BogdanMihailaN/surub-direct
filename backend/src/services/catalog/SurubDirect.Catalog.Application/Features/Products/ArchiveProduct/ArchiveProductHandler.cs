using SurubDirect.BuildingBlocks;

namespace SurubDirect.Catalog.Application;
public sealed class ArchiveProductHandler(ICatalogRepository repository)
{
    public async Task Handle(Guid id, CancellationToken ct)
    {
        var product = await repository.Get(id, ct) ?? throw new BusinessException("Produsul nu există.", 404);
        product.Archive();
        await repository.Save(ct);
    }
}
