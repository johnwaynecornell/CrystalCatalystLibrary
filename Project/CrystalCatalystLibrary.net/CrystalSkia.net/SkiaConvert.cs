using SkiaSharp;

namespace CrystalSkia.net;

public static class SkiaConvert
{
    public static SKBitmap ToBitmap(SKImage source, SKImageInfo destInfo)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (destInfo.Width <= 0 || destInfo.Height <= 0)
            throw new ArgumentException("Destination image info must have positive dimensions.", nameof(destInfo));

        if (!TryToBitmap(source, destInfo, out var bitmap))
            throw new InvalidOperationException("Failed to convert SKImage to destination SKBitmap.");

        return bitmap!;
    }

    public static bool TryToBitmap(
        SKImage source,
        SKImageInfo destInfo,
        out SKBitmap? bitmap)
    {
        bitmap = null;

        try
        {
            ArgumentNullException.ThrowIfNull(source);

            if (destInfo.Width <= 0 || destInfo.Height <= 0)
                return false;

            bitmap = new SKBitmap(destInfo);

            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.Transparent);

            using var paint = new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High
            };

            var destRect = new SKRect(0, 0, destInfo.Width, destInfo.Height);
            canvas.DrawImage(source, destRect, paint);
            canvas.Flush();

            return true;
        }
        catch
        {
            bitmap?.Dispose();
            bitmap = null;
            return false;
        }
    }

    public static SKBitmap ToPremul(
        SKImage source,
        SKColorType? colorType = null,
        SKColorSpace? colorSpace = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        var destInfo = new SKImageInfo(
            source.Width,
            source.Height,
            colorType ?? source.ColorType,
            SKAlphaType.Premul,
            colorSpace ?? source.ColorSpace);
        return ToBitmap(source, destInfo);
    }

    public static SKBitmap ToUnpremul(
        SKImage source,
        SKColorType? colorType = null,
        SKColorSpace? colorSpace = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        var destInfo = new SKImageInfo(
            source.Width,
            source.Height,
            colorType ?? source.ColorType,
            SKAlphaType.Unpremul,
            colorSpace ?? source.ColorSpace);
        return ToBitmap(source, destInfo);
    }

    public static SKBitmap ToOpaque(
        SKImage source,
        SKColorType? colorType = null,
        SKColorSpace? colorSpace = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        var destInfo = new SKImageInfo(
            source.Width,
            source.Height,
            colorType ?? source.ColorType,
            SKAlphaType.Opaque,
            colorSpace ?? source.ColorSpace);
        return ToBitmap(source, destInfo);
    }

    public static bool TryToPremul(
        SKImage source,
        out SKBitmap? bitmap,
        SKColorType? colorType = null,
        SKColorSpace? colorSpace = null)
    {
        bitmap = null;
        try
        {
            ArgumentNullException.ThrowIfNull(source);
            var destInfo = new SKImageInfo(
                source.Width,
                source.Height,
                colorType ?? source.ColorType,
                SKAlphaType.Premul,
                colorSpace ?? source.ColorSpace);
            return TryToBitmap(source, destInfo, out bitmap);
        }
        catch
        {
            return false;
        }
    }

    public static bool TryToUnpremul(
        SKImage source,
        out SKBitmap? bitmap,
        SKColorType? colorType = null,
        SKColorSpace? colorSpace = null)
    {
        bitmap = null;
        try
        {
            ArgumentNullException.ThrowIfNull(source);
            var destInfo = new SKImageInfo(
                source.Width,
                source.Height,
                colorType ?? source.ColorType,
                SKAlphaType.Unpremul,
                colorSpace ?? source.ColorSpace);
            return TryToBitmap(source, destInfo, out bitmap);
        }
        catch
        {
            return false;
        }
    }
}
