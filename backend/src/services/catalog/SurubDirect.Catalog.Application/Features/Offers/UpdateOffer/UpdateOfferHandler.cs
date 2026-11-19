namespace SurubDirect.Catalog.Application;

public sealed class UpdateOfferHandler(ICatalogRepository repository)
{
    public Task<bool> Handle(Guid productId, Guid variantId, Guid offerId, UpdateOffer command, CancellationToken ct)
        => repository.Change(productId, product => Task.FromResult(product.UpdateOffer(variantId, offerId, command.Label, command.UnitsPerPack, command.MinPacks, command.PackStep)), ct);
}
