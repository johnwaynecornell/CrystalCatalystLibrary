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

        string formatStr = pixData.pix_format.ToString();
        if (string.IsNullOrEmpty(formatStr)) formatStr = "rgba:int8";
        string format = PixFormats.Parse(formatStr).PixFormat;
        PixData proxy = default;

        if (!GLPixFormatMap.TryGetUploadFormat(format, out var internalFormat, out var pixelFormat, out var pixelType))
        {
            string fallbackFormat = "rgba:int8";
            proxy = Pixels.ConvertPixelsPix(ref pixData, fallbackFormat);
            if (!proxy) throw new NotSupportedException($"Format '{format}' is not supported and fallback conversion failed.");
            
            if (!GLPixFormatMap.TryGetUploadFormat(fallbackFormat, out internalFormat, out pixelFormat, out pixelType))
                throw new Exception($"Unexpected: {fallbackFormat} not found in GLPixFormatMap");
            
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

        string formatStr = src.pix_format.ToString();
        if (string.IsNullOrEmpty(formatStr)) formatStr = "rgba:int8";
        string format = PixFormats.Parse(formatStr).PixFormat;
        PixData proxy = default;

        if (!GLPixFormatMap.TryGetUploadFormat(format, out _, out var pixelFormat, out var pixelType))
        {
            string fallbackFormat = "rgba:int8";
            proxy = Pixels.ConvertPixelsPix(ref src, fallbackFormat);
            if (!proxy) throw new NotSupportedException($"Format '{format}' is not supported and fallback conversion failed.");
            
            GLPixFormatMap.TryGetUploadFormat(fallbackFormat, out _, out pixelFormat, out pixelType);
            src = proxy;
        }

        GLBridges.TexSubImage2D(gl, TextureTarget.Texture2D, 0, 0, 0, (uint)src.width, (uint)src.height, pixelFormat, pixelType, src.pix_data);

        if (proxy) proxy.Dispose();
    }

    public static string GetPixFormatFromTexture(GL gl, uint texture)
    {
        gl.BindTexture(TextureTarget.Texture2D, texture);
        gl.GetTexLevelParameter(TextureTarget.Texture2D, 0, GLEnum.TextureInternalFormat, out int internalFormatInt);
        
        if (GLPixFormatMap.TryGetPixFormat((InternalFormat)internalFormatInt, out string pixFormat))
        {
            return pixFormat;
        }
        
        return "rgba:int8";
    }

    public static PixData ReadPixels(GL gl, uint texture, string? pixFormatDest = null)
    {
        if (string.IsNullOrEmpty(pixFormatDest))
        {
            pixFormatDest = GetPixFormatFromTexture(gl, texture);
        }

        gl.BindTexture(TextureTarget.Texture2D, texture);

        gl.GetTexLevelParameter(TextureTarget.Texture2D, 0, GLEnum.TextureWidth, out int width);
        gl.GetTexLevelParameter(TextureTarget.Texture2D, 0, GLEnum.TextureHeight, out int height);

        if (width <= 0 || height <= 0)
            throw new Exception("Failed to retrieve texture dimensions or texture is empty.");

        string destFormat = PixFormats.Parse(pixFormatDest).PixFormat;

        // Try to read directly in the requested format if supported by GLPixFormatMap
        string readFormat = destFormat;
        if (!GLPixFormatMap.TryGetUploadFormat(readFormat, out _, out var glFormat, out var pixelType))
        {
             // Fallback to RGBA or BGRA
             readFormat = "rgba:int8";
             glFormat = PixelFormat.Rgba;
             pixelType = PixelType.UnsignedByte;
             
             // If dest is bgra, use bgra as middleman
             if (destFormat == "bgra:int8")
             {
                 readFormat = "bgra:int8";
                 glFormat = PixelFormat.Bgra;
             }
        }

        int byteCount = PixFormats.GetExpectedByteLength(readFormat, width, height);
        IntPtr unmanagedPixels = Marshal.AllocHGlobal(byteCount);

        gl.PixelStore(PixelStoreParameter.PackAlignment, 1);
        GLBridges.GetTexImage(gl, TextureTarget.Texture2D, 0, glFormat, pixelType, unmanagedPixels);

        var pixData = new PixData
        {
            width = width,
            height = height,
            pix_format = readFormat,
            pix_data = unmanagedPixels,
            pix_data_length = (IntPtr)byteCount,
            pix_data_free = SafeFreeDelegate
        };

        if (readFormat != destFormat)
        {
            var proxy = Pixels.ConvertPixelsPix(ref pixData, destFormat);
            if (proxy)
            {
                pixData.Dispose();
                return proxy;
            }
        }

        return pixData;
    }
}
