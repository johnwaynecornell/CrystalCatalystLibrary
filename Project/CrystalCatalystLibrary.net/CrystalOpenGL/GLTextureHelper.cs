using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;
using CrystalCatalystLibrary.net;

namespace CrystalOpenGL;

public static class GLTextureHelper
{
    private delegate void TexImage2DDelegate(GL gl, TextureTarget target, int level, InternalFormat internalformat, uint width, uint height, int border, PixelFormat format, PixelType type, IntPtr pixels);
    private static readonly TexImage2DDelegate _texImage2D;

    private delegate void GetTexImageDelegate(GL gl, TextureTarget target, int level, PixelFormat format, PixelType type, IntPtr pixels);
    private static readonly GetTexImageDelegate _getTexImage;

    private delegate void TexSubImage2DDelegate(GL gl, TextureTarget target, int level, int xoffset, int yoffset, uint width, uint height, PixelFormat format, PixelType type, IntPtr pixels);
    private static readonly TexSubImage2DDelegate _texSubImage2D;

    private static readonly PixData.Pix_data_free SafeFreeDelegate = FreeUnmanagedPixels;

    private static bool FreeUnmanagedPixels(IntPtr pixdata)
    {
        if (pixdata != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(pixdata);
        }
        return true;
    }

    static GLTextureHelper()
    {
        _texImage2D = CreateDelegate<TexImage2DDelegate>("TexImage2D", 9);
        _getTexImage = CreateDelegate<GetTexImageDelegate>("GetTexImage", 5);
        _texSubImage2D = CreateDelegate<TexSubImage2DDelegate>("TexSubImage2D", 9);
    }

    private static T CreateDelegate<T>(string methodName, int paramCount) where T : Delegate
    {
        var method = typeof(GL).GetMethods()
            .FirstOrDefault(m => m.Name == methodName && 
                                 m.GetParameters().Length == paramCount && 
                                 m.GetParameters()[paramCount - 1].ParameterType.IsPointer);

        if (method == null)
        {
            throw new Exception($"Could not find GL.{methodName} with a pointer parameter and {paramCount} arguments");
        }

        var paramTypes = new Type[paramCount + 1];
        paramTypes[0] = typeof(GL);
        for (int i = 0; i < paramCount; i++)
        {
            var p = method.GetParameters()[i];
            paramTypes[i + 1] = p.ParameterType.IsPointer ? typeof(IntPtr) : p.ParameterType;
        }

        var dynamicMethod = new DynamicMethod(
            methodName + "IntPtr",
            null,
            paramTypes,
            typeof(GLTextureHelper).Module,
            true);

        var il = dynamicMethod.GetILGenerator();
        for (int i = 0; i <= paramCount; i++)
        {
            il.Emit(OpCodes.Ldarg, i);
        }
        il.Emit(OpCodes.Callvirt, method);
        il.Emit(OpCodes.Ret);

        return (T)dynamicMethod.CreateDelegate(typeof(T));
    }

    public static uint CreateTexture2DFromPixData(GL gl, PixData pixData, bool generateMipmaps = false)
    {
        if (pixData.pix_data == IntPtr.Zero)
            throw new ArgumentException("PixData.pix_data is zero.");

        if (pixData.width <= 0 || pixData.height <= 0)
            throw new ArgumentException("PixData width and height must be positive.");

        //PixData proxy = CrystalCatalystLibrary.net.Pixels.ConvertPixels(pixData, "bgra:int8");

        if (pixData.pix_format != "bgra:int8")
        {
            throw new NotSupportedException($"Only 'bgra:int8' format is supported. Found: {pixData.pix_format}");
        }

        uint texture = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, texture);

        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

        _texImage2D(gl, TextureTarget.Texture2D, 0, InternalFormat.Rgba8, (uint)pixData.width, (uint)pixData.height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, pixData.pix_data);

        if (generateMipmaps)
        {
            gl.GenerateMipmap(TextureTarget.Texture2D);
        }

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

        string formatStr = src.pix_format.ToString() ?? "";
        if (formatStr == "bgra:int8")
        {
            _texSubImage2D(gl, TextureTarget.Texture2D, 0, 0, 0, (uint)src.width, (uint)src.height, PixelFormat.Bgra, PixelType.UnsignedByte, src.pix_data);
        }
        else if (formatStr == "rgba:int8")
        {
            _texSubImage2D(gl, TextureTarget.Texture2D, 0, 0, 0, (uint)src.width, (uint)src.height, PixelFormat.Rgba, PixelType.UnsignedByte, src.pix_data);
        }
        else
        {
            throw new NotSupportedException($"Format '{formatStr}' is not supported for WritePixels.");
        }
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

        // In a real implementation, we might look at internalFormat to decide the best matching PixData format.
        // For this prototype, we'll support basic RGBA/BGRA inference.
        if (internalFormat == (int)InternalFormat.Rgba8 || internalFormat == (int)GLEnum.Rgba)
        {
            // Defaulting to bgra:int8 is often preferred for Skia, 
            // but let's see if we can be smarter.
            // If the user didn't specify, bgra:int8 is a safe bet for this codebase.
        }
        
        int byteCount = width * height * 4;
        IntPtr unmanagedPixels = Marshal.AllocHGlobal(byteCount);

        gl.PixelStore(PixelStoreParameter.PackAlignment, 1);
        _getTexImage(gl, TextureTarget.Texture2D, 0, glFormat, PixelType.UnsignedByte, unmanagedPixels);

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
