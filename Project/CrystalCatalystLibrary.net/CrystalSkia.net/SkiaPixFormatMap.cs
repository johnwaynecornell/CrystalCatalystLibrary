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

    public static bool IsRenderTargetSupported(string pixFormat)
    {
        return TryGetImageInfo(pixFormat, 1, 1, out _);
    }

    public static string GetFallbackRenderTargetFormat(string pixFormat)
    {
        return "bgra:int8";
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

        throw new NotSupportedException($"Pixel format '{pixFormat}' is not supported as a Skia render target.");
    }
}
