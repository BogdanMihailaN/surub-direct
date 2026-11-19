namespace SurubDirect.Catalog.Application;

public sealed class AddOfferHandler(ICatalogRepository repository)
{
    public Task<Guid> Handle(Guid productId, Guid variantId, AddOffer command, CancellationToken ct)
        => repository.Change(productId, product => Task.FromResult(product.AddOffer(variantId, command.Code, command.Label, command.UnitsPerPack, command.MinPacks, command.PackStep).Id), ct);
}
