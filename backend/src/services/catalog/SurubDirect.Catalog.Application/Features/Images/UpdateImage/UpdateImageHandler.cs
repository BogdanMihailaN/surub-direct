namespace SurubDirect.Catalog.Application;
public sealed class UpdateImageHandler(IProductImageRepository repository)
{
    public Task<Guid> Handle(Guid productId, Guid imageId, UpdateImage command, CancellationToken ct) => repository.Change(productId, product =>
    {
        product.UpdateImage(imageId, command.AltText, command.VariantId);
        return Task.FromResult(imageId);
    }, ct);
}
