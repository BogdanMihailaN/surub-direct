using Microsoft.Extensions.Logging;
using SurubDirect.Catalog.Application;
namespace SurubDirect.Catalog.Infrastructure;
public sealed class LocalImageStorage(string root, ILogger<LocalImageStorage> logger) : IImageStorage
{
    private string PathFor(Guid id, bool thumbnail) => Path.Combine(Path.GetFullPath(root), id.ToString("N") + (thumbnail ? "-thumb.webp" : ".webp"));
    public async Task Write(Guid id, ProcessedImage image, CancellationToken ct)
    {
        Directory.CreateDirectory(root);
        await WriteFile(PathFor(id, false), image.Content, ct);
        await WriteFile(PathFor(id, true), image.Thumbnail, ct);
    }
    private static async Task WriteFile(string path, byte[] content, CancellationToken ct)
    {
        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        await file.WriteAsync(content, ct);
    }
    public Task<Stream?> Open(Guid id, bool thumbnail, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try { return Task.FromResult<Stream?>(new FileStream(PathFor(id, thumbnail), FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, 81920, true)); }
        catch (FileNotFoundException) { return Task.FromResult<Stream?>(null); }
        catch (DirectoryNotFoundException) { return Task.FromResult<Stream?>(null); }
    }
    public Task Delete(Guid id, CancellationToken ct)
    {
        foreach (var thumbnail in new[] { false, true })
        {
            try { File.Delete(PathFor(id, thumbnail)); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                // Metadata is already removed. The private orphan is inaccessible through the API.
                logger.LogWarning(error, "Image file cleanup required for {ImageId}, thumbnail {Thumbnail}", id, thumbnail);
            }
        }
        return Task.CompletedTask;
    }
}
