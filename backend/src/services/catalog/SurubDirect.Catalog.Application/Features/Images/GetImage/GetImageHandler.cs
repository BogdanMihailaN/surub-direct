using SurubDirect.BuildingBlocks;
namespace SurubDirect.Catalog.Application;
public sealed class GetImageHandler(IProductImageRepository repository, IImageStorage storage)
{
    public async Task<Stream> Handle(Guid productId, Guid imageId, string size, bool includeDrafts, CancellationToken ct)
    {
        if (size is not ("full" or "thumbnail") || !await repository.Exists(productId, imageId, includeDrafts, ct))
            throw new BusinessException("Imaginea nu există.", 404);
        return await storage.Open(imageId, size == "thumbnail", ct)
            ?? throw new BusinessException("Fișierul imaginii este temporar indisponibil.", 503);
    }
}
