// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
using CrystalCatalystLibrary.net;
using SkiaSharp;

namespace CrystalCatalyst.Optics.FacetCLI;

public static class ImageProcessor
{
    public static byte[] Process(
        PixData pix,
        string  format,
        int     quality,
        bool    grayscale,
        int[]?  bounds)
    {
        using var bmp = PixDataToBitmap(pix);
        SKBitmap working = bmp;
        bool ownWorking = false;

        try
        {
            if (bounds != null && bounds.Length == 4)
            {
                int bx = Math.Clamp(bounds[0], 0, bmp.Width  - 1);
                int by = Math.Clamp(bounds[1], 0, bmp.Height - 1);
                int bw = Math.Clamp(bounds[2], 1, bmp.Width  - bx);
                int bh = Math.Clamp(bounds[3], 1, bmp.Height - by);
                working = new SKBitmap(bw, bh);
                ownWorking = true;
                using var canvas = new SKCanvas(working);
                canvas.DrawBitmap(bmp, SKRect.Create(bx, by, bw, bh),
                                       SKRect.Create(0,  0,  bw, bh));
            }

            if (grayscale)
            {
                var gray = new SKBitmap(working.Width, working.Height,
                                        SKColorType.Gray8, SKAlphaType.Opaque);
                using var canvas = new SKCanvas(gray);
                using var paint  = new SKPaint();
                paint.ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
                {
                    0.299f, 0.587f, 0.114f, 0, 0,
                    0.299f, 0.587f, 0.114f, 0, 0,
                    0.299f, 0.587f, 0.114f, 0, 0,
                    0,      0,      0,      1, 0
                });
                canvas.DrawBitmap(working, 0, 0, paint);
                if (ownWorking) working.Dispose();
                working = gray;
                ownWorking = true;
            }

            using var image = SKImage.FromBitmap(working);
            var (skFormat, skQuality) = ResolveEncoding(format, quality);
            using var data = image.Encode(skFormat, skQuality);
            return data.ToArray();
        }
        finally
        {
            if (ownWorking) working.Dispose();
        }
    }

    private static SKBitmap PixDataToBitmap(PixData pix)
    {
        var info = new SKImageInfo(pix.width, pix.height,
                                   SKColorType.Bgra8888, SKAlphaType.Opaque);
        using var view = new SKBitmap();
        view.InstallPixels(info, pix.pix_data, pix.width * 4);
        return view.Copy()!;
    }

    private static (SKEncodedImageFormat, int) ResolveEncoding(string name, int quality) =>
        name.ToLowerInvariant() switch
        {
            "png"  => (SKEncodedImageFormat.Png,  100),
            "jpeg" => (SKEncodedImageFormat.Jpeg, quality),
            "jpg"  => (SKEncodedImageFormat.Jpeg, quality),
            _      => (SKEncodedImageFormat.Webp, quality),
        };
}
