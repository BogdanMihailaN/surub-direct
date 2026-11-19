namespace SurubDirect.Catalog.Application;

public sealed class AddVariantHandler(ICatalogRepository repository)
{
    public Task<Guid> Handle(Guid productId, AddVariant command, CancellationToken ct)
        => repository.Change(productId, product => Task.FromResult(product.AddVariant(command.Sku, command.Label, command.DiameterMm, command.LengthMm, command.Material, command.Finish).Id), ct);
}
