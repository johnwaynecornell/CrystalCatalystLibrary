using Silk.NET.OpenGL;
using CrystalCatalystLibrary.net;

namespace CrystalOpenGL;

public static class GLPixFormatMap
{
    public static bool TryGetUploadFormat(
        string pixFormat,
        out InternalFormat internalFormat,
        out PixelFormat pixelFormat,
        out PixelType pixelType)
    {
        internalFormat = default;
        pixelFormat = default;
        pixelType = default;

        if (string.IsNullOrWhiteSpace(pixFormat)) return false;

        string normalized = pixFormat.ToLowerInvariant();

        switch (normalized)
        {
            case "rgba:int8":
                internalFormat = InternalFormat.Rgba8;
                pixelFormat = PixelFormat.Rgba;
                pixelType = PixelType.UnsignedByte;
                return true;
            case "bgra:int8":
                internalFormat = InternalFormat.Rgba8;
                pixelFormat = PixelFormat.Bgra;
                pixelType = PixelType.UnsignedByte;
                return true;
            case "rgb:int8":
                internalFormat = InternalFormat.Rgb8;
                pixelFormat = PixelFormat.Rgb;
                pixelType = PixelType.UnsignedByte;
                return true;
            case "bgr:int8":
                internalFormat = InternalFormat.Rgb8;
                pixelFormat = PixelFormat.Bgr;
                pixelType = PixelType.UnsignedByte;
                return true;
            case "r:int8":
                internalFormat = InternalFormat.R8;
                pixelFormat = PixelFormat.Red;
                pixelType = PixelType.UnsignedByte;
                return true;
            case "rg:int8":
                internalFormat = InternalFormat.RG8;
                pixelFormat = PixelFormat.RG;
                pixelType = PixelType.UnsignedByte;
                return true;
            case "rgba:int16":
                internalFormat = InternalFormat.Rgba16;
                pixelFormat = PixelFormat.Rgba;
                pixelType = PixelType.UnsignedShort;
                return true;
            case "rgb:int16":
                internalFormat = InternalFormat.Rgb16;
                pixelFormat = PixelFormat.Rgb;
                pixelType = PixelType.UnsignedShort;
                return true;
            case "rg:int16":
                internalFormat = InternalFormat.RG16;
                pixelFormat = PixelFormat.RG;
                pixelType = PixelType.UnsignedShort;
                return true;
            case "r:int16":
                internalFormat = InternalFormat.R16;
                pixelFormat = PixelFormat.Red;
                pixelType = PixelType.UnsignedShort;
                return true;
            case "rgba:float32":
                internalFormat = InternalFormat.Rgba32f;
                pixelFormat = PixelFormat.Rgba;
                pixelType = PixelType.Float;
                return true;
            case "rgb:float32":
                internalFormat = InternalFormat.Rgb32f;
                pixelFormat = PixelFormat.Rgb;
                pixelType = PixelType.Float;
                return true;
            /*
            case "rgba:float16":
                internalFormat = InternalFormat.Rgba16f;
                pixelFormat = PixelFormat.Rgba;
                pixelType = PixelType.HalfFloat;
                return true;
            case "rgb:float16":
                internalFormat = InternalFormat.Rgb16f;
                pixelFormat = PixelFormat.Rgb;
                pixelType = PixelType.HalfFloat;
                return true;
            */
            case "bgra:float32":
                internalFormat = InternalFormat.Rgba32f;
                pixelFormat = PixelFormat.Bgra;
                pixelType = PixelType.Float;
                return true;
            case "bgr:float32":
                internalFormat = InternalFormat.Rgb32f;
                pixelFormat = PixelFormat.Bgr;
                pixelType = PixelType.Float;
                return true;
            default:
                return false;
        }
    }

    public static bool IsUploadSupported(string pixFormat)
    {
        return TryGetUploadFormat(pixFormat, out _, out _, out _);
    }

    public static string GetFallbackUploadFormat(string pixFormat)
    {
        if (PixFormats.IsFloatFormat(pixFormat))
        {
            return "rgba:float32";
        }
        return "rgba:int8";
    }

    public static bool TryGetPixFormat(InternalFormat internalFormat, out string pixFormat)
    {
        switch (internalFormat)
        {
            case InternalFormat.Rgba8:
                pixFormat = "rgba:int8";
                return true;
            case InternalFormat.Rgb8:
                pixFormat = "rgb:int8";
                return true;
            case InternalFormat.R8:
                pixFormat = "r:int8";
                return true;
            case InternalFormat.RG8:
                pixFormat = "rg:int8";
                return true;
            case InternalFormat.Rgba16:
                pixFormat = "rgba:int16";
                return true;
            case InternalFormat.Rgb16:
                pixFormat = "rgb:int16";
                return true;
            case InternalFormat.RG16:
                pixFormat = "rg:int16";
                return true;
            case InternalFormat.R16:
                pixFormat = "r:int16";
                return true;
            case InternalFormat.Rgba32f:
                pixFormat = "rgba:float32";
                return true;
            case InternalFormat.Rgb32f:
                pixFormat = "rgb:float32";
                return true;
            /*
            case InternalFormat.Rgba16f:
                pixFormat = "rgba:float16";
                return true;
            case InternalFormat.Rgb16f:
                pixFormat = "rgb:float16";            
                return true;
            */
            default:
                pixFormat = "";
                return false;
        }
    }
}
