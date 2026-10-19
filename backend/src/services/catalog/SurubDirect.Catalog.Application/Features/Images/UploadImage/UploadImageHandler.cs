namespace SurubDirect.Catalog.Application;
public sealed class UploadImageHandler(IProductImageRepository repository, IImageProcessor processor, IImageStorage storage)
{
    public async Task<Guid> Handle(Guid productId, Stream content, string altText, Guid? variantId, CancellationToken ct)
    {
        var processed = await processor.Process(content, ct);
        return await repository.Change(productId, async product =>
        {
            var image = product.AddImage(Guid.NewGuid(), altText, variantId, processed.Width, processed.Height);
            await storage.Write(image.Id, processed, ct);
            // Files precede the DB commit. An ambiguous commit may leave an orphan, never a broken reference.
            return image.Id;
        }, ct);
    }
}
