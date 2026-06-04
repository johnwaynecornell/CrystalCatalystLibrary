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
    private delegate void TexSubImage2DDelegate(GL gl, TextureTarget target, int level, int xoffset, int yoffset, uint width, uint height, PixelFormat format, PixelType type, IntPtr pixels);
    private delegate void GetTexImageDelegate(GL gl, TextureTarget target, int level, PixelFormat format, PixelType type, IntPtr pixels);
    private delegate void DrawElementsDelegate(GL gl, PrimitiveType mode, uint count, DrawElementsType type, IntPtr indices);

    // Static fields
    private static readonly BufferDataDelegate _bufferData;
    private static readonly BufferSubDataDelegate _bufferSubData;
    private static readonly VertexAttribPointerDelegate _vertexAttribPointer;
    private static readonly ReadPixelsDelegate _readPixels;
    private static readonly DrawBuffersDelegate _drawBuffers;
    private static readonly TexImage2DDelegate _texImage2D;
    private static readonly TexSubImage2DDelegate _texSubImage2D;
    private static readonly GetTexImageDelegate _getTexImage;
    private static readonly DrawElementsDelegate _drawElements;

    static GLBridges()
    {
        _bufferData = CreateDelegate<BufferDataDelegate>("BufferData", typeof(BufferTargetARB), typeof(UIntPtr), typeof(void*), typeof(BufferUsageARB));
        _bufferSubData = CreateDelegate<BufferSubDataDelegate>("BufferSubData", typeof(BufferTargetARB), typeof(IntPtr), typeof(UIntPtr), typeof(void*));
        _vertexAttribPointer = CreateDelegate<VertexAttribPointerDelegate>("VertexAttribPointer", typeof(uint), typeof(int), typeof(GLEnum), typeof(bool), typeof(uint), typeof(void*));
        _readPixels = CreateDelegate<ReadPixelsDelegate>("ReadPixels", typeof(int), typeof(int), typeof(uint), typeof(uint), typeof(PixelFormat), typeof(PixelType), typeof(void*));
        _drawBuffers = CreateDelegate<DrawBuffersDelegate>("DrawBuffers", typeof(uint), typeof(GLEnum*));
        _texImage2D = CreateDelegate<TexImage2DDelegate>("TexImage2D", typeof(TextureTarget), typeof(int), typeof(InternalFormat), typeof(uint), typeof(uint), typeof(int), typeof(PixelFormat), typeof(PixelType), typeof(void*));
        _texSubImage2D = CreateDelegate<TexSubImage2DDelegate>("TexSubImage2D", typeof(TextureTarget), typeof(int), typeof(int), typeof(int), typeof(uint), typeof(uint), typeof(PixelFormat), typeof(PixelType), typeof(void*));
        _getTexImage = CreateDelegate<GetTexImageDelegate>("GetTexImage", typeof(TextureTarget), typeof(int), typeof(PixelFormat), typeof(PixelType), typeof(void*));
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
    public static void TexSubImage2D(GL gl, TextureTarget target, int level, int xoffset, int yoffset, uint width, uint height, PixelFormat format, PixelType type, IntPtr pixels) => _texSubImage2D(gl, target, level, xoffset, yoffset, width, height, format, type, pixels);
    public static void GetTexImage(GL gl, TextureTarget target, int level, PixelFormat format, PixelType type, IntPtr pixels) => _getTexImage(gl, target, level, format, type, pixels);
    public static void DrawElements(GL gl, PrimitiveType mode, uint count, DrawElementsType type, IntPtr indices) => _drawElements(gl, mode, count, type, indices);

    // Stereo / Buffer Selection
    public static void DrawBuffer(GL gl, DrawBufferMode mode) => gl.DrawBuffer(mode);
    public static void ReadBuffer(GL gl, ReadBufferMode mode) => gl.ReadBuffer(mode);
}
