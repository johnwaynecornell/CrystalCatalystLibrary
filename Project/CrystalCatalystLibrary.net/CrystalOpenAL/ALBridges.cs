using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Silk.NET.OpenAL;

namespace CrystalOpenAL;

/// <summary>
/// Provides exhaustive bridges for OpenAL and ALC functions that require pointer arguments, using <see cref="IntPtr"/>,
/// <see cref="Array"/>, and <see cref="GCHandle"/> pinning for complete compatibility with managed code without unsafe blocks.
/// </summary>
public static class ALBridges
{
    // ==========================================
    // ALContext Delegates
    // ==========================================
    private delegate IntPtr OpenDeviceDelegate(ALContext alc, string? name);
    private delegate bool CloseDeviceDelegate(ALContext alc, IntPtr device);
    private delegate IntPtr CreateContextDelegate(ALContext alc, IntPtr device, IntPtr attrlist);
    private delegate IntPtr CreateContextHandleDelegate(ALContext alc, IntPtr device, IntPtr attrlist);
    private delegate bool MakeContextCurrentDelegate(ALContext alc, IntPtr context);
    private delegate void DestroyContextDelegate(ALContext alc, IntPtr context);
    private delegate IntPtr GetCurrentContextDelegate(ALContext alc);
    private delegate IntPtr GetContextsDeviceDelegate(ALContext alc, IntPtr context);
    private delegate ContextError GetContextErrorDelegate(ALContext alc, IntPtr device);
    private delegate void ProcessContextDelegate(ALContext alc, IntPtr context);
    private delegate void SuspendContextDelegate(ALContext alc, IntPtr context);
    private delegate bool IsExtensionPresentDelegate(ALContext alc, IntPtr device, string name);
    private delegate IntPtr GetProcAddressDelegate(ALContext alc, IntPtr device, string name);
    private delegate int GetEnumValueDelegate(ALContext alc, IntPtr device, string name);
    private delegate string GetContextStringPropertyDelegate(ALContext alc, IntPtr device, GetContextString param);
    private delegate void GetContextIntegerPropertyDelegate(ALContext alc, IntPtr device, GetContextInteger param, int count, IntPtr data);

    // ==========================================
    // AL Delegates
    // ==========================================
    private delegate void BufferDataDelegate(AL al, uint buffer, BufferFormat format, IntPtr data, int size, int frequency);
    private delegate void GenBuffersDelegate(AL al, int count, IntPtr buffers);
    private delegate void DeleteBuffersDelegate(AL al, int count, IntPtr buffers);
    private delegate void GenSourcesDelegate(AL al, int count, IntPtr sources);
    private delegate void DeleteSourcesDelegate(AL al, int count, IntPtr sources);

    private delegate void SourcePlayDelegate(AL al, int count, IntPtr sources);
    private delegate void SourcePauseDelegate(AL al, int count, IntPtr sources);
    private delegate void SourceStopDelegate(AL al, int count, IntPtr sources);
    private delegate void SourceRewindDelegate(AL al, int count, IntPtr sources);

    private delegate void SourceQueueBuffersDelegate(AL al, uint source, int count, IntPtr buffers);
    private delegate void SourceUnqueueBuffersDelegate(AL al, uint source, int count, IntPtr buffers);

    private delegate void GetBufferFloatPropertyDelegate(AL al, uint buffer, BufferFloat param, IntPtr value);
    private delegate void GetBufferIntegerPropertyDelegate(AL al, uint buffer, GetBufferInteger param, IntPtr value);
    private delegate void SetBufferIntegerPropertyDelegate(AL al, uint buffer, BufferInteger param, IntPtr value);
    private delegate void SetBufferVector3PropertyDelegate(AL al, uint buffer, BufferVector3 param, IntPtr value);

    private delegate void GetListenerFloatArrayPropertyDelegate(AL al, ListenerFloatArray param, IntPtr value);
    private delegate void GetListenerIntegerPropertyDelegate(AL al, ListenerInteger param, IntPtr value);
    private delegate void SetListenerFloatArrayPropertyDelegate(AL al, ListenerFloatArray param, IntPtr value);
    private delegate void SetListenerIntegerPropertyDelegate(AL al, ListenerInteger param, IntPtr value);

    private delegate void GetSourceIntegerPropertyDelegate(AL al, uint source, GetSourceInteger param, IntPtr value);
    private delegate void GetSourceFloatPropertyDelegate(AL al, uint source, SourceFloat param, IntPtr value);
    private delegate void SetSourceIntegerPropertyDelegate(AL al, uint source, SourceInteger param, IntPtr value);
    private delegate void SetSourceVector3PropertyDelegate(AL al, uint source, SourceVector3 param, IntPtr value);

    // Static fields - ALContext
    private static readonly OpenDeviceDelegate _openDevice;
    private static readonly CloseDeviceDelegate _closeDevice;
    private static readonly CreateContextDelegate _createContext;
    private static readonly CreateContextHandleDelegate _createContextHandle;
    private static readonly MakeContextCurrentDelegate _makeContextCurrent;
    private static readonly DestroyContextDelegate _destroyContext;
    private static readonly GetCurrentContextDelegate _getCurrentContext;
    private static readonly GetContextsDeviceDelegate _getContextsDevice;
    private static readonly GetContextErrorDelegate _getContextError;
    private static readonly ProcessContextDelegate _processContext;
    private static readonly SuspendContextDelegate _suspendContext;
    private static readonly IsExtensionPresentDelegate _isExtensionPresent;
    private static readonly GetProcAddressDelegate _getProcAddress;
    private static readonly GetEnumValueDelegate _getEnumValue;
    private static readonly GetContextStringPropertyDelegate _getContextStringProperty;
    private static readonly GetContextIntegerPropertyDelegate _getContextIntegerProperty;

    // Static fields - AL
    private static readonly BufferDataDelegate _bufferData;
    private static readonly GenBuffersDelegate _genBuffers;
    private static readonly DeleteBuffersDelegate _deleteBuffers;
    private static readonly GenSourcesDelegate _genSources;
    private static readonly DeleteSourcesDelegate _deleteSources;

    private static readonly SourcePlayDelegate _sourcePlay;
    private static readonly SourcePauseDelegate _sourcePause;
    private static readonly SourceStopDelegate _sourceStop;
    private static readonly SourceRewindDelegate _sourceRewind;

    private static readonly SourceQueueBuffersDelegate _sourceQueueBuffers;
    private static readonly SourceUnqueueBuffersDelegate _sourceUnqueueBuffers;

    private static readonly GetBufferFloatPropertyDelegate _getBufferFloatProperty;
    private static readonly GetBufferIntegerPropertyDelegate _getBufferIntegerProperty;
    private static readonly SetBufferIntegerPropertyDelegate _setBufferIntegerProperty;
    private static readonly SetBufferVector3PropertyDelegate _setBufferVector3Property;

    private static readonly GetListenerFloatArrayPropertyDelegate _getListenerFloatArrayProperty;
    private static readonly GetListenerIntegerPropertyDelegate _getListenerIntegerProperty;
    private static readonly SetListenerFloatArrayPropertyDelegate _setListenerFloatArrayProperty;
    private static readonly SetListenerIntegerPropertyDelegate _setListenerIntegerProperty;

    private static readonly GetSourceIntegerPropertyDelegate _getSourceIntegerProperty;
    private static readonly GetSourceFloatPropertyDelegate _getSourceFloatProperty;
    private static readonly SetSourceIntegerPropertyDelegate _setSourceIntegerProperty;
    private static readonly SetSourceVector3PropertyDelegate _setSourceVector3Property;

    static ALBridges()
    {
        // ALContext delegates
        _openDevice = CreateDelegate<OpenDeviceDelegate, ALContext>("OpenDevice", typeof(string));
        _closeDevice = CreateDelegate<CloseDeviceDelegate, ALContext>("CloseDevice", typeof(IntPtr));
        _createContext = CreateDelegate<CreateContextDelegate, ALContext>("CreateContext", typeof(IntPtr), typeof(IntPtr));
        _createContextHandle = CreateDelegate<CreateContextHandleDelegate, ALContext>("CreateContextHandle", typeof(IntPtr), typeof(IntPtr));
        _makeContextCurrent = CreateDelegate<MakeContextCurrentDelegate, ALContext>("MakeContextCurrent", typeof(IntPtr));
        _destroyContext = CreateDelegate<DestroyContextDelegate, ALContext>("DestroyContext", typeof(IntPtr));
        _getCurrentContext = CreateDelegate<GetCurrentContextDelegate, ALContext>("GetCurrentContext");
        _getContextsDevice = CreateDelegate<GetContextsDeviceDelegate, ALContext>("GetContextsDevice", typeof(IntPtr));
        _getContextError = CreateDelegate<GetContextErrorDelegate, ALContext>("GetError", typeof(IntPtr));
        _processContext = CreateDelegate<ProcessContextDelegate, ALContext>("ProcessContext", typeof(IntPtr));
        _suspendContext = CreateDelegate<SuspendContextDelegate, ALContext>("SuspendContext", typeof(IntPtr));
        _isExtensionPresent = CreateDelegate<IsExtensionPresentDelegate, ALContext>("IsExtensionPresent", typeof(IntPtr), typeof(string));
        _getProcAddress = CreateDelegate<GetProcAddressDelegate, ALContext>("GetProcAddress", typeof(IntPtr), typeof(string));
        _getEnumValue = CreateDelegate<GetEnumValueDelegate, ALContext>("GetEnumValue", typeof(IntPtr), typeof(string));
        _getContextStringProperty = CreateDelegate<GetContextStringPropertyDelegate, ALContext>("GetContextProperty", typeof(IntPtr), typeof(GetContextString));
        _getContextIntegerProperty = CreateDelegate<GetContextIntegerPropertyDelegate, ALContext>("GetContextProperty", typeof(IntPtr), typeof(GetContextInteger), typeof(int), typeof(IntPtr));

        // AL delegates
        _bufferData = CreateDelegate<BufferDataDelegate, AL>("BufferData", typeof(uint), typeof(BufferFormat), typeof(IntPtr), typeof(int), typeof(int));
        _genBuffers = CreateDelegate<GenBuffersDelegate, AL>("GenBuffers", typeof(int), typeof(IntPtr));
        _deleteBuffers = CreateDelegate<DeleteBuffersDelegate, AL>("DeleteBuffers", typeof(int), typeof(IntPtr));
        _genSources = CreateDelegate<GenSourcesDelegate, AL>("GenSources", typeof(int), typeof(IntPtr));
        _deleteSources = CreateDelegate<DeleteSourcesDelegate, AL>("DeleteSources", typeof(int), typeof(IntPtr));

        _sourcePlay = CreateDelegate<SourcePlayDelegate, AL>("SourcePlay", typeof(int), typeof(IntPtr));
        _sourcePause = CreateDelegate<SourcePauseDelegate, AL>("SourcePause", typeof(int), typeof(IntPtr));
        _sourceStop = CreateDelegate<SourceStopDelegate, AL>("SourceStop", typeof(int), typeof(IntPtr));
        _sourceRewind = CreateDelegate<SourceRewindDelegate, AL>("SourceRewind", typeof(int), typeof(IntPtr));

        _sourceQueueBuffers = CreateDelegate<SourceQueueBuffersDelegate, AL>("SourceQueueBuffers", typeof(uint), typeof(int), typeof(IntPtr));
        _sourceUnqueueBuffers = CreateDelegate<SourceUnqueueBuffersDelegate, AL>("SourceUnqueueBuffers", typeof(uint), typeof(int), typeof(IntPtr));

        _getBufferFloatProperty = CreateDelegate<GetBufferFloatPropertyDelegate, AL>("GetBufferProperty", typeof(uint), typeof(BufferFloat), typeof(IntPtr));
        _getBufferIntegerProperty = CreateDelegate<GetBufferIntegerPropertyDelegate, AL>("GetBufferProperty", typeof(uint), typeof(GetBufferInteger), typeof(IntPtr));
        _setBufferIntegerProperty = CreateDelegate<SetBufferIntegerPropertyDelegate, AL>("SetBufferProperty", typeof(uint), typeof(BufferInteger), typeof(IntPtr));
        _setBufferVector3Property = CreateDelegate<SetBufferVector3PropertyDelegate, AL>("SetBufferProperty", typeof(uint), typeof(BufferVector3), typeof(IntPtr));

        _getListenerFloatArrayProperty = CreateDelegate<GetListenerFloatArrayPropertyDelegate, AL>("GetListenerProperty", typeof(ListenerFloatArray), typeof(IntPtr));
        _getListenerIntegerProperty = CreateDelegate<GetListenerIntegerPropertyDelegate, AL>("GetListenerProperty", typeof(ListenerInteger), typeof(IntPtr));
        _setListenerFloatArrayProperty = CreateDelegate<SetListenerFloatArrayPropertyDelegate, AL>("SetListenerProperty", typeof(ListenerFloatArray), typeof(IntPtr));
        _setListenerIntegerProperty = CreateDelegate<SetListenerIntegerPropertyDelegate, AL>("SetListenerProperty", typeof(ListenerInteger), typeof(IntPtr));

        _getSourceIntegerProperty = CreateDelegate<GetSourceIntegerPropertyDelegate, AL>("GetSourceProperty", typeof(uint), typeof(GetSourceInteger), typeof(IntPtr));
        _getSourceFloatProperty = CreateDelegate<GetSourceFloatPropertyDelegate, AL>("GetSourceProperty", typeof(uint), typeof(SourceFloat), typeof(IntPtr));
        _setSourceIntegerProperty = CreateDelegate<SetSourceIntegerPropertyDelegate, AL>("SetSourceProperty", typeof(uint), typeof(SourceInteger), typeof(IntPtr));
        _setSourceVector3Property = CreateDelegate<SetSourceVector3PropertyDelegate, AL>("SetSourceProperty", typeof(uint), typeof(SourceVector3), typeof(IntPtr));
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

                if (expected == typeof(IntPtr) || expected == typeof(void*))
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
            $"{targetType.Name}_{methodName}_{string.Join("_", parameterTypes.Select(p => p.Name))}_Bridge",
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

    // ==========================================
    // ALContext Bridges
    // ==========================================

    /// <summary>
    /// Opens an audio device. Pass null to open the default system audio device.
    /// </summary>
    public static IntPtr OpenDevice(ALContext alc, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(alc);
        return _openDevice(alc, string.IsNullOrEmpty(name) ? null : name);
    }

    /// <summary>
    /// Closes the specified audio device.
    /// </summary>
    public static bool CloseDevice(ALContext alc, IntPtr device)
    {
        ArgumentNullException.ThrowIfNull(alc);
        return _closeDevice(alc, device);
    }

    /// <summary>
    /// Creates an OpenAL context for the specified device.
    /// </summary>
    public static IntPtr CreateContext(ALContext alc, IntPtr device, IntPtr attrlist)
    {
        ArgumentNullException.ThrowIfNull(alc);
        return _createContext(alc, device, attrlist);
    }

    /// <summary>
    /// Creates an OpenAL context for the specified device, optionally passing an attribute list pinned with <see cref="GCHandle"/>.
    /// </summary>
    public static IntPtr CreateContext(ALContext alc, IntPtr device, int[]? attrlist = null)
    {
        ArgumentNullException.ThrowIfNull(alc);
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

    /// <summary>
    /// Creates a context handle for the specified device with attribute list.
    /// </summary>
    public static IntPtr CreateContextHandle(ALContext alc, IntPtr device, IntPtr attrlist)
    {
        ArgumentNullException.ThrowIfNull(alc);
        return _createContextHandle(alc, device, attrlist);
    }

    /// <summary>
    /// Creates a context handle for the specified device, optionally passing an attribute list pinned with <see cref="GCHandle"/>.
    /// </summary>
    public static IntPtr CreateContextHandle(ALContext alc, IntPtr device, int[]? attrlist = null)
    {
        ArgumentNullException.ThrowIfNull(alc);
        if (attrlist == null || attrlist.Length == 0)
        {
            return _createContextHandle(alc, device, IntPtr.Zero);
        }

        GCHandle handle = GCHandle.Alloc(attrlist, GCHandleType.Pinned);
        try
        {
            return _createContextHandle(alc, device, handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    /// <summary>
    /// Makes the specified context current on the calling thread. Pass <see cref="IntPtr.Zero"/> to release.
    /// </summary>
    public static bool MakeContextCurrent(ALContext alc, IntPtr context)
    {
        ArgumentNullException.ThrowIfNull(alc);
        return _makeContextCurrent(alc, context);
    }

    /// <summary>
    /// Destroys the specified OpenAL context.
    /// </summary>
    public static void DestroyContext(ALContext alc, IntPtr context)
    {
        ArgumentNullException.ThrowIfNull(alc);
        _destroyContext(alc, context);
    }

    /// <summary>
    /// Retrieves the current active OpenAL context handle.
    /// </summary>
    public static IntPtr GetCurrentContext(ALContext alc)
    {
        ArgumentNullException.ThrowIfNull(alc);
        return _getCurrentContext(alc);
    }

    /// <summary>
    /// Retrieves the device associated with the specified context.
    /// </summary>
    public static IntPtr GetContextsDevice(ALContext alc, IntPtr context)
    {
        ArgumentNullException.ThrowIfNull(alc);
        return _getContextsDevice(alc, context);
    }

    /// <summary>
    /// Queries error status for the specified device or ALC subsystem.
    /// </summary>
    public static ContextError GetError(ALContext alc, IntPtr device)
    {
        ArgumentNullException.ThrowIfNull(alc);
        return _getContextError(alc, device);
    }

    /// <summary>
    /// Tells a context to begin processing.
    /// </summary>
    public static void ProcessContext(ALContext alc, IntPtr context)
    {
        ArgumentNullException.ThrowIfNull(alc);
        _processContext(alc, context);
    }

    /// <summary>
    /// Suspends processing on the specified context.
    /// </summary>
    public static void SuspendContext(ALContext alc, IntPtr context)
    {
        ArgumentNullException.ThrowIfNull(alc);
        _suspendContext(alc, context);
    }

    /// <summary>
    /// Checks if the specified ALC extension is present on the device.
    /// </summary>
    public static bool IsExtensionPresent(ALContext alc, IntPtr device, string name)
    {
        ArgumentNullException.ThrowIfNull(alc);
        ArgumentNullException.ThrowIfNull(name);
        return _isExtensionPresent(alc, device, name);
    }

    /// <summary>
    /// Retrieves a function pointer for the specified ALC extension.
    /// </summary>
    public static IntPtr GetProcAddress(ALContext alc, IntPtr device, string name)
    {
        ArgumentNullException.ThrowIfNull(alc);
        ArgumentNullException.ThrowIfNull(name);
        return _getProcAddress(alc, device, name);
    }

    /// <summary>
    /// Retrieves the enumeration value corresponding to an ALC extension token name.
    /// </summary>
    public static int GetEnumValue(ALContext alc, IntPtr device, string name)
    {
        ArgumentNullException.ThrowIfNull(alc);
        ArgumentNullException.ThrowIfNull(name);
        return _getEnumValue(alc, device, name);
    }

    /// <summary>
    /// Retrieves a string property from the ALC subsystem or device.
    /// </summary>
    public static string GetContextProperty(ALContext alc, IntPtr device, GetContextString param)
    {
        ArgumentNullException.ThrowIfNull(alc);
        return _getContextStringProperty(alc, device, param);
    }

    /// <summary>
    /// Queries integer values from the ALC subsystem or device into an unmanaged pointer.
    /// </summary>
    public static void GetContextProperty(ALContext alc, IntPtr device, GetContextInteger param, int count, IntPtr data)
    {
        ArgumentNullException.ThrowIfNull(alc);
        _getContextIntegerProperty(alc, device, param, count, data);
    }

    /// <summary>
    /// Queries integer values from the ALC subsystem or device into a managed array using <see cref="GCHandle"/> pinning.
    /// </summary>
    public static void GetContextProperty(ALContext alc, IntPtr device, GetContextInteger param, int count, int[] data)
    {
        ArgumentNullException.ThrowIfNull(alc);
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length < count) throw new ArgumentException("Array length must be at least count.", nameof(data));

        GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            _getContextIntegerProperty(alc, device, param, count, handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    /// <summary>
    /// Convenience helper to retrieve a single integer attribute from the ALC subsystem.
    /// </summary>
    public static int GetContextInteger(ALContext alc, IntPtr device, GetContextInteger param)
    {
        int[] result = new int[1];
        GetContextProperty(alc, device, param, 1, result);
        return result[0];
    }

    // ==========================================
    // AL Bridges - Buffer Data
    // ==========================================

    /// <summary>
    /// Uploads audio data to an OpenAL buffer using an unmanaged pointer.
    /// </summary>
    public static void BufferData(AL al, uint buffer, BufferFormat format, IntPtr data, int size, int frequency)
    {
        ArgumentNullException.ThrowIfNull(al);
        _bufferData(al, buffer, format, data, size, frequency);
    }

    /// <summary>
    /// Uploads audio data from any managed <see cref="Array"/> to an OpenAL buffer using <see cref="GCHandle"/> pinning.
    /// </summary>
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

    // ==========================================
    // AL Bridges - Buffer Allocation
    // ==========================================

    /// <summary>
    /// Generates buffer identifiers into an unmanaged memory address.
    /// </summary>
    public static void GenBuffers(AL al, int count, IntPtr buffers)
    {
        ArgumentNullException.ThrowIfNull(al);
        _genBuffers(al, count, buffers);
    }

    /// <summary>
    /// Generates buffer identifiers into a managed array using <see cref="GCHandle"/> pinning.
    /// </summary>
    public static void GenBuffers(AL al, uint[] buffers)
    {
        ArgumentNullException.ThrowIfNull(al);
        ArgumentNullException.ThrowIfNull(buffers);

        GCHandle handle = GCHandle.Alloc(buffers, GCHandleType.Pinned);
        try
        {
            _genBuffers(al, buffers.Length, handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    /// <summary>
    /// Generates and returns an array of buffer identifiers.
    /// </summary>
    public static uint[] GenBuffers(AL al, int count)
    {
        if (count <= 0) return Array.Empty<uint>();
        uint[] buffers = new uint[count];
        GenBuffers(al, buffers);
        return buffers;
    }

    /// <summary>
    /// Generates a single buffer identifier.
    /// </summary>
    public static uint GenBuffer(AL al)
    {
        uint[] buffers = new uint[1];
        GenBuffers(al, buffers);
        return buffers[0];
    }

    /// <summary>
    /// Deletes buffer identifiers from an unmanaged memory address.
    /// </summary>
    public static void DeleteBuffers(AL al, int count, IntPtr buffers)
    {
        ArgumentNullException.ThrowIfNull(al);
        _deleteBuffers(al, count, buffers);
    }

    /// <summary>
    /// Deletes buffer identifiers in a managed array using <see cref="GCHandle"/> pinning.
    /// </summary>
    public static void DeleteBuffers(AL al, uint[] buffers)
    {
        ArgumentNullException.ThrowIfNull(al);
        ArgumentNullException.ThrowIfNull(buffers);

        GCHandle handle = GCHandle.Alloc(buffers, GCHandleType.Pinned);
        try
        {
            _deleteBuffers(al, buffers.Length, handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    /// <summary>
    /// Deletes a single buffer identifier.
    /// </summary>
    public static void DeleteBuffer(AL al, uint buffer)
    {
        uint[] buffers = new uint[] { buffer };
        DeleteBuffers(al, buffers);
    }

    // ==========================================
    // AL Bridges - Source Allocation
    // ==========================================

    /// <summary>
    /// Generates source identifiers into an unmanaged memory address.
    /// </summary>
    public static void GenSources(AL al, int count, IntPtr sources)
    {
        ArgumentNullException.ThrowIfNull(al);
        _genSources(al, count, sources);
    }

    /// <summary>
    /// Generates source identifiers into a managed array using <see cref="GCHandle"/> pinning.
    /// </summary>
    public static void GenSources(AL al, uint[] sources)
    {
        ArgumentNullException.ThrowIfNull(al);
        ArgumentNullException.ThrowIfNull(sources);

        GCHandle handle = GCHandle.Alloc(sources, GCHandleType.Pinned);
        try
        {
            _genSources(al, sources.Length, handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    /// <summary>
    /// Generates and returns an array of source identifiers.
    /// </summary>
    public static uint[] GenSources(AL al, int count)
    {
        if (count <= 0) return Array.Empty<uint>();
        uint[] sources = new uint[count];
        GenSources(al, sources);
        return sources;
    }

    /// <summary>
    /// Generates a single source identifier.
    /// </summary>
    public static uint GenSource(AL al)
    {
        uint[] sources = new uint[1];
        GenSources(al, sources);
        return sources[0];
    }

    /// <summary>
    /// Deletes source identifiers from an unmanaged memory address.
    /// </summary>
    public static void DeleteSources(AL al, int count, IntPtr sources)
    {
        ArgumentNullException.ThrowIfNull(al);
        _deleteSources(al, count, sources);
    }

    /// <summary>
    /// Deletes source identifiers in a managed array using <see cref="GCHandle"/> pinning.
    /// </summary>
    public static void DeleteSources(AL al, uint[] sources)
    {
        ArgumentNullException.ThrowIfNull(al);
        ArgumentNullException.ThrowIfNull(sources);

        GCHandle handle = GCHandle.Alloc(sources, GCHandleType.Pinned);
        try
        {
            _deleteSources(al, sources.Length, handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    /// <summary>
    /// Deletes a single source identifier.
    /// </summary>
    public static void DeleteSource(AL al, uint source)
    {
        uint[] sources = new uint[] { source };
        DeleteSources(al, sources);
    }

    // ==========================================
    // AL Bridges - Multi-Source Playback Commands
    // ==========================================

    /// <summary>
    /// Plays multiple sources simultaneously using an unmanaged pointer.
    /// </summary>
    public static void SourcePlay(AL al, int count, IntPtr sources)
    {
        ArgumentNullException.ThrowIfNull(al);
        _sourcePlay(al, count, sources);
    }

    /// <summary>
    /// Plays multiple sources simultaneously using a pinned array.
    /// </summary>
    public static void SourcePlay(AL al, uint[] sources)
    {
        ArgumentNullException.ThrowIfNull(al);
        ArgumentNullException.ThrowIfNull(sources);

        GCHandle handle = GCHandle.Alloc(sources, GCHandleType.Pinned);
        try
        {
            _sourcePlay(al, sources.Length, handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    /// <summary>
    /// Plays a single source.
    /// </summary>
    public static void SourcePlay(AL al, uint source)
    {
        uint[] sources = new uint[] { source };
        SourcePlay(al, sources);
    }

    /// <summary>
    /// Pauses multiple sources simultaneously using an unmanaged pointer.
    /// </summary>
    public static void SourcePause(AL al, int count, IntPtr sources)
    {
        ArgumentNullException.ThrowIfNull(al);
        _sourcePause(al, count, sources);
    }

    /// <summary>
    /// Pauses multiple sources simultaneously using a pinned array.
    /// </summary>
    public static void SourcePause(AL al, uint[] sources)
    {
        ArgumentNullException.ThrowIfNull(al);
        ArgumentNullException.ThrowIfNull(sources);

        GCHandle handle = GCHandle.Alloc(sources, GCHandleType.Pinned);
        try
        {
            _sourcePause(al, sources.Length, handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    /// <summary>
    /// Pauses a single source.
    /// </summary>
    public static void SourcePause(AL al, uint source)
    {
        uint[] sources = new uint[] { source };
        SourcePause(al, sources);
    }

    /// <summary>
    /// Stops multiple sources simultaneously using an unmanaged pointer.
    /// </summary>
    public static void SourceStop(AL al, int count, IntPtr sources)
    {
        ArgumentNullException.ThrowIfNull(al);
        _sourceStop(al, count, sources);
    }

    /// <summary>
    /// Stops multiple sources simultaneously using a pinned array.
    /// </summary>
    public static void SourceStop(AL al, uint[] sources)
    {
        ArgumentNullException.ThrowIfNull(al);
        ArgumentNullException.ThrowIfNull(sources);

        GCHandle handle = GCHandle.Alloc(sources, GCHandleType.Pinned);
        try
        {
            _sourceStop(al, sources.Length, handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    /// <summary>
    /// Stops a single source.
    /// </summary>
    public static void SourceStop(AL al, uint source)
    {
        uint[] sources = new uint[] { source };
        SourceStop(al, sources);
    }

    /// <summary>
    /// Rewinds multiple sources simultaneously using an unmanaged pointer.
    /// </summary>
    public static void SourceRewind(AL al, int count, IntPtr sources)
    {
        ArgumentNullException.ThrowIfNull(al);
        _sourceRewind(al, count, sources);
    }

    /// <summary>
    /// Rewinds multiple sources simultaneously using a pinned array.
    /// </summary>
    public static void SourceRewind(AL al, uint[] sources)
    {
        ArgumentNullException.ThrowIfNull(al);
        ArgumentNullException.ThrowIfNull(sources);

        GCHandle handle = GCHandle.Alloc(sources, GCHandleType.Pinned);
        try
        {
            _sourceRewind(al, sources.Length, handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    /// <summary>
    /// Rewinds a single source.
    /// </summary>
    public static void SourceRewind(AL al, uint source)
    {
        uint[] sources = new uint[] { source };
        SourceRewind(al, sources);
    }

    // ==========================================
    // AL Bridges - Streaming Buffer Queues
    // ==========================================

    /// <summary>
    /// Queues buffers onto a source using an unmanaged pointer.
    /// </summary>
    public static void SourceQueueBuffers(AL al, uint source, int count, IntPtr buffers)
    {
        ArgumentNullException.ThrowIfNull(al);
        _sourceQueueBuffers(al, source, count, buffers);
    }

    /// <summary>
    /// Queues buffers onto a source using a managed array with <see cref="GCHandle"/> pinning.
    /// </summary>
    public static void SourceQueueBuffers(AL al, uint source, uint[] buffers)
    {
        ArgumentNullException.ThrowIfNull(al);
        ArgumentNullException.ThrowIfNull(buffers);

        GCHandle handle = GCHandle.Alloc(buffers, GCHandleType.Pinned);
        try
        {
            _sourceQueueBuffers(al, source, buffers.Length, handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    /// <summary>
    /// Queues a single buffer onto a source.
    /// </summary>
    public static void SourceQueueBuffers(AL al, uint source, uint buffer)
    {
        uint[] buffers = new uint[] { buffer };
        SourceQueueBuffers(al, source, buffers);
    }

    /// <summary>
    /// Unqueues processed buffers from a source into an unmanaged pointer.
    /// </summary>
    public static void SourceUnqueueBuffers(AL al, uint source, int count, IntPtr buffers)
    {
        ArgumentNullException.ThrowIfNull(al);
        _sourceUnqueueBuffers(al, source, count, buffers);
    }

    /// <summary>
    /// Unqueues processed buffers from a source into a managed array using <see cref="GCHandle"/> pinning.
    /// </summary>
    public static void SourceUnqueueBuffers(AL al, uint source, uint[] buffers)
    {
        ArgumentNullException.ThrowIfNull(al);
        ArgumentNullException.ThrowIfNull(buffers);

        GCHandle handle = GCHandle.Alloc(buffers, GCHandleType.Pinned);
        try
        {
            _sourceUnqueueBuffers(al, source, buffers.Length, handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    /// <summary>
    /// Unqueues and returns a single processed buffer from a source.
    /// </summary>
    public static uint SourceUnqueueBuffers(AL al, uint source)
    {
        uint[] buffers = new uint[1];
        SourceUnqueueBuffers(al, source, buffers);
        return buffers[0];
    }

    // ==========================================
    // AL Bridges - Buffer Properties
    // ==========================================

    public static void GetBufferProperty(AL al, uint buffer, BufferFloat param, IntPtr value)
    {
        ArgumentNullException.ThrowIfNull(al);
        _getBufferFloatProperty(al, buffer, param, value);
    }

    public static void GetBufferProperty(AL al, uint buffer, BufferFloat param, float[] values)
    {
        ArgumentNullException.ThrowIfNull(al);
        ArgumentNullException.ThrowIfNull(values);

        GCHandle handle = GCHandle.Alloc(values, GCHandleType.Pinned);
        try
        {
            _getBufferFloatProperty(al, buffer, param, handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    public static void GetBufferProperty(AL al, uint buffer, BufferFloat param, out float value)
    {
        float[] arr = new float[1];
        GetBufferProperty(al, buffer, param, arr);
        value = arr[0];
    }

    public static void GetBufferProperty(AL al, uint buffer, GetBufferInteger param, IntPtr value)
    {
        ArgumentNullException.ThrowIfNull(al);
        _getBufferIntegerProperty(al, buffer, param, value);
    }

    public static void GetBufferProperty(AL al, uint buffer, GetBufferInteger param, int[] values)
    {
        ArgumentNullException.ThrowIfNull(al);
        ArgumentNullException.ThrowIfNull(values);

        GCHandle handle = GCHandle.Alloc(values, GCHandleType.Pinned);
        try
        {
            _getBufferIntegerProperty(al, buffer, param, handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    public static void GetBufferProperty(AL al, uint buffer, GetBufferInteger param, out int value)
    {
        int[] arr = new int[1];
        GetBufferProperty(al, buffer, param, arr);
        value = arr[0];
    }

    public static void SetBufferProperty(AL al, uint buffer, BufferInteger param, IntPtr value)
    {
        ArgumentNullException.ThrowIfNull(al);
        _setBufferIntegerProperty(al, buffer, param, value);
    }

    public static void SetBufferProperty(AL al, uint buffer, BufferInteger param, int[] values)
    {
        ArgumentNullException.ThrowIfNull(al);
        ArgumentNullException.ThrowIfNull(values);

        GCHandle handle = GCHandle.Alloc(values, GCHandleType.Pinned);
        try
        {
            _setBufferIntegerProperty(al, buffer, param, handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    public static void SetBufferProperty(AL al, uint buffer, BufferInteger param, int value)
    {
        int[] arr = new int[] { value };
        SetBufferProperty(al, buffer, param, arr);
    }

    public static void SetBufferProperty(AL al, uint buffer, BufferVector3 param, IntPtr value)
    {
        ArgumentNullException.ThrowIfNull(al);
        _setBufferVector3Property(al, buffer, param, value);
    }

    public static void SetBufferProperty(AL al, uint buffer, BufferVector3 param, float[] values)
    {
        ArgumentNullException.ThrowIfNull(al);
        ArgumentNullException.ThrowIfNull(values);

        GCHandle handle = GCHandle.Alloc(values, GCHandleType.Pinned);
        try
        {
            _setBufferVector3Property(al, buffer, param, handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    public static void SetBufferProperty(AL al, uint buffer, BufferVector3 param, float x, float y, float z)
    {
        float[] arr = new float[] { x, y, z };
        SetBufferProperty(al, buffer, param, arr);
    }

    // ==========================================
    // AL Bridges - Listener Properties
    // ==========================================

    public static void GetListenerProperty(AL al, ListenerFloatArray param, IntPtr value)
    {
        ArgumentNullException.ThrowIfNull(al);
        _getListenerFloatArrayProperty(al, param, value);
    }

    public static void GetListenerProperty(AL al, ListenerFloatArray param, float[] values)
    {
        ArgumentNullException.ThrowIfNull(al);
        ArgumentNullException.ThrowIfNull(values);

        GCHandle handle = GCHandle.Alloc(values, GCHandleType.Pinned);
        try
        {
            _getListenerFloatArrayProperty(al, param, handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    public static void GetListenerProperty(AL al, ListenerInteger param, IntPtr value)
    {
        ArgumentNullException.ThrowIfNull(al);
        _getListenerIntegerProperty(al, param, value);
    }

    public static void GetListenerProperty(AL al, ListenerInteger param, int[] values)
    {
        ArgumentNullException.ThrowIfNull(al);
        ArgumentNullException.ThrowIfNull(values);

        GCHandle handle = GCHandle.Alloc(values, GCHandleType.Pinned);
        try
        {
            _getListenerIntegerProperty(al, param, handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    public static void GetListenerProperty(AL al, ListenerInteger param, out int value)
    {
        int[] arr = new int[1];
        GetListenerProperty(al, param, arr);
        value = arr[0];
    }

    public static void SetListenerProperty(AL al, ListenerFloatArray param, IntPtr value)
    {
        ArgumentNullException.ThrowIfNull(al);
        _setListenerFloatArrayProperty(al, param, value);
    }

    public static void SetListenerProperty(AL al, ListenerFloatArray param, float[] values)
    {
        ArgumentNullException.ThrowIfNull(al);
        ArgumentNullException.ThrowIfNull(values);

        GCHandle handle = GCHandle.Alloc(values, GCHandleType.Pinned);
        try
        {
            _setListenerFloatArrayProperty(al, param, handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    public static void SetListenerProperty(AL al, ListenerInteger param, IntPtr value)
    {
        ArgumentNullException.ThrowIfNull(al);
        _setListenerIntegerProperty(al, param, value);
    }

    public static void SetListenerProperty(AL al, ListenerInteger param, int[] values)
    {
        ArgumentNullException.ThrowIfNull(al);
        ArgumentNullException.ThrowIfNull(values);

        GCHandle handle = GCHandle.Alloc(values, GCHandleType.Pinned);
        try
        {
            _setListenerIntegerProperty(al, param, handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    public static void SetListenerProperty(AL al, ListenerInteger param, int value)
    {
        int[] arr = new int[] { value };
        SetListenerProperty(al, param, arr);
    }

    // ==========================================
    // AL Bridges - Source Properties
    // ==========================================

    public static void GetSourceProperty(AL al, uint source, GetSourceInteger param, IntPtr value)
    {
        ArgumentNullException.ThrowIfNull(al);
        _getSourceIntegerProperty(al, source, param, value);
    }

    public static void GetSourceProperty(AL al, uint source, GetSourceInteger param, int[] values)
    {
        ArgumentNullException.ThrowIfNull(al);
        ArgumentNullException.ThrowIfNull(values);

        GCHandle handle = GCHandle.Alloc(values, GCHandleType.Pinned);
        try
        {
            _getSourceIntegerProperty(al, source, param, handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    public static void GetSourceProperty(AL al, uint source, GetSourceInteger param, out int value)
    {
        int[] arr = new int[1];
        GetSourceProperty(al, source, param, arr);
        value = arr[0];
    }

    public static void GetSourceProperty(AL al, uint source, SourceFloat param, IntPtr value)
    {
        ArgumentNullException.ThrowIfNull(al);
        _getSourceFloatProperty(al, source, param, value);
    }

    public static void GetSourceProperty(AL al, uint source, SourceFloat param, float[] values)
    {
        ArgumentNullException.ThrowIfNull(al);
        ArgumentNullException.ThrowIfNull(values);

        GCHandle handle = GCHandle.Alloc(values, GCHandleType.Pinned);
        try
        {
            _getSourceFloatProperty(al, source, param, handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    public static void GetSourceProperty(AL al, uint source, SourceFloat param, out float value)
    {
        float[] arr = new float[1];
        GetSourceProperty(al, source, param, arr);
        value = arr[0];
    }

    public static void SetSourceProperty(AL al, uint source, SourceInteger param, IntPtr value)
    {
        ArgumentNullException.ThrowIfNull(al);
        _setSourceIntegerProperty(al, source, param, value);
    }

    public static void SetSourceProperty(AL al, uint source, SourceInteger param, int[] values)
    {
        ArgumentNullException.ThrowIfNull(al);
        ArgumentNullException.ThrowIfNull(values);

        GCHandle handle = GCHandle.Alloc(values, GCHandleType.Pinned);
        try
        {
            _setSourceIntegerProperty(al, source, param, handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    public static void SetSourceProperty(AL al, uint source, SourceInteger param, uint[] values)
    {
        ArgumentNullException.ThrowIfNull(al);
        ArgumentNullException.ThrowIfNull(values);

        GCHandle handle = GCHandle.Alloc(values, GCHandleType.Pinned);
        try
        {
            _setSourceIntegerProperty(al, source, param, handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    public static void SetSourceProperty(AL al, uint source, SourceInteger param, int value)
    {
        int[] arr = new int[] { value };
        SetSourceProperty(al, source, param, arr);
    }

    public static void SetSourceProperty(AL al, uint source, SourceVector3 param, IntPtr value)
    {
        ArgumentNullException.ThrowIfNull(al);
        _setSourceVector3Property(al, source, param, value);
    }

    public static void SetSourceProperty(AL al, uint source, SourceVector3 param, float[] values)
    {
        ArgumentNullException.ThrowIfNull(al);
        ArgumentNullException.ThrowIfNull(values);

        GCHandle handle = GCHandle.Alloc(values, GCHandleType.Pinned);
        try
        {
            _setSourceVector3Property(al, source, param, handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    public static void SetSourceProperty(AL al, uint source, SourceVector3 param, float x, float y, float z)
    {
        float[] arr = new float[] { x, y, z };
        SetSourceProperty(al, source, param, arr);
    }
}
