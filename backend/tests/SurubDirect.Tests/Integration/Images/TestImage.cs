using SkiaSharp;
namespace SurubDirect.Tests;
internal static class TestImage
{
    public static byte[] Create(int width = 200, int height = 100, SKEncodedImageFormat format = SKEncodedImageFormat.Png)
    {
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        surface.Canvas.Clear(SKColors.White);
        using var paint = new SKPaint { Color = SKColors.OrangeRed };
        surface.Canvas.DrawRect(0, 0, width / 2f, height, paint);
        using var image = surface.Snapshot();
        using var data = image.Encode(format, 100);
        return data.ToArray();
    }
    public static byte[] WithOrientation(int orientation)
    {
        var jpeg = Create(100, 40, SKEncodedImageFormat.Jpeg);
        byte[] exif = [0xff,0xe1,0,34,69,120,105,102,0,0,73,73,42,0,8,0,0,0,1,0,18,1,3,0,1,0,0,0,(byte)orientation,0,0,0,0,0,0,0];
        return [..jpeg[..2], ..exif, ..jpeg[2..]];
    }
}
