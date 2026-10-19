namespace SurubDirect.Catalog.Application;
public interface IImageStorage
{
    Task Write(Guid id, ProcessedImage image, CancellationToken ct);
    Task<Stream?> Open(Guid id, bool thumbnail, CancellationToken ct);
    Task Delete(Guid id, CancellationToken ct);
}
