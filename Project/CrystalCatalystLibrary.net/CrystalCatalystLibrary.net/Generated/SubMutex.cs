using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using JWCEssentials.net;

namespace CrystalCatalystLibrary.net;

public partial class SubMutex : IDisposable
{
    /// <summary>
    /// Cleans up the native resource and removes it from the cache.
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
                Close();
                // 3. Clear the handle so we don't double-free
                Handle = IntPtr.Zero;
            }

            _disposed = true;
        }
    }

    // Thread-safe tracker mapping the native IntPtr to our managed wrapper.
    // We use WeakReference so we don't cause memory leaks if the user drops the reference.
    private static readonly ConcurrentDictionary<IntPtr, WeakReference<SubMutex>> InstanceCache =
        new ConcurrentDictionary<IntPtr, WeakReference<SubMutex>>();

    // Tracks whether this specific instance has been disposed
    private bool _disposed = false;

    /// <summary>
    /// This cast method should be called by the generated code right after a native "Create" function 
    /// returns a new IntPtr, OR when a native callback passes an IntPtr back to C#.
    /// </summary>
    public static explicit operator SubMutex(IntPtr handle)
    {
        if (handle == IntPtr.Zero) throw new NullReferenceException("IntPtr cannot be null");

        // Try to find an existing alive wrapper
        if (InstanceCache.TryGetValue(handle, out var weakRef) && weakRef.TryGetTarget(out var existingContext))
        {
            return existingContext;
        }

        // If we didn't find one, or it was garbage collected, create a new one
        var newContext = new SubMutex(handle);
        InstanceCache[handle] = new WeakReference<SubMutex>(newContext);

        return newContext;
    }

    public IntPtr Handle;

    public SubMutex(IntPtr Handle)
    {
        this.Handle = Handle;
    }

    public static bool Size(out IntPtr sz)
    {
        bool Ret = (bool)Imports.SubMutex_Size(out sz);
        return Ret;
    }

    public bool Init(string name)
    {
        utf8_string_struct param_name = name;
        bool Ret = (bool)Imports.SubMutex_Init(Handle, ref param_name);
        return Ret;
    }

    public bool hasClosed = false;

    public bool Close()
    {
        if (hasClosed) return false;
        hasClosed = true;
        bool Ret = (bool)Imports.SubMutex_Close(Handle);
        return Ret;
    }

    public bool Lock()
    {
        bool Ret = (bool)Imports.SubMutex_Lock(Handle);
        return Ret;
    }

    public bool Unlock()
    {
        bool Ret = (bool)Imports.SubMutex_Unlock(Handle);
        return Ret;
    }

    public class Imports
    {
        // bool SubMutex_Size(P_OUT size_t sz)
        [DllImport("CrystalCatalystLibrary")]
        public static extern bool SubMutex_Size(out IntPtr sz);

        // bool SubMutex_Init(P_INSTANCE void spiderMutex, utf8_string_struct name)
        [DllImport("CrystalCatalystLibrary")]
        public static extern bool SubMutex_Init(IntPtr spiderMutex, ref utf8_string_struct name);

        // bool SubMutex_Close(P_INSTANCE void spiderMutex)
        [DllImport("CrystalCatalystLibrary")]
        public static extern bool SubMutex_Close(IntPtr spiderMutex);

        // bool SubMutex_Lock(P_INSTANCE void spiderMutex)
        [DllImport("CrystalCatalystLibrary")]
        public static extern bool SubMutex_Lock(IntPtr spiderMutex);

        // bool SubMutex_Unlock(P_INSTANCE void spiderMutex)
        [DllImport("CrystalCatalystLibrary")]
        public static extern bool SubMutex_Unlock(IntPtr spiderMutex);

    }
}
