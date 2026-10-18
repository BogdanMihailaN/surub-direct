using SurubDirect.BuildingBlocks;

namespace SurubDirect.Catalog.Application;
public sealed class AddOfferHandler(ICatalogRepository repository)
{
    public async Task<Guid> Handle(Guid productId, Guid variantId, AddOffer command, CancellationToken ct)
    {
        var product = await repository.Get(productId, ct) ?? throw new BusinessException("Produsul nu există.", 404);
        product.EnsureEditable();
        var variant = product.Variants.SingleOrDefault(x => x.Id == variantId) ?? throw new BusinessException("Varianta nu există.", 404);
        var offer = variant.AddOffer(command.Code, command.Label, command.UnitsPerPack, command.MinPacks, command.PackStep);
        await repository.Save(ct);
        return offer.Id;
    }
}
