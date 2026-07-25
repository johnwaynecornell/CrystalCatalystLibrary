using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using JWCEssentials.net;

namespace CrystalCatalystLibrary.net;
public partial class DragDropData : DataInterchange
{
    protected override void Dispose(bool disposing)
    {
    if (!_disposed)
    {
    if (Handle != IntPtr.Zero)
    {
    // 1. Remove from our tracking dictionary
    InstanceCache.TryRemove(Handle, out _);

    // 2. Call the native destroy function generated in the Imports class
    // (Assuming you mapped the destroy function in your generator)
    // 3. Clear the handle so we don't double-free
    Handle = IntPtr.Zero;
    }

    _disposed = true;
    }
    }
    // Thread-safe tracker mapping the native IntPtr to our managed wrapper.
    // We use WeakReference so we don't cause memory leaks if the user drops the reference.
    private static readonly ConcurrentDictionary<IntPtr, WeakReference<DragDropData>> InstanceCache = 
    new ConcurrentDictionary<IntPtr, WeakReference<DragDropData>>();

    // Tracks whether this specific instance has been disposed
    private bool _disposed = false;

    /// <summary>
    /// This cast method should be called by the generated code right after a native "Create" function 
    /// returns a new IntPtr, OR when a native callback passes an IntPtr back to C#.
    /// </summary>
    public static explicit operator DragDropData(IntPtr handle)           
    {
    if (handle == IntPtr.Zero) throw new NullReferenceException("IntPtr cannot be null");

    // Try to find an existing alive wrapper
    if (InstanceCache.TryGetValue(handle, out var weakRef) && weakRef.TryGetTarget(out var existingContext))
    {
    return existingContext;
    }

    // If we didn't find one, or it was garbage collected, create a new one
    var newContext = new DragDropData(handle);
    InstanceCache[handle] = new WeakReference<DragDropData>(newContext);

    return newContext;
    }
    public DragDropData(IntPtr Handle) : base(Handle)
    {
    }
    public static string DragActionsString(DragActions  actions)
    {
        string Ret = ( string ) Imports.DragDropData_DragActionsString((DragActions)actions);
        return Ret;
    }
    public new static DragDropData Create()
    {
        DragDropData Ret = ( DragDropData ) Imports.DragDropData_Create();
        return Ret;
    }

    public new class Imports
    {
        // utf8_string_struct DragDropData_DragActionsString(DragActions actions)
        [DllImport("CrystalCatalystLibrary")]
        public static extern utf8_string_struct DragDropData_DragActionsString(DragActions actions);

        // P_INSTANCE DragDropData DragDropData_Create()
        [DllImport("CrystalCatalystLibrary")]
        public static extern IntPtr DragDropData_Create();

    }
}
