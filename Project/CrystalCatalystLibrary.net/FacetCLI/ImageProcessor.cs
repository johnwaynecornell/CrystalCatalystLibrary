// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
using CrystalCatalystLibrary.net;
using SkiaSharp;

namespace CrystalCatalyst.Optics.FacetCLI;

public static class ImageProcessor
{
    /// <summary>
    /// Encodes a PixData capture to the requested format, applying optional
    /// grayscale conversion and bounds crop.
    /// </summary>
    public static byte[] Process(
        PixData pix,
        string  format,
        int     quality,
        bool    grayscale,
        int[]?  bounds)   // [x, y, w, h] or null
    {
        // Build SKBitmap from PixData (assumed bgra:int8)
        using var bmp = PixDataToBitmap(pix);

        SKBitmap working = bmp;
        bool ownWorking = false;

        try
        {
            // Crop to --bounds if specified
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

            // Grayscale conversion
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
        var info = new SKImageInfo(pix.width, pix.height, SKColorType.Bgra8888, SKAlphaType.Opaque);
        // InstallPixels points into native PixData memory — copy immediately so
        // we can dispose PixData independently.
        using var view = new SKBitmap();
        view.InstallPixels(info, pix.pix_data, pix.width * 4);
        return view.Copy()!;
    }

    private static (SKEncodedImageFormat format, int quality) ResolveEncoding(
        string formatName, int quality)
    {
        return formatName.ToLowerInvariant() switch
        {
            "png"  => (SKEncodedImageFormat.Png,  100),
            "jpeg" => (SKEncodedImageFormat.Jpeg, quality),
            "jpg"  => (SKEncodedImageFormat.Jpeg, quality),
            _      => (SKEncodedImageFormat.Webp, quality),  // default: webp
        };
    }
}
