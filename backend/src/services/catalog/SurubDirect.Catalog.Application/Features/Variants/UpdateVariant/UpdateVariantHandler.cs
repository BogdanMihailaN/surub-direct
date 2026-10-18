using SurubDirect.BuildingBlocks;

namespace SurubDirect.Catalog.Application;
public sealed class UpdateVariantHandler(ICatalogRepository repository)
{
    public async Task Handle(Guid productId, Guid variantId, UpdateVariant command, CancellationToken ct)
    {
        var product = await repository.Get(productId, ct) ?? throw new BusinessException("Produsul nu există.", 404);
        product.UpdateVariant(variantId, command.Label, command.DiameterMm, command.LengthMm, command.Material, command.Finish);
        await repository.Save(ct);
    }
}
