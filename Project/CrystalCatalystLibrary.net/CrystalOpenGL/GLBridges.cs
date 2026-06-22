using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Silk.NET.OpenGL;

namespace CrystalOpenGL;

/// <summary>
/// Provides bridges for OpenGL functions that require pointer arguments, using <see cref="IntPtr"/> for compatibility with managed code.
/// </summary>
public static class GLBridges
{
    // Delegates
    private delegate void BufferDataDelegate(GL gl, BufferTargetARB target, UIntPtr size, IntPtr data, BufferUsageARB usage);
    private delegate void BufferSubDataDelegate(GL gl, BufferTargetARB target, IntPtr offset, UIntPtr size, IntPtr data);
    private delegate void VertexAttribPointerDelegate(GL gl, uint index, int size, GLEnum type, bool normalized, uint stride, IntPtr pointer);
    private delegate void ReadPixelsDelegate(GL gl, int x, int y, uint width, uint height, PixelFormat format, PixelType type, IntPtr pixels);
    private delegate void DrawBuffersDelegate(GL gl, uint n, IntPtr bufs);
    private delegate void TexImage2DDelegate(GL gl, TextureTarget target, int level, InternalFormat internalformat, uint width, uint height, int border, PixelFormat format, PixelType type, IntPtr pixels);
    private delegate void TexImage3DDelegate(GL gl, TextureTarget target, int level, InternalFormat internalformat, uint width, uint height, uint depth, int border, PixelFormat format, PixelType type, IntPtr pixels);
    private delegate void TexSubImage2DDelegate(GL gl, TextureTarget target, int level, int xoffset, int yoffset, uint width, uint height, PixelFormat format, PixelType type, IntPtr pixels);
    private delegate void TexSubImage3DDelegate(GL gl, TextureTarget target, int level, int xoffset, int yoffset, int zoffset, uint width, uint height, uint depth, PixelFormat format, PixelType type, IntPtr pixels);
    private delegate void CompressedTexImage3DDelegate(GL gl, TextureTarget target, int level, InternalFormat internalformat, uint width, uint height, uint depth, int border, uint imageSize, IntPtr data);
    private delegate void CompressedTexSubImage3DDelegate(GL gl, TextureTarget target, int level, int xoffset, int yoffset, int zoffset, uint width, uint height, uint depth, InternalFormat format, uint imageSize, IntPtr data);
    private delegate void GetTexImageDelegate(GL gl, TextureTarget target, int level, PixelFormat format, PixelType type, IntPtr pixels);
    private delegate void TextureSubImage2DDelegate(GL gl, uint texture, int level, int xoffset, int yoffset, uint width, uint height, PixelFormat format, PixelType type, IntPtr pixels);
    private delegate void TextureSubImage3DDelegate(GL gl, uint texture, int level, int xoffset, int yoffset, int zoffset, uint width, uint height, uint depth, PixelFormat format, PixelType type, IntPtr pixels);
    private delegate void CompressedTextureSubImage3DDelegate(GL gl, uint texture, int level, int xoffset, int yoffset, int zoffset, uint width, uint height, uint depth, InternalFormat format, uint imageSize, IntPtr data);
    private delegate void GetTextureSubImageDelegate(GL gl, uint texture, int level, int xoffset, int yoffset, int zoffset, uint width, uint height, uint depth, PixelFormat format, PixelType type, uint bufSize, IntPtr pixels);
    private delegate void GetTextureImageDelegate(GL gl, uint texture, int level, PixelFormat format, PixelType type, uint bufSize, IntPtr pixels);
    private delegate void DrawElementsDelegate(GL gl, PrimitiveType mode, uint count, DrawElementsType type, IntPtr indices);

    // Static fields
    private static readonly BufferDataDelegate _bufferData;
    private static readonly BufferSubDataDelegate _bufferSubData;
    private static readonly VertexAttribPointerDelegate _vertexAttribPointer;
    private static readonly ReadPixelsDelegate _readPixels;
    private static readonly DrawBuffersDelegate _drawBuffers;
    private static readonly TexImage2DDelegate _texImage2D;
    private static readonly TexImage3DDelegate _texImage3D;
    private static readonly TexSubImage2DDelegate _texSubImage2D;
    private static readonly TexSubImage3DDelegate _texSubImage3D;
    private static readonly CompressedTexImage3DDelegate _compressedTexImage3D;
    private static readonly CompressedTexSubImage3DDelegate _compressedTexSubImage3D;
    private static readonly GetTexImageDelegate _getTexImage;
    private static readonly TextureSubImage2DDelegate _textureSubImage2D;
    private static readonly TextureSubImage3DDelegate _textureSubImage3D;
    private static readonly CompressedTextureSubImage3DDelegate _compressedTextureSubImage3D;
    private static readonly GetTextureSubImageDelegate _getTextureSubImage;
    private static readonly GetTextureImageDelegate _getTextureImage;
    private static readonly DrawElementsDelegate _drawElements;

    static GLBridges()
    {
        _bufferData = CreateDelegate<BufferDataDelegate>("BufferData", typeof(BufferTargetARB), typeof(UIntPtr), typeof(void*), typeof(BufferUsageARB));
        _bufferSubData = CreateDelegate<BufferSubDataDelegate>("BufferSubData", typeof(BufferTargetARB), typeof(IntPtr), typeof(UIntPtr), typeof(void*));
        _vertexAttribPointer = CreateDelegate<VertexAttribPointerDelegate>("VertexAttribPointer", typeof(uint), typeof(int), typeof(GLEnum), typeof(bool), typeof(uint), typeof(void*));
        _readPixels = CreateDelegate<ReadPixelsDelegate>("ReadPixels", typeof(int), typeof(int), typeof(uint), typeof(uint), typeof(PixelFormat), typeof(PixelType), typeof(void*));
        _drawBuffers = CreateDelegate<DrawBuffersDelegate>("DrawBuffers", typeof(uint), typeof(GLEnum*));
        _texImage2D = CreateDelegate<TexImage2DDelegate>("TexImage2D", typeof(TextureTarget), typeof(int), typeof(InternalFormat), typeof(uint), typeof(uint), typeof(int), typeof(PixelFormat), typeof(PixelType), typeof(void*));
        _texImage3D = CreateDelegate<TexImage3DDelegate>("TexImage3D", typeof(TextureTarget), typeof(int), typeof(InternalFormat), typeof(uint), typeof(uint), typeof(uint), typeof(int), typeof(PixelFormat), typeof(PixelType), typeof(void*));
        _texSubImage2D = CreateDelegate<TexSubImage2DDelegate>("TexSubImage2D", typeof(TextureTarget), typeof(int), typeof(int), typeof(int), typeof(uint), typeof(uint), typeof(PixelFormat), typeof(PixelType), typeof(void*));
        _texSubImage3D = CreateDelegate<TexSubImage3DDelegate>("TexSubImage3D", typeof(TextureTarget), typeof(int), typeof(int), typeof(int), typeof(int), typeof(uint), typeof(uint), typeof(uint), typeof(PixelFormat), typeof(PixelType), typeof(void*));
        _compressedTexImage3D = CreateDelegate<CompressedTexImage3DDelegate>("CompressedTexImage3D", typeof(TextureTarget), typeof(int), typeof(InternalFormat), typeof(uint), typeof(uint), typeof(uint), typeof(int), typeof(uint), typeof(void*));
        _compressedTexSubImage3D = CreateDelegate<CompressedTexSubImage3DDelegate>("CompressedTexSubImage3D", typeof(TextureTarget), typeof(int), typeof(int), typeof(int), typeof(int), typeof(uint), typeof(uint), typeof(uint), typeof(InternalFormat), typeof(uint), typeof(void*));
        _getTexImage = CreateDelegate<GetTexImageDelegate>("GetTexImage", typeof(TextureTarget), typeof(int), typeof(PixelFormat), typeof(PixelType), typeof(void*));
        _textureSubImage2D = CreateDelegate<TextureSubImage2DDelegate>("TextureSubImage2D", typeof(uint), typeof(int), typeof(int), typeof(int), typeof(uint), typeof(uint), typeof(PixelFormat), typeof(PixelType), typeof(void*));
        _textureSubImage3D = CreateDelegate<TextureSubImage3DDelegate>("TextureSubImage3D", typeof(uint), typeof(int), typeof(int), typeof(int), typeof(int), typeof(uint), typeof(uint), typeof(uint), typeof(PixelFormat), typeof(PixelType), typeof(void*));
        _compressedTextureSubImage3D = CreateDelegate<CompressedTextureSubImage3DDelegate>("CompressedTextureSubImage3D", typeof(uint), typeof(int), typeof(int), typeof(int), typeof(int), typeof(uint), typeof(uint), typeof(uint), typeof(InternalFormat), typeof(uint), typeof(void*));
        _getTextureSubImage = CreateDelegate<GetTextureSubImageDelegate>("GetTextureSubImage", typeof(uint), typeof(int), typeof(int), typeof(int), typeof(int), typeof(uint), typeof(uint), typeof(uint), typeof(PixelFormat), typeof(PixelType), typeof(uint), typeof(void*));
        _getTextureImage = CreateDelegate<GetTextureImageDelegate>("GetTextureImage", typeof(uint), typeof(int), typeof(PixelFormat), typeof(PixelType), typeof(uint), typeof(void*));
        _drawElements = CreateDelegate<DrawElementsDelegate>("DrawElements", typeof(PrimitiveType), typeof(uint), typeof(DrawElementsType), typeof(void*));
    }

    private static T CreateDelegate<T>(string methodName, params Type[] parameterTypes) where T : Delegate
    {
        var method = typeof(GL).GetMethod(methodName, parameterTypes);
        if (method == null)
        {
            // Try fallback for cases where Silk.NET uses GLEnum instead of specific enum types
            var fallbackTypes = parameterTypes.Select(t => t.IsEnum ? typeof(GLEnum) : t).ToArray();
            method = typeof(GL).GetMethod(methodName, fallbackTypes);
        }

        if (method == null)
        {
             // Last ditch effort: find by name and parameter count
             method = typeof(GL).GetMethods().FirstOrDefault(m => m.Name == methodName && m.GetParameters().Length == parameterTypes.Length && m.GetParameters().Any(p => p.ParameterType.IsPointer));
        }

        if (method == null)
            throw new Exception($"Could not find GL.{methodName} with matching signature.");

        var actualParams = method.GetParameters();
        var bridgeParamTypes = new Type[actualParams.Length + 1];
        bridgeParamTypes[0] = typeof(GL);
        for (int i = 0; i < actualParams.Length; i++)
        {
            bridgeParamTypes[i + 1] = actualParams[i].ParameterType.IsPointer ? typeof(IntPtr) : actualParams[i].ParameterType;
        }

        var dynamicMethod = new DynamicMethod(methodName + "IntPtr", null, bridgeParamTypes, typeof(GLBridges).Module, true);
        var il = dynamicMethod.GetILGenerator();
        for (int i = 0; i <= actualParams.Length; i++)
            il.Emit(OpCodes.Ldarg, i);
        il.Emit(OpCodes.Callvirt, method);
        il.Emit(OpCodes.Ret);

        return (T)dynamicMethod.CreateDelegate(typeof(T));
    }

    public static void BufferData(GL gl, BufferTargetARB target, UIntPtr size, IntPtr data, BufferUsageARB usage) => _bufferData(gl, target, size, data, usage);
    public static void BufferSubData(GL gl, BufferTargetARB target, IntPtr offset, UIntPtr size, IntPtr data) => _bufferSubData(gl, target, offset, size, data);
    public static void VertexAttribPointer(GL gl, uint index, int size, GLEnum type, bool normalized, uint stride, IntPtr pointer) => _vertexAttribPointer(gl, index, size, type, normalized, stride, pointer);
    public static void ReadPixels(GL gl, int x, int y, uint width, uint height, PixelFormat format, PixelType type, IntPtr pixels) => _readPixels(gl, x, y, width, height, format, type, pixels);
    public static void DrawBuffers(GL gl, uint n, IntPtr bufs) => _drawBuffers(gl, n, bufs);
    public static void TexImage2D(GL gl, TextureTarget target, int level, InternalFormat internalformat, uint width, uint height, int border, PixelFormat format, PixelType type, IntPtr pixels) => _texImage2D(gl, target, level, internalformat, width, height, border, format, type, pixels);
    public static void TexImage3D(GL gl, TextureTarget target, int level, InternalFormat internalformat, uint width, uint height, uint depth, int border, PixelFormat format, PixelType type, IntPtr pixels) => _texImage3D(gl, target, level, internalformat, width, height, depth, border, format, type, pixels);
    public static void TexSubImage2D(GL gl, TextureTarget target, int level, int xoffset, int yoffset, uint width, uint height, PixelFormat format, PixelType type, IntPtr pixels) => _texSubImage2D(gl, target, level, xoffset, yoffset, width, height, format, type, pixels);
    public static void TexSubImage3D(GL gl, TextureTarget target, int level, int xoffset, int yoffset, int zoffset, uint width, uint height, uint depth, PixelFormat format, PixelType type, IntPtr pixels) => _texSubImage3D(gl, target, level, xoffset, yoffset, zoffset, width, height, depth, format, type, pixels);
    public static void CompressedTexImage3D(GL gl, TextureTarget target, int level, InternalFormat internalformat, uint width, uint height, uint depth, int border, uint imageSize, IntPtr data) => _compressedTexImage3D(gl, target, level, internalformat, width, height, depth, border, imageSize, data);
    public static void CompressedTexSubImage3D(GL gl, TextureTarget target, int level, int xoffset, int yoffset, int zoffset, uint width, uint height, uint depth, InternalFormat format, uint imageSize, IntPtr data) => _compressedTexSubImage3D(gl, target, level, xoffset, yoffset, zoffset, width, height, depth, format, imageSize, data);
    public static void TextureSubImage2D(GL gl, uint texture, int level, int xoffset, int yoffset, uint width, uint height, PixelFormat format, PixelType type, IntPtr pixels) => _textureSubImage2D(gl, texture, level, xoffset, yoffset, width, height, format, type, pixels);
    public static void TextureSubImage3D(GL gl, uint texture, int level, int xoffset, int yoffset, int zoffset, uint width, uint height, uint depth, PixelFormat format, PixelType type, IntPtr pixels) => _textureSubImage3D(gl, texture, level, xoffset, yoffset, zoffset, width, height, depth, format, type, pixels);
    public static void CompressedTextureSubImage3D(GL gl, uint texture, int level, int xoffset, int yoffset, int zoffset, uint width, uint height, uint depth, InternalFormat format, uint imageSize, IntPtr data) => _compressedTextureSubImage3D(gl, texture, level, xoffset, yoffset, zoffset, width, height, depth, format, imageSize, data);
    public static void GetTextureSubImage(GL gl, uint texture, int level, int xoffset, int yoffset, int zoffset, uint width, uint height, uint depth, PixelFormat format, PixelType type, uint bufSize, IntPtr pixels) => _getTextureSubImage(gl, texture, level, xoffset, yoffset, zoffset, width, height, depth, format, type, bufSize, pixels);
    public static void GetTexImage(GL gl, TextureTarget target, int level, PixelFormat format, PixelType type, IntPtr pixels) => _getTexImage(gl, target, level, format, type, pixels);
    public static void GetTextureImage(GL gl, uint texture, int level, PixelFormat format, PixelType type, uint bufSize, IntPtr pixels) => _getTextureImage(gl, texture, level, format, type, bufSize, pixels);
    public static void DrawElements(GL gl, PrimitiveType mode, uint count, DrawElementsType type, IntPtr indices) => _drawElements(gl, mode, count, type, indices);

    // Stereo / Buffer Selection
    public static void DrawBuffer(GL gl, DrawBufferMode mode) => gl.DrawBuffer(mode);
    public static void ReadBuffer(GL gl, ReadBufferMode mode) => gl.ReadBuffer(mode);
}
