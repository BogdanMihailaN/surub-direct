using SurubDirect.BuildingBlocks;

namespace SurubDirect.Catalog.Application;
public sealed class AddVariantHandler(ICatalogRepository repository)
{
    public async Task<Guid> Handle(Guid productId, AddVariant command, CancellationToken ct)
    {
        var product = await repository.Get(productId, ct) ?? throw new BusinessException("Produsul nu există.", 404);
        var variant = product.AddVariant(command.Sku, command.Label, command.DiameterMm, command.LengthMm, command.Material, command.Finish);
        await repository.Save(ct);
        return variant.Id;
    }
}
