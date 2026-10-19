namespace SurubDirect.Catalog.Application;
public sealed class ArrangeImagesHandler(IProductImageRepository repository)
{
    public Task<Guid> Handle(Guid productId, ArrangeImages command, CancellationToken ct) => repository.Change(productId, product =>
    {
        product.ArrangeImages(command.ImageIds, command.PrimaryId);
        return Task.FromResult(product.Id);
    }, ct);
}
