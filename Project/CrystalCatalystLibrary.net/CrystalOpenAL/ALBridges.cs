using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Silk.NET.OpenAL;

namespace CrystalOpenAL;

/// <summary>
/// Provides bridges for OpenAL functions that require pointer arguments, using <see cref="IntPtr"/>,
/// <see cref="Array"/>, and <see cref="GCHandle"/> pinning for compatibility with managed code without unsafe blocks.
/// </summary>
public static class ALBridges
{
    // ALContext Delegates
    private delegate IntPtr OpenDeviceDelegate(ALContext alc, string? name);
    private delegate IntPtr CreateContextDelegate(ALContext alc, IntPtr device, IntPtr attrlist);
    private delegate bool MakeContextCurrentDelegate(ALContext alc, IntPtr context);
    private delegate void DestroyContextDelegate(ALContext alc, IntPtr context);
    private delegate bool CloseDeviceDelegate(ALContext alc, IntPtr device);
    private delegate IntPtr GetCurrentContextDelegate(ALContext alc);
    private delegate IntPtr GetContextsDeviceDelegate(ALContext alc, IntPtr context);
    private delegate ContextError GetContextErrorDelegate(ALContext alc, IntPtr device);

    // AL Delegates
    private delegate void BufferDataDelegate(AL al, uint buffer, BufferFormat format, IntPtr data, int size, int frequency);

    // Static fields
    private static readonly OpenDeviceDelegate _openDevice;
    private static readonly CreateContextDelegate _createContext;
    private static readonly MakeContextCurrentDelegate _makeContextCurrent;
    private static readonly DestroyContextDelegate _destroyContext;
    private static readonly CloseDeviceDelegate _closeDevice;
    private static readonly GetCurrentContextDelegate _getCurrentContext;
    private static readonly GetContextsDeviceDelegate _getContextsDevice;
    private static readonly GetContextErrorDelegate _getContextError;

    private static readonly BufferDataDelegate _bufferData;

    static ALBridges()
    {
        _openDevice = CreateDelegate<OpenDeviceDelegate, ALContext>("OpenDevice", typeof(string));
        _createContext = CreateDelegate<CreateContextDelegate, ALContext>("CreateContext", typeof(IntPtr), typeof(IntPtr));
        _makeContextCurrent = CreateDelegate<MakeContextCurrentDelegate, ALContext>("MakeContextCurrent", typeof(IntPtr));
        _destroyContext = CreateDelegate<DestroyContextDelegate, ALContext>("DestroyContext", typeof(IntPtr));
        _closeDevice = CreateDelegate<CloseDeviceDelegate, ALContext>("CloseDevice", typeof(IntPtr));
        _getCurrentContext = CreateDelegate<GetCurrentContextDelegate, ALContext>("GetCurrentContext");
        _getContextsDevice = CreateDelegate<GetContextsDeviceDelegate, ALContext>("GetContextsDevice", typeof(IntPtr));
        _getContextError = CreateDelegate<GetContextErrorDelegate, ALContext>("GetError", typeof(IntPtr));

        _bufferData = CreateDelegate<BufferDataDelegate, AL>("BufferData", typeof(uint), typeof(BufferFormat), typeof(IntPtr), typeof(int), typeof(int));
    }

    private static T CreateDelegate<T, TTarget>(string methodName, params Type[] parameterTypes) where T : Delegate
    {
        var targetType = typeof(TTarget);
        var methods = targetType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

        MethodInfo? method = null;
        foreach (var m in methods)
        {
            if (m.Name != methodName) continue;
            var parameters = m.GetParameters();
            if (parameters.Length != parameterTypes.Length) continue;

            bool matches = true;
            for (int i = 0; i < parameters.Length; i++)
            {
                Type expected = parameterTypes[i];
                Type actual = parameters[i].ParameterType;

                if (expected == typeof(IntPtr))
                {
                    if (!actual.IsPointer && actual != typeof(IntPtr))
                    {
                        matches = false;
                        break;
                    }
                }
                else if (actual != expected && !expected.IsAssignableFrom(actual))
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
            {
                method = m;
                break;
            }
        }

        if (method == null)
        {
            // Fallback match by name and parameter count
            method = methods.FirstOrDefault(m => m.Name == methodName && m.GetParameters().Length == parameterTypes.Length);
        }

        if (method == null)
            throw new InvalidOperationException($"Could not find {targetType.Name}.{methodName} with matching signature.");

        var actualParams = method.GetParameters();
        var bridgeParamTypes = new Type[actualParams.Length + 1];
        bridgeParamTypes[0] = targetType;
        for (int i = 0; i < actualParams.Length; i++)
        {
            bridgeParamTypes[i + 1] = actualParams[i].ParameterType.IsPointer ? typeof(IntPtr) : actualParams[i].ParameterType;
        }

        Type? returnType = method.ReturnType;
        if (returnType == typeof(void))
        {
            returnType = null;
        }
        else if (returnType.IsPointer)
        {
            returnType = typeof(IntPtr);
        }

        var dynamicMethod = new DynamicMethod(
            $"{targetType.Name}_{methodName}_IntPtrBridge",
            returnType,
            bridgeParamTypes,
            typeof(ALBridges).Module,
            true);

        var il = dynamicMethod.GetILGenerator();
        for (int i = 0; i <= actualParams.Length; i++)
        {
            il.Emit(OpCodes.Ldarg, i);
        }
        il.Emit(OpCodes.Callvirt, method);
        il.Emit(OpCodes.Ret);

        return (T)dynamicMethod.CreateDelegate(typeof(T));
    }

    // ALContext Bridges
    public static IntPtr OpenDevice(ALContext alc, string? name = null) => _openDevice(alc, string.IsNullOrEmpty(name) ? null : name);
    public static IntPtr CreateContext(ALContext alc, IntPtr device, IntPtr attrlist) => _createContext(alc, device, attrlist);

    /// <summary>
    /// Creates an OpenAL context for the specified device, optionally passing an attribute list pinned with <see cref="GCHandle"/>.
    /// </summary>
    public static IntPtr CreateContext(ALContext alc, IntPtr device, int[]? attrlist = null)
    {
        if (attrlist == null || attrlist.Length == 0)
        {
            return _createContext(alc, device, IntPtr.Zero);
        }

        GCHandle handle = GCHandle.Alloc(attrlist, GCHandleType.Pinned);
        try
        {
            return _createContext(alc, device, handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    public static bool MakeContextCurrent(ALContext alc, IntPtr context) => _makeContextCurrent(alc, context);
    public static void DestroyContext(ALContext alc, IntPtr context) => _destroyContext(alc, context);
    public static bool CloseDevice(ALContext alc, IntPtr device) => _closeDevice(alc, device);
    public static IntPtr GetCurrentContext(ALContext alc) => _getCurrentContext(alc);
    public static IntPtr GetContextsDevice(ALContext alc, IntPtr context) => _getContextsDevice(alc, context);
    public static ContextError GetError(ALContext alc, IntPtr device) => _getContextError(alc, device);

    // AL Bridges
    public static void BufferData(AL al, uint buffer, BufferFormat format, IntPtr data, int size, int frequency)
        => _bufferData(al, buffer, format, data, size, frequency);

    /// <summary>
    /// Uploads audio data from any managed <see cref="Array"/> to an OpenAL buffer using <see cref="GCHandle"/> pinning.
    /// </summary>
    /// <param name="al">The OpenAL API instance.</param>
    /// <param name="buffer">Target OpenAL buffer id.</param>
    /// <param name="format">Buffer audio format (e.g. Mono16, Stereo16).</param>
    /// <param name="data">Managed array containing PCM data.</param>
    /// <param name="sizeInBytes">Total size of the data in bytes.</param>
    /// <param name="frequency">Sample rate in Hz (e.g. 44100).</param>
    public static void BufferData(AL al, uint buffer, BufferFormat format, Array data, int sizeInBytes, int frequency)
    {
        ArgumentNullException.ThrowIfNull(al);
        ArgumentNullException.ThrowIfNull(data);

        GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            IntPtr ptr = handle.AddrOfPinnedObject();
            _bufferData(al, buffer, format, ptr, sizeInBytes, frequency);
        }
        finally
        {
            handle.Free();
        }
    }

    /// <summary>
    /// Uploads 16-bit PCM short samples to an OpenAL buffer using <see cref="GCHandle"/> pinning.
    /// </summary>
    public static void BufferData(AL al, uint buffer, BufferFormat format, short[] samples, int frequency)
    {
        ArgumentNullException.ThrowIfNull(samples);
        BufferData(al, buffer, format, samples, samples.Length * sizeof(short), frequency);
    }

    /// <summary>
    /// Uploads 8-bit PCM byte samples to an OpenAL buffer using <see cref="GCHandle"/> pinning.
    /// </summary>
    public static void BufferData(AL al, uint buffer, BufferFormat format, byte[] bytes, int frequency)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        BufferData(al, buffer, format, bytes, bytes.Length, frequency);
    }

    /// <summary>
    /// Uploads 32-bit floating point PCM samples to an OpenAL buffer using <see cref="GCHandle"/> pinning.
    /// </summary>
    public static void BufferData(AL al, uint buffer, BufferFormat format, float[] samples, int frequency)
    {
        ArgumentNullException.ThrowIfNull(samples);
        BufferData(al, buffer, format, samples, samples.Length * sizeof(float), frequency);
    }
}
