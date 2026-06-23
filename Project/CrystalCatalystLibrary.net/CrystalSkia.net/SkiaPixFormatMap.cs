using SkiaSharp;
using CrystalCatalystLibrary.net;

namespace CrystalSkia.net;

/// <summary>
/// Maps Crystal pixel format strings to SkiaSharp <see cref="SKImageInfo"/> and <see cref="SKColorType"/>.
/// </summary>
public static class SkiaPixFormatMap
{
    /// <summary>
    /// Attempts to create <see cref="SKImageInfo"/> for the specified Crystal pixel format and dimensions.
    /// </summary>
    public static bool TryGetImageInfo(
        string pixFormat,
        int width,
        int height,
        out SKImageInfo info,
        SKAlphaType? alphaType = null)
    {
        info = default;
        if (string.IsNullOrWhiteSpace(pixFormat)) return false;

        string normalized = pixFormat.ToLowerInvariant();

        SKColorType colorType;
        SKAlphaType resolvedAlphaType = alphaType ?? SKAlphaType.Premul;

        switch (normalized)
        {
            case "bgra:int8":
                colorType = SKColorType.Bgra8888;
                break;
            case "rgba:int8":
                colorType = SKColorType.Rgba8888;
                break;
            case "r:int8":
                colorType = SKColorType.Gray8;
                break;
            case "a:int8":
                colorType = SKColorType.Alpha8;
                break;
            case "rg:int8":
                colorType = SKColorType.Rg88;
                break;
            case "rgba:int16":
                colorType = SKColorType.Rgba16161616;
                break;
            case "rg:int16":
                colorType = SKColorType.Rg1616;
                break;
            case "a:int16":
                colorType = SKColorType.Alpha16;
                break;
            case "rgba:float32":
                colorType = SKColorType.RgbaF32;
                break;
            /*
            case "rgba:float16":
                colorType = SKColorType.RgbaF16;
                break;
            */
            default:
                return false;
        }

        info = new SKImageInfo(width, height, colorType, resolvedAlphaType);
        return true;
    }

    /// <summary>
    /// Attempts to get the Crystal pixel format string for the specified Skia color type.
    /// </summary>
    public static bool TryGetPixFormat(SKColorType colorType, out string pixFormat)
    {
        pixFormat = string.Empty;
        switch (colorType)
        {
            case SKColorType.Bgra8888:
                pixFormat = "bgra:int8";
                break;
            case SKColorType.Rgba8888:
                pixFormat = "rgba:int8";
                break;
            case SKColorType.Gray8:
                pixFormat = "r:int8";
                break;
            case SKColorType.Alpha8:
                pixFormat = "a:int8";
                break;
            case SKColorType.Rg88:
                pixFormat = "rg:int8";
                break;
            case SKColorType.Rgba16161616:
                pixFormat = "rgba:int16";
                break;
            case SKColorType.Rg1616:
                pixFormat = "rg:int16";
                break;
            case SKColorType.Alpha16:
                pixFormat = "a:int16";
                break;
            case SKColorType.RgbaF32:
                pixFormat = "rgba:float32";
                break;
            default:
                return false;
        }
        return true;
    }

    /// <summary>
    /// Gets the Crystal pixel format string for the specified Skia color type. Throws if not supported.
    /// </summary>
    public static string GetPixFormat(SKColorType colorType)
    {
        if (TryGetPixFormat(colorType, out var pixFormat))
        {
            return pixFormat;
        }
        throw new NotSupportedException($"Skia color type '{colorType}' is not supported by PixData.");
    }

    public static bool IsRenderTargetSupported(string pixFormat)
    {
        return TryGetImageInfo(pixFormat, 1, 1, out _);
    }

    public static string GetFallbackRenderTargetFormat(string pixFormat)
    {
        if (PixFormats.IsFloatFormat(pixFormat))
        {
            return "rgba:float32";
        }
        return "bgra:int8";
    }

    public static SKImageInfo GetImageInfo(
        string pixFormat,
        int width,
        int height,
        SKAlphaType? alphaType = null)
    {
        if (TryGetImageInfo(pixFormat, width, height, out var info, alphaType))
        {
            return info;
        }

        throw new NotSupportedException($"Pixel format '{pixFormat}' is not supported as a Skia render target.");
    }
}
