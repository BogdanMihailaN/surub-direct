using System.Text;
using SkiaSharp;
using SurubDirect.BuildingBlocks;
using SurubDirect.Catalog.Infrastructure;
using Xunit;
namespace SurubDirect.Tests;
public class ImageProcessingTests
{
    [Theory]
    [InlineData(SKEncodedImageFormat.Png)]
    [InlineData(SKEncodedImageFormat.Jpeg)]
    [InlineData(SKEncodedImageFormat.Webp)]
    public async Task Accepted_formats_are_decoded_resized_and_reencoded(SKEncodedImageFormat format)
    {
        var processed = await new SkiaImageProcessor().Process(new MemoryStream(TestImage.Create(2000, 1000, format)), default);
        Assert.Equal(1600, processed.Width);
        Assert.Equal(800, processed.Height);
        using var fullData = SKData.CreateCopy(processed.Content);
        using var full = SKCodec.Create(fullData);
        using var thumb = SKBitmap.Decode(processed.Thumbnail);
        Assert.Equal(SKEncodedImageFormat.Webp, full.EncodedFormat);
        Assert.Equal(1600, full.Info.Width);
        Assert.Equal(400, thumb.Width);
        Assert.Equal(200, thumb.Height);
    }
    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    [InlineData(5)] [InlineData(6)] [InlineData(7)] [InlineData(8)]
    public async Task Exif_orientation_is_applied_and_removed(int orientation)
    {
        var processed = await new SkiaImageProcessor().Process(new MemoryStream(TestImage.WithOrientation(orientation)), default);
        Assert.Equal(orientation >= 5 ? 40 : 100, processed.Width);
        Assert.Equal(orientation >= 5 ? 100 : 40, processed.Height);
        using var data = SKData.CreateCopy(processed.Content);
        using var codec = SKCodec.Create(data);
        using var image = SKBitmap.Decode(processed.Content);
        Assert.Equal(SKEncodedOrigin.TopLeft, codec.EncodedOrigin);
        var topLeft = image.GetPixel(5, 5);
        var shouldBeRed = orientation is 1 or 4 or 5 or 6;
        Assert.Equal(shouldBeRed, topLeft.Green < 180);
    }
    [Fact]
    public async Task Invalid_truncated_oversized_and_excessive_dimension_files_are_rejected()
    {
        var processor = new SkiaImageProcessor();
        await Assert.ThrowsAsync<BusinessException>(() => processor.Process(new MemoryStream(Encoding.UTF8.GetBytes("<svg><script>alert(1)</script></svg>")), default));
        await Assert.ThrowsAsync<BusinessException>(() => processor.Process(new MemoryStream(TestImage.Create()[..30]), default));
        await Assert.ThrowsAsync<BusinessException>(() => processor.Process(new MemoryStream(TestImage.Create(6001, 1)), default));
        Assert.Equal(413, (await Assert.ThrowsAsync<BusinessException>(() => processor.Process(new MemoryStream(new byte[SkiaImageProcessor.MaximumBytes + 1]), default))).Status);
    }
}
