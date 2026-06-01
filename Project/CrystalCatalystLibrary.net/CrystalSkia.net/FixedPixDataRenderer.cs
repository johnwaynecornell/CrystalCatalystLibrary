using System;
using System.Runtime.InteropServices;
using SkiaSharp;
using CrystalCatalystLibrary.net;

namespace CrystalSkia.net;

public static class FixedPixDataRenderer
{
    private static readonly PixData.Pix_data_free SafeFreeDelegate = FreeUnmanagedPixels;

    private static bool FreeUnmanagedPixels(IntPtr pixdata)
    {
        if (pixdata != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(pixdata);
        }
        return true;
    }

    public static PixData CreateFixed(
        int width,
        int height,
        Action<SKCanvas, SKImageInfo>? draw = null)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentException("Width and height must be positive.");
        }

        int stride = width * 4;
        int byteCount = stride * height;
        IntPtr unmanagedPixels = Marshal.AllocHGlobal(byteCount);

        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

        using var bitmap = new SKBitmap();
        bitmap.InstallPixels(info, unmanagedPixels, stride);

        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.Transparent);
            draw?.Invoke(canvas, info);
            canvas.Flush();
        }

        var pixData = new PixData
        {
            width = width,
            height = height,
            pix_format = "bgra:int8",
            pix_data = unmanagedPixels,
            pix_data_length = (IntPtr)byteCount,
            pix_data_free = SafeFreeDelegate
        };

        return pixData;
    }

    public static PixData CreateDemoTexture(int width = 256, int height = 256)
    {
        return CreateFixed(width, height, (canvas, info) =>
        {
            // A dark background
            canvas.Clear(new SKColor(30, 30, 30));

            // A bright border
            using var paint = new SKPaint
            {
                Color = SKColors.Cyan,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 4
            };
            canvas.DrawRect(2, 2, width - 4, height - 4, paint);

            // Overlapping colored circles
            paint.Style = SKPaintStyle.Fill;
            paint.Color = SKColors.Red.WithAlpha(180);
            canvas.DrawCircle(width * 0.4f, height * 0.4f, width * 0.2f, paint);

            paint.Color = SKColors.Green.WithAlpha(180);
            canvas.DrawCircle(width * 0.6f, height * 0.4f, width * 0.2f, paint);

            paint.Color = SKColors.Blue.WithAlpha(180);
            canvas.DrawCircle(width * 0.5f, height * 0.6f, width * 0.2f, paint);

            // A diagonal line or accent shape
            paint.Color = SKColors.Yellow;
            paint.StrokeWidth = 2;
            paint.Style = SKPaintStyle.Stroke;
            canvas.DrawLine(0, 0, width, height, paint);

            // Text such as Crystal Skia
            using var textPaint = new SKPaint
            {
                Color = SKColors.White,
                IsAntialias = true
            };
            using var font = new SKFont(SKTypeface.Default, 32);
            canvas.DrawText("Crystal Skia", width / 2f, height / 2f + 16, SKTextAlign.Center, font, textPaint);
        });
    }
}
