using CrystalCatalystLibrary.net;
using SkiaSharp;

namespace CrystalSkia.net;

public static class PixDataSkia
{
    public static bool TryGetImageInfo(PixData pixData, out SKImageInfo info)
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
            out info);
    }

    public static SKImageInfo GetImageInfo(PixData pixData)
    {
        if (TryGetImageInfo(pixData, out var info))
            return info;

        string format = pixData.pix_format.ToString() ?? "";
        throw new NotSupportedException(
            $"PixData format '{format}' cannot be viewed as a Skia bitmap.");
    }

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

    public static SKBitmap CreateBitmapView(PixData pixData)
    {
        if (!pixData)
            throw new ArgumentException("PixData has no pixel buffer.", nameof(pixData));

        SKImageInfo info = GetImageInfo(pixData);
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

    public static void WithBitmapView(PixData pixData, Action<SKBitmap> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        using SKBitmap bitmap = CreateBitmapView(pixData);
        action(bitmap);
    }

    public static T WithBitmapView<T>(PixData pixData, Func<SKBitmap, T> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        using SKBitmap bitmap = CreateBitmapView(pixData);
        return func(bitmap);
    }

    public static void WithCanvasView(PixData pixData, Action<SKBitmap, SKCanvas> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        using SKBitmap bitmap = CreateBitmapView(pixData);
        using SKCanvas canvas = new SKCanvas(bitmap);

        action(bitmap, canvas);
        canvas.Flush();
    }

    public static T WithCanvasView<T>(PixData pixData, Func<SKBitmap, SKCanvas, T> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        using SKBitmap bitmap = CreateBitmapView(pixData);
        using SKCanvas canvas = new SKCanvas(bitmap);

        T result = func(bitmap, canvas);
        canvas.Flush();

        return result;
    }
    
}