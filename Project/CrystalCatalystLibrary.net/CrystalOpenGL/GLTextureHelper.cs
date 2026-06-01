using System;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;
using CrystalCatalystLibrary.net;

namespace CrystalOpenGL;

public static class GLTextureHelper
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

    public static uint CreateTexture2DFromPixData(GL gl, PixData pixData, bool generateMipmaps = false)
    {
        if (pixData.pix_data == IntPtr.Zero)
            throw new ArgumentException("PixData.pix_data is zero.");

        if (pixData.width <= 0 || pixData.height <= 0)
            throw new ArgumentException("PixData width and height must be positive.");

        string format = (string) pixData.pix_format ?? "";
        PixData proxy = default;

        if (!GLPixFormatMap.TryGetUploadFormat(format, out var internalFormat, out var pixelFormat, out var pixelType))
        {
            proxy = Pixels.ConvertPixelsPix(ref pixData, "rgba:int8");
            if (!proxy) throw new NotSupportedException($"Format '{format}' is not supported and fallback conversion failed.");
            
            if (!GLPixFormatMap.TryGetUploadFormat("rgba:int8", out internalFormat, out pixelFormat, out pixelType))
                throw new Exception("Unexpected: rgba:int8 not found in GLPixFormatMap");
            
            pixData = proxy;
        }

        uint texture = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, texture);

        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

        GLBridges.TexImage2D(gl, TextureTarget.Texture2D, 0, internalFormat, (uint)pixData.width, (uint)pixData.height, 0, pixelFormat, pixelType, pixData.pix_data);

        if (generateMipmaps)
        {
            gl.GenerateMipmap(TextureTarget.Texture2D);
        }

        if (proxy) proxy.Dispose();
        
        return texture;
    }

    public static void WritePixels(GL gl, uint texture, PixData src)
    {
        if (src.pix_data == IntPtr.Zero)
            throw new ArgumentException("PixData.pix_data is zero.");

        if (src.width <= 0 || src.height <= 0)
            throw new ArgumentException("PixData width and height must be positive.");

        gl.BindTexture(TextureTarget.Texture2D, texture);
        gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

        string format = (string) src.pix_format ?? "";
        PixData proxy = default;

        if (!GLPixFormatMap.TryGetUploadFormat(format, out _, out var pixelFormat, out var pixelType))
        {
            proxy = Pixels.ConvertPixelsPix(ref src, "rgba:int8");
            if (!proxy) throw new NotSupportedException($"Format '{format}' is not supported and fallback conversion failed.");
            
            GLPixFormatMap.TryGetUploadFormat("rgba:int8", out _, out pixelFormat, out pixelType);
            src = proxy;
        }

        GLBridges.TexSubImage2D(gl, TextureTarget.Texture2D, 0, 0, 0, (uint)src.width, (uint)src.height, pixelFormat, pixelType, src.pix_data);

        if (proxy) proxy.Dispose();
    }

    public static PixData ReadPixels(GL gl, uint texture)
    {
        gl.BindTexture(TextureTarget.Texture2D, texture);

        gl.GetTexLevelParameter(TextureTarget.Texture2D, 0, GLEnum.TextureWidth, out int width);
        gl.GetTexLevelParameter(TextureTarget.Texture2D, 0, GLEnum.TextureHeight, out int height);
        gl.GetTexLevelParameter(TextureTarget.Texture2D, 0, GLEnum.TextureInternalFormat, out int internalFormat);

        if (width <= 0 || height <= 0)
            throw new Exception("Failed to retrieve texture dimensions or texture is empty.");

        // Infer pixformat
        string pixFormat = "bgra:int8";
        PixelFormat glFormat = PixelFormat.Bgra;

        int byteCount = PixFormats.GetExpectedByteLength(pixFormat, width, height);
        IntPtr unmanagedPixels = Marshal.AllocHGlobal(byteCount);

        gl.PixelStore(PixelStoreParameter.PackAlignment, 1);
        GLBridges.GetTexImage(gl, TextureTarget.Texture2D, 0, glFormat, PixelType.UnsignedByte, unmanagedPixels);

        var pixData = new PixData
        {
            width = width,
            height = height,
            pix_format = pixFormat,
            pix_data = unmanagedPixels,
            pix_data_length = (IntPtr)byteCount,
            pix_data_free = SafeFreeDelegate
        };

        return pixData;
    }
}
