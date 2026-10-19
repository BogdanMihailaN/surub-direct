namespace SurubDirect.Catalog.Application;
public sealed record ProcessedImage(byte[] Content, byte[] Thumbnail, int Width, int Height);
