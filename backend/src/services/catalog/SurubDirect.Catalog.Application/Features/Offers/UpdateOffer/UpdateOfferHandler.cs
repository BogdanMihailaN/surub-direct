using SurubDirect.BuildingBlocks;

namespace SurubDirect.Catalog.Application;
public sealed class UpdateOfferHandler(ICatalogRepository repository)
{
    public async Task<bool> Handle(Guid productId, Guid variantId, Guid offerId, UpdateOffer command, CancellationToken ct)
    {
        var product = await repository.Get(productId, ct) ?? throw new BusinessException("Produsul nu există.", 404);
        var packSizeChanged = product.UpdateOffer(variantId, offerId, command.Label, command.UnitsPerPack, command.MinPacks, command.PackStep);
        await repository.Save(ct);
        return packSizeChanged;
    }
}
