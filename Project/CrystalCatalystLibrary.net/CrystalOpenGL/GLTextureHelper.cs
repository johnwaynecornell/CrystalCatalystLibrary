using System;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;
using CrystalCatalystLibrary.net;

namespace CrystalOpenGL;

/// <summary>
/// Provides utility methods for working with OpenGL textures and <see cref="PixData"/>.
/// </summary>
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
    
    /// <summary>
    /// Gets the width and height of an OpenGL texture.
    /// </summary>
    public static void GetTextureDimensions(GL gl, uint texture, out int width, out int height)
    {
        if (GLHelper.VersionCompare(gl, 4, 5) >= 0)
        {
            gl.GetTextureLevelParameter(texture, 0, GetTextureParameter.TextureWidth, out width);
            gl.GetTextureLevelParameter(texture, 0, GetTextureParameter.TextureHeight, out height);
        }
        else
        {
            gl.BindTexture(TextureTarget.Texture2D, texture);
            gl.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureWidth, out width);
            gl.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureHeight, out height);
        }
    }

    /// <summary>
    /// Creates a 2D OpenGL texture from the provided <see cref="PixData"/>.
    /// </summary>
    public static uint CreateTexture2DFromPixData(GL gl, PixData pixData, bool generateMipmaps = false, bool strict = false)
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
            if (strict)
            {
                throw new NotSupportedException($"Pixel format '{format}' is not directly supported by OpenGL texture upload and strict mode is enabled.");
            }

            string fallbackFormat = GLPixFormatMap.GetFallbackUploadFormat(format);
            proxy = Pixels.ConvertPixelsPix(ref pixData, fallbackFormat);
            if (!proxy) throw new NotSupportedException($"Format '{format}' is not supported and fallback conversion to '{fallbackFormat}' failed.");
            
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

    /// <summary>
    /// Updates an existing OpenGL texture with data from <see cref="PixData"/>.
    /// </summary>
    public static void WritePixels(GL gl, uint texture, PixData src, bool strict = false)
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
            if (strict)
            {
                throw new NotSupportedException($"Pixel format '{format}' is not directly supported by OpenGL texture upload and strict mode is enabled.");
            }

            string fallbackFormat = GLPixFormatMap.GetFallbackUploadFormat(format);
            proxy = Pixels.ConvertPixelsPix(ref src, fallbackFormat);
            if (!proxy) throw new NotSupportedException($"Format '{format}' is not supported and fallback conversion to '{fallbackFormat}' failed.");
            
            GLPixFormatMap.TryGetUploadFormat(fallbackFormat, out _, out pixelFormat, out pixelType);
            src = proxy;
        }

        GLBridges.TexSubImage2D(gl, TextureTarget.Texture2D, 0, 0, 0, (uint)src.width, (uint)src.height, pixelFormat, pixelType, src.pix_data);

        if (proxy) proxy.Dispose();
    }

    /// <summary>
    /// Infers a PixData format from the OpenGL texture internal format.
    /// Note: this cannot recover the original external upload channel order.
    /// For example, both bgra:int8 and rgba:int8 commonly resolve to Rgba8
    /// internally, so this method returns a canonical readback format.
    /// </summary>
    public static string GetCanonicalPixFormatFromTexture(GL gl, uint texture)
    {
        gl.BindTexture(TextureTarget.Texture2D, texture);
        gl.GetTexLevelParameter(TextureTarget.Texture2D, 0, GLEnum.TextureInternalFormat, out int internalFormatInt);
        
        if (GLPixFormatMap.TryGetPixFormat((InternalFormat)internalFormatInt, out string pixFormat))
        {
            return pixFormat;
        }
        
        return "rgba:int8";
    }

    /// <summary>
    /// Reads pixels from an OpenGL texture into a new <see cref="PixData"/> buffer.
    /// </summary>
    public static PixData ReadPixels(GL gl, uint texture, string? pixFormatDest = null, bool strict = false)
    {
        if (string.IsNullOrEmpty(pixFormatDest))
        {
            pixFormatDest = GetCanonicalPixFormatFromTexture(gl, texture);
        }

        gl.BindTexture(TextureTarget.Texture2D, texture);
        
        GLTextureHelper.GetTextureDimensions(gl,texture, out int width, out int height);

        if (width <= 0 || height <= 0)
            throw new Exception("Failed to retrieve texture dimensions or texture is empty.");

        string destFormat = PixFormats.Parse(pixFormatDest).PixFormat;

        // Try to read directly in the requested format if supported by GLPixFormatMap
        string readFormat = destFormat;
        if (!GLPixFormatMap.TryGetUploadFormat(readFormat, out _, out var glFormat, out var pixelType))
        {
            if (strict)
            {
                throw new NotSupportedException($"Pixel format '{destFormat}' is not directly supported by OpenGL texture readback and strict mode is enabled.");
            }

            readFormat = GLPixFormatMap.GetFallbackUploadFormat(destFormat);
            if (!GLPixFormatMap.TryGetUploadFormat(readFormat, out _, out glFormat, out pixelType))
                throw new Exception($"Unexpected: {readFormat} not found in GLPixFormatMap");
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
