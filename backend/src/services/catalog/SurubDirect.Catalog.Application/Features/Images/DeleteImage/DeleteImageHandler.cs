namespace SurubDirect.Catalog.Application;
public sealed class DeleteImageHandler(IProductImageRepository repository, IImageStorage storage)
{
    public async Task Handle(Guid productId, Guid imageId, CancellationToken ct)
    {
        await repository.Change(productId, product => { product.RemoveImage(imageId); return Task.FromResult(imageId); }, ct);
        await storage.Delete(imageId, CancellationToken.None);
    }
}
