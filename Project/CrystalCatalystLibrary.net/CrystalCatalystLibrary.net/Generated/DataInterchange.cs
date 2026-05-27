using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using JWCEssentials.net;

namespace CrystalCatalystLibrary.net;

public class DataInterchange : IDisposable
{
    // Thread-safe tracker mapping the native IntPtr to our managed wrapper.
    // We use WeakReference so we don't cause memory leaks if the user drops the reference.
    private static readonly ConcurrentDictionary<IntPtr, WeakReference<DataInterchange>> InstanceCache = new();

    // Tracks whether this specific instance has been disposed
    private bool _disposed;
    public IntPtr Handle;
    public bool hasClosed;

    public DataInterchange(IntPtr Handle)
    {
        this.Handle = Handle;
    }

    /// <summary>
    ///     Cleans up the native resource and removes it from the cache.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (Handle != IntPtr.Zero)
            {
                // 1. Remove from our tracking dictionary
                InstanceCache.TryRemove(Handle, out _);

                // 2. Call the native destroy function generated in the Imports class
                // (Assuming you mapped the destroy function in your generator)
                Free();
                // 3. Clear the handle so we don't double-free
                Handle = IntPtr.Zero;
            }

            _disposed = true;
        }
    }

    /// <summary>
    ///     This cast method should be called by the generated code right after a native "Create" function
    ///     returns a new IntPtr, OR when a native callback passes an IntPtr back to C#.
    /// </summary>
    public static explicit operator DataInterchange(IntPtr handle)
    {
        if (handle == IntPtr.Zero) throw new NullReferenceException("IntPtr cannot be null");

        // Try to find an existing alive wrapper
        if (InstanceCache.TryGetValue(handle, out var weakRef) && weakRef.TryGetTarget(out var existingContext))
            return existingContext;

        // If we didn't find one, or it was garbage collected, create a new one
        var newContext = new DataInterchange(handle);
        InstanceCache[handle] = new WeakReference<DataInterchange>(newContext);

        return newContext;
    }

    public static DataInterchange Create()
    {
        var Ret = (DataInterchange)Imports.DataInterchange_Create();
        return Ret;
    }

    public void Free()
    {
        if (hasClosed) return;
        hasClosed = true;
        Imports.DataInterchange_Free(Handle);
    }

    public IntPtr FormatAdd(string format)
    {
        utf8_string_struct param_format = format;
        var Ret = Imports.DataInterchange_FormatAdd(Handle, ref param_format);
        return Ret;
    }

    public bool FormatExists(string format)
    {
        utf8_string_struct param_format = format;
        var Ret = Imports.DataInterchange_FormatExists(Handle, ref param_format);
        return Ret;
    }

    public IntPtr FormatEnum()
    {
        var Ret = Imports.DataInterchange_FormatEnum(Handle);
        return Ret;
    }

    public static IntPtr FormatEnumNext(IntPtr node)
    {
        var Ret = Imports.DataInterchange_FormatEnumNext(node);
        return Ret;
    }

    public static void FormatEnumText(IntPtr node, out string text)
    {
        utf8_string_struct param_text;
        Imports.DataInterchange_FormatEnumText(node, out param_text);
        text = param_text;
    }

    public IntPtr ItemsFormatRemove(IntPtr node)
    {
        var Ret = Imports.DataInterchange_ItemsFormatRemove(Handle, node);
        return Ret;
    }

    public bool isClipboard()
    {
        var Ret = Imports.DataInterchange_isClipboard(Handle);
        return Ret;
    }

    public void Select(string format)
    {
        utf8_string_struct param_format = format;
        Imports.DataInterchange_Select(Handle, ref param_format);
    }

    public void SelectionReveal(out string format, out IntPtr data, out IntPtr size)
    {
        utf8_string_struct param_format;
        Imports.DataInterchange_SelectionReveal(Handle, out param_format, out data, out size);
        format = param_format;
    }

    public void SelectionSet(string format, IntPtr data, IntPtr size)
    {
        utf8_string_struct param_format = format;
        Imports.DataInterchange_SelectionSet(Handle, ref param_format, data, size);
    }

    public class Imports
    {
        // P_INSTANCE DataInterchange DataInterchange_Create()
        [DllImport("CrystalCatalystLibrary")]
        public static extern IntPtr DataInterchange_Create();

        // void DataInterchange_Free(P_INSTANCE DataInterchange drag)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void DataInterchange_Free(IntPtr drag);

        // P_INSTANCE DataInterchange::Node DataInterchange_FormatAdd(P_INSTANCE DataInterchange drop, utf8_string_struct format)
        [DllImport("CrystalCatalystLibrary")]
        public static extern IntPtr DataInterchange_FormatAdd(IntPtr drop, ref utf8_string_struct format);

        // bool DataInterchange_FormatExists(P_INSTANCE DataInterchange data, utf8_string_struct format)
        [DllImport("CrystalCatalystLibrary")]
        public static extern bool DataInterchange_FormatExists(IntPtr data, ref utf8_string_struct format);

        // P_INSTANCE DataInterchange::Node DataInterchange_FormatEnum(P_INSTANCE DataInterchange drop)
        [DllImport("CrystalCatalystLibrary")]
        public static extern IntPtr DataInterchange_FormatEnum(IntPtr drop);

        // P_INSTANCE DataInterchange::Node DataInterchange_FormatEnumNext(P_INSTANCE DataInterchange::Node node)
        [DllImport("CrystalCatalystLibrary")]
        public static extern IntPtr DataInterchange_FormatEnumNext(IntPtr node);

        // void DataInterchange_FormatEnumText(P_INSTANCE DataInterchange::Node node, P_OUT utf8_string_struct text)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void DataInterchange_FormatEnumText(IntPtr node, out utf8_string_struct text);

        // P_INSTANCE DataInterchange::Node DataInterchange_ItemsFormatRemove(P_INSTANCE DataInterchange drop, P_INSTANCE DataInterchange::Node node)
        [DllImport("CrystalCatalystLibrary")]
        public static extern IntPtr DataInterchange_ItemsFormatRemove(IntPtr drop, IntPtr node);

        // bool DataInterchange_isClipboard(P_INSTANCE DataInterchange data)
        [DllImport("CrystalCatalystLibrary")]
        public static extern bool DataInterchange_isClipboard(IntPtr data);

        // void DataInterchange_Select(P_INSTANCE DataInterchange data, utf8_string_struct format)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void DataInterchange_Select(IntPtr data, ref utf8_string_struct format);

        // void DataInterchange_SelectionReveal(P_INSTANCE DataInterchange drag, P_OUT utf8_string_struct format, P_OUT P_INSTANCE void data, P_OUT size_t size)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void DataInterchange_SelectionReveal(IntPtr drag, out utf8_string_struct format,
            out IntPtr data, out IntPtr size);

        // void DataInterchange_SelectionSet(P_INSTANCE DataInterchange drag, utf8_string_struct format, P_INSTANCE void data, size_t size)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void DataInterchange_SelectionSet(IntPtr drag, ref utf8_string_struct format, IntPtr data,
            IntPtr size);
    }
}