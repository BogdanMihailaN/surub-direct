using SkiaSharp;
using SurubDirect.BuildingBlocks;
using SurubDirect.Catalog.Application;
namespace SurubDirect.Catalog.Infrastructure;
public sealed class SkiaImageProcessor : IImageProcessor
{
    public const int MaximumBytes = 8 * 1024 * 1024;
    public async Task<ProcessedImage> Process(Stream input, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int count;
        while ((count = await input.ReadAsync(chunk.AsMemory(0, Math.Min(chunk.Length, MaximumBytes + 1 - (int)buffer.Length)), ct)) > 0)
        {
            buffer.Write(chunk, 0, count);
            if (buffer.Length > MaximumBytes) throw new BusinessException("Imaginea poate avea maximum 8 MB.", 413);
        }
        using var data = SKData.CreateCopy(buffer.ToArray());
        using var codec = SKCodec.Create(data);
        if (codec is null || codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png or SKEncodedImageFormat.Webp))
            throw new BusinessException("Alege o imagine JPEG, PNG sau WebP validă.");
        var info = codec.Info;
        if (info.Width < 1 || info.Height < 1 || info.Width > 6000 || info.Height > 6000 || (long)info.Width * info.Height > 24000000 || codec.FrameCount > 1)
            throw new BusinessException("Imagine statică: maximum 6000 px pe latură și 24 megapixeli.");
        ct.ThrowIfCancellationRequested();
        using var bitmap = new SKBitmap(new SKImageInfo(info.Width, info.Height, SKColorType.Rgba8888, SKAlphaType.Premul));
        if (codec.GetPixels(bitmap.Info, bitmap.GetPixels()) != SKCodecResult.Success)
            throw new BusinessException("Imaginea este incompletă sau deteriorată.");
        using var image = SKImage.FromBitmap(bitmap);
        var rotated = (int)codec.EncodedOrigin >= 5;
        var width = rotated ? info.Height : info.Width;
        var height = rotated ? info.Width : info.Height;
        var ratio = Math.Min(1d, 1600d / Math.Max(width, height));
        return new(Render(image, codec.EncodedOrigin, 1600), Render(image, codec.EncodedOrigin, 400),
            Math.Max(1, (int)Math.Round(width * ratio)), Math.Max(1, (int)Math.Round(height * ratio)));
    }
    private static byte[] Render(SKImage image, SKEncodedOrigin origin, int maximum)
    {
        var rotated = (int)origin >= 5;
        var width = rotated ? image.Height : image.Width;
        var height = rotated ? image.Width : image.Height;
        var ratio = Math.Min(1d, (double)maximum / Math.Max(width, height));
        var outputWidth = Math.Max(1, (int)Math.Round(width * ratio));
        var outputHeight = Math.Max(1, (int)Math.Round(height * ratio));
        using var surface = SKSurface.Create(new SKImageInfo(outputWidth, outputHeight, SKColorType.Rgba8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        canvas.Scale((float)outputWidth / width, (float)outputHeight / height);
        var w = image.Width;
        var h = image.Height;
        var matrix = origin switch
        {
            SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, w, 0, 1, 0, 0, 0, 1),
            SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, w, 0, -1, h, 0, 0, 1),
            SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, h, 0, 0, 1),
            SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
            SKEncodedOrigin.RightTop => new SKMatrix(0, -1, h, 1, 0, 0, 0, 0, 1),
            SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, h, -1, 0, w, 0, 0, 1),
            SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, w, 0, 0, 1),
            _ => SKMatrix.Identity
        };
        canvas.Concat(matrix);
        canvas.DrawImage(image, new SKRect(0, 0, w, h), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
        using var result = surface.Snapshot();
        using var encoded = result.Encode(SKEncodedImageFormat.Webp, 85);
        return encoded.ToArray();
    }
}
