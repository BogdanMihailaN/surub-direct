namespace SurubDirect.Catalog.Application;
public interface IImageProcessor
{
    Task<ProcessedImage> Process(Stream input, CancellationToken ct);
}
