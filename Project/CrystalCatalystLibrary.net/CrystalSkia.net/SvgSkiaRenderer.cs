using System;
using System.Diagnostics;
using SkiaSharp;
using Svg.Skia;
using CrystalCatalystLibrary.net;
using System.Runtime.InteropServices;

namespace CrystalSkia.net;

public class SvgSkiaRenderer
{
    public SKMatrix Matrix { get; set; } = SKMatrix.Identity;
    public bool Crop { get; set; } = true;
    public SKSize? Size { get; set; }
        
    public int Border { get; set; } = 8;

    public SKSize Measure(Svg.Skia.SKSvg svg)
    {
        if (svg.Picture == null) return SKSize.Empty;
        var bounds = svg.Picture.CullRect;
        var transformedBounds = Matrix.MapRect(bounds);
        return new SKSize(transformedBounds.Width, transformedBounds.Height);
    }

    public void Render(Svg.Skia.SKSvg svg, SKCanvas canvas)
    {
        if (svg.Picture == null) return;
        
        canvas.Save();
        var matrix = Matrix;
        canvas.Concat(ref matrix);
        canvas.DrawPicture(svg.Picture);
        canvas.Restore();
    }
    
    float translateX, translateY;

    public SKPoint TranslateHotSpot(SKPoint point)
    {
        return Matrix.MapPoint(point) + new SKPoint(translateX, translateY);
    }

    // 1. Define a STATIC delegate at the class level to prevent Garbage Collection

    private static readonly PixData.Pix_data_free SafeFreeDelegate = FreeUnmanagedPixels;

    private static bool FreeUnmanagedPixels(IntPtr pixdata)
    {
        if (pixdata != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(pixdata);
        }
        return true;
    }

    public PixData RenderPix(Svg.Skia.SKSvg svg, Action<SKBitmap, SKCanvas> onCanvas = null)
    {
        if (svg.Picture == null) return default;
        var bounds = svg.Picture.CullRect;
        var transformedBounds = Matrix.MapRect(bounds);

        int width, height;

        if (Size.HasValue)
        {
            width = (int)Math.Ceiling(Size.Value.Width);
            height = (int)Math.Ceiling(Size.Value.Height);
            translateX = Crop ? -transformedBounds.Left : 0;
            translateY = Crop ? -transformedBounds.Top : 0;
        }
        else if (Crop)
        {
            width = (int)Math.Ceiling(transformedBounds.Width) + (Border<<1);
            height = (int)Math.Ceiling(transformedBounds.Height) + (Border<<1);
            translateX = -transformedBounds.Left +  Border;
            translateY = -transformedBounds.Top  + Border;
        }
        else
        {
            width = (int)Math.Ceiling(bounds.Width);
            height = (int)Math.Ceiling(bounds.Height);
            translateX = 0;
            translateY = 0;
        }

        if (width <= 0) width = 1;
        if (height <= 0) height = 1;

        // 2. Force the stride to be EXACTLY width * 4 (No Skia SIMD padding)
        int stride = width * 4;
        int byteCount = stride * height;

        // 3. Allocate unmanaged memory directly
        IntPtr unmanagedPixels = Marshal.AllocHGlobal(byteCount);

        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

        // 4. Wrap Skia around our strictly-sized unmanaged memory
        using var bitmap = new SKBitmap();
        bitmap.InstallPixels(info, unmanagedPixels, stride);
        
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        onCanvas?.Invoke(bitmap, canvas);

        canvas.Translate(translateX, translateY);
        Render(svg, canvas);
        canvas.Flush();

        var pixData = new PixData();
        pixData.width = width;
        pixData.height = height;
        pixData.pix_format = "bgra:int8";
        
        // Pass the unmanaged pointer directly to C++
        pixData.pix_data = unmanagedPixels;
        pixData.pix_data_length = (IntPtr)byteCount;
        
        // 5. Use the static delegate so C# doesn't trash it before C++ calls it
        pixData.pix_data_free = SafeFreeDelegate;

        return pixData;
    }

}
