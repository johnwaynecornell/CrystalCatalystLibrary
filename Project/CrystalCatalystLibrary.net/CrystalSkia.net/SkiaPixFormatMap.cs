using SkiaSharp;

namespace CrystalSkia.net;

public static class SkiaPixFormatMap
{
    public static bool TryGetImageInfo(
        string pixFormat,
        int width,
        int height,
        out SKImageInfo info)
    {
        info = default;
        if (string.IsNullOrWhiteSpace(pixFormat)) return false;

        string normalized = pixFormat.ToLowerInvariant();

        SKColorType colorType;
        SKAlphaType alphaType = SKAlphaType.Premul;

        switch (normalized)
        {
            case "bgra:int8":
                colorType = SKColorType.Bgra8888;
                break;
            case "rgba:int8":
                colorType = SKColorType.Rgba8888;
                break;
            default:
                return false;
        }

        info = new SKImageInfo(width, height, colorType, alphaType);
        return true;
    }

    public static SKImageInfo GetImageInfo(
        string pixFormat,
        int width,
        int height)
    {
        if (TryGetImageInfo(pixFormat, width, height, out var info))
        {
            return info;
        }
        
        // Fallback to a default format that Skia likes
        return new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
    }
}
