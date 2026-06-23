using CrystalCatalystLibrary.net;
using SkiaSharp;

namespace CrystalSkia.net;

/// <summary>
/// Provides extension methods and utilities for converting between <see cref="PixData"/> and SkiaSharp types.
/// </summary>
public static class PixDataSkia
{
    /// <summary>
    /// Attempts to get <see cref="SKImageInfo"/> corresponding to the <see cref="PixData"/> format and dimensions.
    /// </summary>
    public static bool TryGetImageInfo(PixData pixData, out SKImageInfo info, SKAlphaType? alphaType = null)
    {
        info = default;

        if (!pixData)
            return false;

        if (pixData.width <= 0 || pixData.height <= 0)
            return false;

        string? formatString = pixData.pix_format.ToString();
        if (string.IsNullOrWhiteSpace(formatString))
            return false;

        string pixFormat = PixFormats.Parse(formatString).PixFormat;
        return SkiaPixFormatMap.TryGetImageInfo(
            pixFormat,
            pixData.width,
            pixData.height,
            out info,
            alphaType);
    }

    /// <summary>
    /// Gets <see cref="SKImageInfo"/> for the <see cref="PixData"/>. Throws if the format is not supported by Skia.
    /// </summary>
    public static SKImageInfo GetImageInfo(PixData pixData, SKAlphaType? alphaType = null)
    {
        if (TryGetImageInfo(pixData, out var info, alphaType))
            return info;

        string format = pixData.pix_format.ToString() ?? "";
        throw new NotSupportedException(
            $"PixData format '{format}' cannot be viewed as a Skia bitmap.");
    }

    /// <summary>
    /// Gets the row stride (bytes per row) for the <see cref="PixData"/>.
    /// </summary>
    public static int GetStride(PixData pixData)
    {
        if (!pixData)
            throw new ArgumentException("PixData has no pixel buffer.", nameof(pixData));

        string? formatString = pixData.pix_format.ToString();
        if (string.IsNullOrWhiteSpace(formatString))
            throw new ArgumentException("PixData has no pixel format.", nameof(pixData));

        string pixFormat = PixFormats.Parse(formatString).PixFormat;
        return PixFormats.GetBytesPerPixel(pixFormat) * pixData.width;
    }

    /// <summary>
    /// Creates an <see cref="SKBitmap"/> that views the <see cref="PixData"/> pixel buffer without copying it.
    /// </summary>
    /// <remarks>
    /// The <see cref="PixData"/> must remain valid for the lifetime of the returned bitmap.
    /// </remarks>
    public static SKBitmap CreateBitmapView(PixData pixData, SKAlphaType? alphaType = null)
    {
        if (!pixData)
            throw new ArgumentException("PixData has no pixel buffer.", nameof(pixData));

        SKImageInfo info = GetImageInfo(pixData, alphaType);
        int stride = GetStride(pixData);

        var bitmap = new SKBitmap();
        bool installed = bitmap.InstallPixels(info, pixData.pix_data, stride);

        if (!installed)
        {
            bitmap.Dispose();
            throw new InvalidOperationException(
                $"Failed to install PixData pixels into SKBitmap for format '{pixData.pix_format}'.");
        }

        return bitmap;
    }
    
    /// <summary>
    /// Creates a <see cref="PixData"/> that views the <see cref="SKBitmap"/> pixel buffer without copying it.
    /// </summary>
    /// <remarks>
    /// The <see cref="SKBitmap"/> must remain valid for the lifetime of the returned <see cref="PixData"/>.
    /// </remarks>
    public static PixData CreatePixDataView(SKBitmap bitmap)
    {
        if (bitmap == null)
            throw new ArgumentNullException(nameof(bitmap));

        if (bitmap.IsEmpty)
            throw new ArgumentException("Bitmap is empty.", nameof(bitmap));

        return new PixData
        {
            width = bitmap.Width,
            height = bitmap.Height,
            pix_format = SkiaPixFormatMap.GetPixFormat(bitmap.ColorType),
            pix_data = bitmap.GetPixels(),
            pix_data_length = (IntPtr)bitmap.ByteCount
            
        };
    }

    public static void WithPixDataView(SKBitmap bitmap, Action<PixData> action)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        ArgumentNullException.ThrowIfNull(action);
        PixData pixData = CreatePixDataView(bitmap);
        try
        {
            action(pixData);
        }
        finally
        {
            pixData.Dispose();
        }
    }

    /// <summary>
    /// Executes an action with a temporary <see cref="SKBitmap"/> view of the <see cref="PixData"/>.
    /// </summary>
    public static void WithBitmapView(PixData pixData, Action<SKBitmap> action, SKAlphaType? alphaType = null)
    {
        ArgumentNullException.ThrowIfNull(action);

        using SKBitmap bitmap = CreateBitmapView(pixData, alphaType);
        action(bitmap);
    }

    /// <summary>
    /// Executes a function with a temporary <see cref="SKBitmap"/> view of the <see cref="PixData"/> and returns its result.
    /// </summary>
    public static T WithBitmapView<T>(PixData pixData, Func<SKBitmap, T> func, SKAlphaType? alphaType = null)
    {
        ArgumentNullException.ThrowIfNull(func);

        using SKBitmap bitmap = CreateBitmapView(pixData, alphaType);
        return func(bitmap);
    }

    /// <summary>
    /// Executes an action with a temporary <see cref="SKCanvas"/> targeting the <see cref="PixData"/>.
    /// </summary>
    public static void WithCanvasView(PixData pixData, Action<SKBitmap, SKCanvas> action, SKAlphaType? alphaType = null)
    {
        ArgumentNullException.ThrowIfNull(action);

        using SKBitmap bitmap = CreateBitmapView(pixData, alphaType);
        using SKCanvas canvas = new SKCanvas(bitmap);

        action(bitmap, canvas);
        canvas.Flush();
    }

    /// <summary>
    /// Executes a function with a temporary <see cref="SKCanvas"/> targeting the <see cref="PixData"/> and returns its result.
    /// </summary>
    public static T WithCanvasView<T>(PixData pixData, Func<SKBitmap, SKCanvas, T> func, SKAlphaType? alphaType = null)
    {
        ArgumentNullException.ThrowIfNull(func);

        using SKBitmap bitmap = CreateBitmapView(pixData, alphaType);
        using SKCanvas canvas = new SKCanvas(bitmap);

        T result = func(bitmap, canvas);
        canvas.Flush();

        return result;
    }
    
    /// <summary>
    /// Creates a new <see cref="SKBitmap"/> by copying the pixels from <see cref="PixData"/>.
    /// </summary>
    public static SKBitmap CreateBitmapCopy(PixData pixData, SKAlphaType? alphaType = null)
    {
        using SKBitmap view = CreateBitmapView(pixData, alphaType);

        SKBitmap? copy = view.Copy();
        if (copy == null)
        {
            throw new InvalidOperationException(
                $"Failed to copy PixData into an owning SKBitmap for format '{pixData.pix_format}'.");
        }

        return copy;
    }
    
    public static SKBitmap CreateBitmapCopy(
        PixData pixData,
        string fallbackPixFormat,
        bool strict = false,
        SKAlphaType? alphaType = null)
    {
        if (TryGetImageInfo(pixData, out _, alphaType))
        {
            return CreateBitmapCopy(pixData, alphaType);
        }

        if (strict)
        {
            throw new NotSupportedException(
                $"PixData format '{pixData.pix_format}' cannot be viewed as a Skia bitmap and strict mode is enabled.");
        }

        PixData proxy = Pixels.ConvertPixelsPix(ref pixData, fallbackPixFormat);
        if (!proxy)
        {
            throw new NotSupportedException(
                $"PixData format '{pixData.pix_format}' cannot be converted to '{fallbackPixFormat}'.");
        }

        try
        {
            return CreateBitmapCopy(proxy, alphaType);
        }
        finally
        {
            proxy.Dispose();
        }
    }

    /// <summary>
    /// Converts <see cref="PixData"/> to an owning <see cref="SKBitmap"/>.
    /// </summary>
    public static SKBitmap ToBitmap(this PixData pixData, SKAlphaType? alphaType = null)
    {
        return CreateBitmapCopy(pixData, alphaType);
    }

    /// <summary>
    /// Creates a <see cref="PixData"/> view of the <see cref="SKBitmap"/>.
    /// </summary>
    public static PixData ToPixDataView(this SKBitmap bitmap)
    {
        return CreatePixDataView(bitmap);
    }

    public static bool TryCreateBitmapCopy(PixData pixData, out SKBitmap? bitmap, SKAlphaType? alphaType = null)
    {
        bitmap = null;

        try
        {
            bitmap = CreateBitmapCopy(pixData, alphaType);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Creates a new <see cref="SKImage"/> by copying the pixels from <see cref="PixData"/>.
    /// </summary>
    public static SKImage CreateImageCopy(
        PixData pixData,
        SKAlphaType? alphaType = null)
    {
        using SKBitmap bitmap = CreateBitmapCopy(pixData, alphaType);
        return SKImage.FromBitmap(bitmap);
    }

    public static bool TryCreateImageCopy(
        PixData pixData,
        out SKImage? image,
        SKAlphaType? alphaType = null)
    {
        image = null;
        try
        {
            image = CreateImageCopy(pixData, alphaType);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Creates <see cref="PixData"/> from an <see cref="SKBitmap"/>.
    /// </summary>
    public static PixData FromBitmap(
        SKBitmap bitmap,
        string? pixFormatDest = null,
        bool strict = false,
        SKAlphaType? alphaType = null)
    {
        return FixedPixDataRenderer.CreateFixed(
            bitmap.Width,
            bitmap.Height,
            (canvas, info) =>
            {
                canvas.DrawBitmap(bitmap, 0, 0);
            },
            pixFormatDest,
            strict,
            alphaType);
    }

    public static PixData ConvertBySkia(
        PixData source,
        SKImageInfo destInfo,
        string? pixFormatDest = null,
        bool strict = false,
        SKAlphaType? sourceAlphaType = null)
    {
        using SKImage image = CreateImageCopy(source, sourceAlphaType);
        using SKBitmap converted = SkiaConvert.ToBitmap(image, destInfo);
        return FromBitmap(converted, pixFormatDest, strict, destInfo.AlphaType);
    }

    public static bool TryConvertBySkia(
        PixData source,
        SKImageInfo destInfo,
        out PixData result,
        string? pixFormatDest = null,
        bool strict = false,
        SKAlphaType? sourceAlphaType = null)
    {
        try
        {
            result = ConvertBySkia(source, destInfo, pixFormatDest, strict, sourceAlphaType);
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    public static PixData ToPremul(
        PixData source,
        string? pixFormatDest = null,
        bool strict = false,
        SKAlphaType? sourceAlphaType = null)
    {
        SKImageInfo srcInfo = GetImageInfo(source, sourceAlphaType);
        var destInfo = new SKImageInfo(
            srcInfo.Width,
            srcInfo.Height,
            srcInfo.ColorType,
            SKAlphaType.Premul,
            srcInfo.ColorSpace);

        return ConvertBySkia(source, destInfo, pixFormatDest, strict, sourceAlphaType);
    }

    public static PixData ToUnpremul(
        PixData source,
        string? pixFormatDest = null,
        bool strict = false,
        SKAlphaType? sourceAlphaType = null)
    {
        SKImageInfo srcInfo = GetImageInfo(source, sourceAlphaType);
        var destInfo = new SKImageInfo(
            srcInfo.Width,
            srcInfo.Height,
            srcInfo.ColorType,
            SKAlphaType.Unpremul,
            srcInfo.ColorSpace);

        return ConvertBySkia(source, destInfo, pixFormatDest, strict, sourceAlphaType);
    }

    public static PixData ToOpaque(
        PixData source,
        string? pixFormatDest = null,
        bool strict = false,
        SKAlphaType? sourceAlphaType = null)
    {
        SKImageInfo srcInfo = GetImageInfo(source, sourceAlphaType);
        var destInfo = new SKImageInfo(
            srcInfo.Width,
            srcInfo.Height,
            srcInfo.ColorType,
            SKAlphaType.Opaque,
            srcInfo.ColorSpace);

        return ConvertBySkia(source, destInfo, pixFormatDest, strict, sourceAlphaType);
    }

    public static bool TryToPremul(
        PixData source,
        out PixData result,
        string? pixFormatDest = null,
        bool strict = false,
        SKAlphaType? sourceAlphaType = null)
    {
        try
        {
            result = ToPremul(source, pixFormatDest, strict, sourceAlphaType);
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    public static bool TryToUnpremul(
        PixData source,
        out PixData result,
        string? pixFormatDest = null,
        bool strict = false,
        SKAlphaType? sourceAlphaType = null)
    {
        try
        {
            result = ToUnpremul(source, pixFormatDest, strict, sourceAlphaType);
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }
}