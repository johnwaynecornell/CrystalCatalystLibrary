using System;
using System.IO;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace ClipFlow.Format;

public static class BmpEncoder
{
    public static byte[]? EncodeToBmp(SKImage? image)
    {
        if (image == null) return null;
        int width = image.Width;
        int height = image.Height;
        if (width <= 0 || height <= 0) return null;

        int rowStride = width * 4;
        int imageSize = rowStride * height;
        int fileSize = 54 + imageSize;

        byte[] rawPixels = new byte[imageSize];
        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

        IntPtr ptr = Marshal.AllocHGlobal(imageSize);
        try
        {
            bool ok = image.ReadPixels(info, ptr, rowStride, 0, 0);
            if (ok)
            {
                Marshal.Copy(ptr, rawPixels, 0, imageSize);
            }
            else
            {
                using var bitmap = new SKBitmap(info);
                using var canvas = new SKCanvas(bitmap);
                canvas.Clear(SKColors.Transparent);
                canvas.DrawImage(image, 0, 0);
                canvas.Flush();
                Marshal.Copy(bitmap.GetPixels(), rawPixels, 0, imageSize);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }

        byte[] bmpBytes = new byte[fileSize];
        using var ms = new MemoryStream(bmpBytes);
        using var bw = new BinaryWriter(ms);

        // BITMAPFILEHEADER (14 bytes)
        bw.Write((byte)'B');
        bw.Write((byte)'M');
        bw.Write(fileSize);
        bw.Write((short)0); // reserved1
        bw.Write((short)0); // reserved2
        bw.Write(54); // data offset

        // BITMAPINFOHEADER (40 bytes)
        bw.Write(40); // header size
        bw.Write(width);
        bw.Write(height); // positive for bottom-up DIB
        bw.Write((short)1); // planes
        bw.Write((short)32); // bit count (BGRA 32bpp)
        bw.Write(0); // compression (BI_RGB)
        bw.Write(imageSize);
        bw.Write(2835); // horizontal resolution (~72 dpi)
        bw.Write(2835); // vertical resolution (~72 dpi)
        bw.Write(0); // colors used
        bw.Write(0); // colors important

        // Write rows bottom-up
        for (int y = height - 1; y >= 0; y--)
        {
            bw.Write(rawPixels, y * rowStride, rowStride);
        }

        return bmpBytes;
    }
}
