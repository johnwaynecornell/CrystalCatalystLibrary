using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using JWCEssentials.net;

namespace CrystalCatalystLibrary.net;
public partial class CrystalWindow : IDisposable
{
    /// <summary>
    /// Cleans up the native resource and removes it from the cache.
    /// </summary>
    public void Dispose()
    {
    Dispose(true);
    GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing){
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
    private static readonly ConcurrentDictionary<IntPtr, WeakReference<CrystalWindow>> InstanceCache = 
    new ConcurrentDictionary<IntPtr, WeakReference<CrystalWindow>>();

    // Tracks whether this specific instance has been disposed
    private bool _disposed = false;

    /// <summary>
    /// This cast method should be called by the generated code right after a native "Create" function 
    /// returns a new IntPtr, OR when a native callback passes an IntPtr back to C#.
    /// </summary>
    public static explicit operator CrystalWindow(IntPtr handle)           
    {
    if (handle == IntPtr.Zero)
    return null;

    // Try to find an existing alive wrapper
    if (InstanceCache.TryGetValue(handle, out var weakRef) && weakRef.TryGetTarget(out var existingContext))
    {
    return existingContext;
    }

    // If we didn't find one, or it was garbage collected, create a new one
    var newContext = new CrystalWindow(handle);
    InstanceCache[handle] = new WeakReference<CrystalWindow>(newContext);

    return newContext;
    }
    public class Imports
    {
        // P_INSTANCE DataInterchange CrystalWindow_ClipboardPaste(P_INSTANCE WindowHandle handle)
        [DllImport("CrystalCatalystLibrary")]
        public static extern IntPtr  CrystalWindow_ClipboardPaste(IntPtr handle);

        // void CrystalWindow_ClipboardCopy(P_INSTANCE WindowHandle handle, P_INSTANCE DataInterchange data)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void  CrystalWindow_ClipboardCopy(IntPtr handle, IntPtr data);

        // void (*)(P_INSTANCE DataInterchange data)
        public delegate void ClipboardCopyWithCallback_provide(IntPtr data);

        // void CrystalWindow_ClipboardCopyWithCallback(void (*)(P_INSTANCE DataInterchange data) provide, P_INSTANCE DataInterchange data)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void  CrystalWindow_ClipboardCopyWithCallback(ClipboardCopyWithCallback_provide provide, IntPtr data);

        // void CrystalWindow_ClipboardCopyPersist(P_INSTANCE WindowHandle handle, P_INSTANCE DataInterchange data)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void  CrystalWindow_ClipboardCopyPersist(IntPtr handle, IntPtr data);

        // void CrystalWindow_ClipboardClear()
        [DllImport("CrystalCatalystLibrary")]
        public static extern void  CrystalWindow_ClipboardClear();

        // void CrystalWindow_RegisterDragTarget(P_INSTANCE WindowHandle window_handle)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void  CrystalWindow_RegisterDragTarget(IntPtr window_handle);

        // void CrystalWindow_DragStart(P_INSTANCE WindowHandle handle, P_INSTANCE DragDropData data, int32_t x, int32_t y)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void  CrystalWindow_DragStart(IntPtr handle, IntPtr data, int x, int y);

        // void CrystalWindow_DragChoose(P_INSTANCE WindowHandle handle, P_INSTANCE DragDropData data, utf8_string_struct fmt)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void  CrystalWindow_DragChoose(IntPtr handle, IntPtr data, ref utf8_string_struct fmt);

        // P_INSTANCE WindowHandle CrystalWindow_Create(int32_t width, int32_t height, utf8_string_struct title)
        [DllImport("CrystalCatalystLibrary")]
        public static extern IntPtr  CrystalWindow_Create(int width, int height, ref utf8_string_struct title);

        // P_INSTANCE WindowHandle CrystalWindow_CreateSimple(int32_t width, int32_t height, utf8_string_struct title)
        [DllImport("CrystalCatalystLibrary")]
        public static extern IntPtr  CrystalWindow_CreateSimple(int width, int height, ref utf8_string_struct title);

        // void CrystalWindow_ApplicationRetain(P_INSTANCE WindowHandle window_handle)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void  CrystalWindow_ApplicationRetain(IntPtr window_handle);

        // void CrystalWindow_ApplicationRelease(P_INSTANCE WindowHandle window_handle)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void  CrystalWindow_ApplicationRelease(IntPtr window_handle);

        // void CrystalWindow_PresentImage(P_INSTANCE WindowHandle window_handle, utf8_string_struct pixformat, P_ELEMENTS void pixdata, size_t pixdata_length, int32_t width, int32_t height)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void  CrystalWindow_PresentImage(IntPtr window_handle, ref utf8_string_struct pixformat, IntPtr pixdata, IntPtr pixdata_length, int width, int height);

        // void CrystalWindow_QueueRedraw(P_INSTANCE WindowHandle window_handle)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void  CrystalWindow_QueueRedraw(IntPtr window_handle);

        // void CrystalWindow_MouseCapture(P_INSTANCE WindowHandle window_handle)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void  CrystalWindow_MouseCapture(IntPtr window_handle);

        // void CrystalWindow_MouseRelease(P_INSTANCE WindowHandle window_handle)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void  CrystalWindow_MouseRelease(IntPtr window_handle);

        // void CrystalWindow_GLInit(P_INSTANCE WindowHandle window_handle)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void  CrystalWindow_GLInit(IntPtr window_handle);

        // void CrystalWindow_Show(P_INSTANCE WindowHandle window_handle, bool restore)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void  CrystalWindow_Show(IntPtr window_handle, bool restore);

        // void CrystalWindow_Close(P_INSTANCE WindowHandle window_handle)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void  CrystalWindow_Close(IntPtr window_handle);

        // void CrystalWindow_PostClose(P_INSTANCE WindowHandle window_handle)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void  CrystalWindow_PostClose(IntPtr window_handle);

        // bool CrystalWindow_SetMessaqeHandler(P_INSTANCE WindowHandle window_handle, utf8_string_struct handler_name, P_INSTANCE void handler)
        [DllImport("CrystalCatalystLibrary")]
        public static extern bool  CrystalWindow_SetMessaqeHandler(IntPtr window_handle, ref utf8_string_struct handler_name, IntPtr handler);

        // double CrystalWindow_uptimeSeconds(P_INSTANCE WindowHandle window_handle)
        [DllImport("CrystalCatalystLibrary")]
        public static extern double  CrystalWindow_uptimeSeconds(IntPtr window_handle);

        // void CrystalWindow_uptimeReset(P_INSTANCE WindowHandle window_handle)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void  CrystalWindow_uptimeReset(IntPtr window_handle);

        // void (*on_draw)(P_INSTANCE WindowHandle window_handle)
        public delegate void Delegate_on_draw(IntPtr window_handle);

        // void (*on_key_down)(P_INSTANCE WindowHandle window_handle, int32_t keycode)
        public delegate void Delegate_on_key_down(IntPtr window_handle, int keycode);

        // void (*on_key_up)(P_INSTANCE WindowHandle window_handle, int32_t keycode)
        public delegate void Delegate_on_key_up(IntPtr window_handle, int keycode);

        // void (*on_mouse_move)(P_INSTANCE WindowHandle window_handle, int32_t x, int32_t y)
        public delegate void Delegate_on_mouse_move(IntPtr window_handle, int x, int y);

        // void (*on_mouse_down)(P_INSTANCE WindowHandle window_handle, int32_t button, int32_t x, int32_t y)
        public delegate void Delegate_on_mouse_down(IntPtr window_handle, int button, int x, int y);

        // void (*on_mouse_up)(P_INSTANCE WindowHandle window_handle, int32_t button, int32_t x, int32_t y)
        public delegate void Delegate_on_mouse_up(IntPtr window_handle, int button, int x, int y);

        // void (*on_resize)(P_INSTANCE WindowHandle window_handle, int32_t width, int32_t height)
        public delegate void Delegate_on_resize(IntPtr window_handle, int width, int height);

        // void (*on_close)(P_INSTANCE WindowHandle window_handle)
        public delegate void Delegate_on_close(IntPtr window_handle);

        // void (*on_focus_in)(P_INSTANCE WindowHandle window_handle)
        public delegate void Delegate_on_focus_in(IntPtr window_handle);

        // void (*on_focus_out)(P_INSTANCE WindowHandle window_handle)
        public delegate void Delegate_on_focus_out(IntPtr window_handle);

        // void (*on_drag_receive_start)(P_INSTANCE WindowHandle window_handle, P_INSTANCE DragDropData data)
        public delegate void Delegate_on_drag_receive_start(IntPtr window_handle, IntPtr data);

        // void (*on_drag_receive_enter)(P_INSTANCE WindowHandle window_handle, P_INSTANCE DragDropData data)
        public delegate void Delegate_on_drag_receive_enter(IntPtr window_handle, IntPtr data);

        // void (*on_drag_receive_motion)(P_INSTANCE WindowHandle window_handle, P_INSTANCE DragDropData data, int32_t x, int32_t y, uint32_t key_state)
        public delegate void Delegate_on_drag_receive_motion(IntPtr window_handle, IntPtr data, int x, int y, uint key_state);

        // void (*on_drag_receive_leave)(P_INSTANCE WindowHandle window_handle, P_INSTANCE DragDropData data)
        public delegate void Delegate_on_drag_receive_leave(IntPtr window_handle, IntPtr data);

        // void (*on_drag_receive_drop)(P_INSTANCE WindowHandle window_handle, P_INSTANCE DragDropData data)
        public delegate void Delegate_on_drag_receive_drop(IntPtr window_handle, IntPtr data);

        // utf8_string_struct (*on_drag_receive_select)(P_INSTANCE WindowHandle window_handle, P_INSTANCE DragDropData data)
        public delegate string Delegate_on_drag_receive_select(IntPtr window_handle, IntPtr data);

        // void (*on_drag_provide_status)(P_INSTANCE WindowHandle window_handle, P_INSTANCE DragDropData data)
        public delegate void Delegate_on_drag_provide_status(IntPtr window_handle, IntPtr data);

        // void (*on_drag_provide_chosen)(P_INSTANCE WindowHandle window_handle, P_INSTANCE DragDropData data, utf8_string_struct format)
        public delegate void Delegate_on_drag_provide_chosen(IntPtr window_handle, IntPtr data, ref utf8_string_struct format);

        // void (*on_drag_provide_finished)(P_INSTANCE WindowHandle window_handle, P_INSTANCE DragDropData data, bool success)
        public delegate void Delegate_on_drag_provide_finished(IntPtr window_handle, IntPtr data, bool success);

        // void (*on_clipboard_provide_chosen)(P_INSTANCE WindowHandle window_handle, P_INSTANCE DataInterchange data, utf8_string_struct format)
        public delegate void Delegate_on_clipboard_provide_chosen(IntPtr window_handle, IntPtr data, ref utf8_string_struct format);

        // void (*on_clipboard_receive_data)(P_INSTANCE WindowHandle window_handle, P_INSTANCE DataInterchange data)
        public delegate void Delegate_on_clipboard_receive_data(IntPtr window_handle, IntPtr data);

        // void (*on_idle)(P_INSTANCE WindowHandle window_handle)
        public delegate void Delegate_on_idle(IntPtr window_handle);

    }
    public IntPtr Handle;
    public CrystalWindow(IntPtr Handle)
    {
        this.Handle= Handle;
    }
    public DataInterchange ClipboardPaste()
    {
        return (DataInterchange) Imports.CrystalWindow_ClipboardPaste(Handle);
    }
    public void ClipboardCopy(DataInterchange  data)
    {
        Imports.CrystalWindow_ClipboardCopy(Handle, data.Handle);
    }
    public delegate void ClipboardCopyWithCallback_provide(DataInterchange data);

    protected static Imports.ClipboardCopyWithCallback_provide TranslateClipboardCopyWithCallback_provide(ClipboardCopyWithCallback_provide callback)
    {
        return (IntPtr data) =>
        {
            callback((DataInterchange)data);

        }
        ;
    }

    public static void ClipboardCopyWithCallback(ClipboardCopyWithCallback_provide  provide, DataInterchange  data)
    {
        Imports.CrystalWindow_ClipboardCopyWithCallback(TranslateClipboardCopyWithCallback_provide(provide), data.Handle);
    }
    public void ClipboardCopyPersist(DataInterchange  data)
    {
        Imports.CrystalWindow_ClipboardCopyPersist(Handle, data.Handle);
    }
    public static void ClipboardClear()
    {
        Imports.CrystalWindow_ClipboardClear();
    }
    public void RegisterDragTarget()
    {
        Imports.CrystalWindow_RegisterDragTarget(Handle);
    }
    public void DragStart(DragDropData  data, int  x, int  y)
    {
        Imports.CrystalWindow_DragStart(Handle, data.Handle, (int)x, (int)y);
    }
    public void DragChoose(DragDropData  data, string  fmt)
    {
        utf8_string_struct param_fmt = fmt;
        Imports.CrystalWindow_DragChoose(Handle, data.Handle,  ref param_fmt);
    }
    public static CrystalWindow Create(int  width, int  height, string  title)
    {
        utf8_string_struct param_title = title;
        return (CrystalWindow) Imports.CrystalWindow_Create((int)width, (int)height,  ref param_title);
    }
    public static CrystalWindow CreateSimple(int  width, int  height, string  title)
    {
        utf8_string_struct param_title = title;
        return (CrystalWindow) Imports.CrystalWindow_CreateSimple((int)width, (int)height,  ref param_title);
    }
    public void ApplicationRetain()
    {
        Imports.CrystalWindow_ApplicationRetain(Handle);
    }
    public void ApplicationRelease()
    {
        Imports.CrystalWindow_ApplicationRelease(Handle);
    }
    public void PresentImage(string  pixformat, IntPtr  pixdata, IntPtr  pixdata_length, int  width, int  height)
    {
        utf8_string_struct param_pixformat = pixformat;
        Imports.CrystalWindow_PresentImage(Handle,  ref param_pixformat, (IntPtr)pixdata, (IntPtr)pixdata_length, (int)width, (int)height);
    }
    public void QueueRedraw()
    {
        Imports.CrystalWindow_QueueRedraw(Handle);
    }
    public void MouseCapture()
    {
        Imports.CrystalWindow_MouseCapture(Handle);
    }
    public void MouseRelease()
    {
        Imports.CrystalWindow_MouseRelease(Handle);
    }
    public void GLInit()
    {
        Imports.CrystalWindow_GLInit(Handle);
    }
    public void Show(bool  restore)
    {
        Imports.CrystalWindow_Show(Handle, (bool)restore);
    }
    public bool hasClosed = false;
    public void Close()
    {
        if (hasClosed) return;
        hasClosed = true;
        Imports.CrystalWindow_Close(Handle);
    }
    public void PostClose()
    {
        Imports.CrystalWindow_PostClose(Handle);
    }
    public bool SetMessaqeHandler(string  handler_name, IntPtr  handler)
    {
        utf8_string_struct param_handler_name = handler_name;
        return (bool) Imports.CrystalWindow_SetMessaqeHandler(Handle,  ref param_handler_name, (IntPtr)handler);
    }
    public double uptimeSeconds()
    {
        return (double) Imports.CrystalWindow_uptimeSeconds(Handle);
    }
    public void uptimeReset()
    {
        Imports.CrystalWindow_uptimeReset(Handle);
    }
    public delegate void Delegate_on_draw(CrystalWindow window_handle);

    protected static Imports.Delegate_on_draw TranslateDelegate_on_draw(Delegate_on_draw callback)
    {
        return (IntPtr window_handle) =>
        {
            callback((CrystalWindow)window_handle);

        }
        ;
    }

    public delegate void Delegate_on_key_down(CrystalWindow window_handle, int keycode);

    protected static Imports.Delegate_on_key_down TranslateDelegate_on_key_down(Delegate_on_key_down callback)
    {
        return (IntPtr window_handle, int keycode) =>
        {
            callback((CrystalWindow)window_handle, (int)keycode);

        }
        ;
    }

    public delegate void Delegate_on_key_up(CrystalWindow window_handle, int keycode);

    protected static Imports.Delegate_on_key_up TranslateDelegate_on_key_up(Delegate_on_key_up callback)
    {
        return (IntPtr window_handle, int keycode) =>
        {
            callback((CrystalWindow)window_handle, (int)keycode);

        }
        ;
    }

    public delegate void Delegate_on_mouse_move(CrystalWindow window_handle, int x, int y);

    protected static Imports.Delegate_on_mouse_move TranslateDelegate_on_mouse_move(Delegate_on_mouse_move callback)
    {
        return (IntPtr window_handle, int x, int y) =>
        {
            callback((CrystalWindow)window_handle, (int)x, (int)y);

        }
        ;
    }

    public delegate void Delegate_on_mouse_down(CrystalWindow window_handle, int button, int x, int y);

    protected static Imports.Delegate_on_mouse_down TranslateDelegate_on_mouse_down(Delegate_on_mouse_down callback)
    {
        return (IntPtr window_handle, int button, int x, int y) =>
        {
            callback((CrystalWindow)window_handle, (int)button, (int)x, (int)y);

        }
        ;
    }

    public delegate void Delegate_on_mouse_up(CrystalWindow window_handle, int button, int x, int y);

    protected static Imports.Delegate_on_mouse_up TranslateDelegate_on_mouse_up(Delegate_on_mouse_up callback)
    {
        return (IntPtr window_handle, int button, int x, int y) =>
        {
            callback((CrystalWindow)window_handle, (int)button, (int)x, (int)y);

        }
        ;
    }

    public delegate void Delegate_on_resize(CrystalWindow window_handle, int width, int height);

    protected static Imports.Delegate_on_resize TranslateDelegate_on_resize(Delegate_on_resize callback)
    {
        return (IntPtr window_handle, int width, int height) =>
        {
            callback((CrystalWindow)window_handle, (int)width, (int)height);

        }
        ;
    }

    public delegate void Delegate_on_close(CrystalWindow window_handle);

    protected static Imports.Delegate_on_close TranslateDelegate_on_close(Delegate_on_close callback)
    {
        return (IntPtr window_handle) =>
        {
            callback((CrystalWindow)window_handle);

        }
        ;
    }

    public delegate void Delegate_on_focus_in(CrystalWindow window_handle);

    protected static Imports.Delegate_on_focus_in TranslateDelegate_on_focus_in(Delegate_on_focus_in callback)
    {
        return (IntPtr window_handle) =>
        {
            callback((CrystalWindow)window_handle);

        }
        ;
    }

    public delegate void Delegate_on_focus_out(CrystalWindow window_handle);

    protected static Imports.Delegate_on_focus_out TranslateDelegate_on_focus_out(Delegate_on_focus_out callback)
    {
        return (IntPtr window_handle) =>
        {
            callback((CrystalWindow)window_handle);

        }
        ;
    }

    public delegate void Delegate_on_drag_receive_start(CrystalWindow window_handle, DragDropData data);

    protected static Imports.Delegate_on_drag_receive_start TranslateDelegate_on_drag_receive_start(Delegate_on_drag_receive_start callback)
    {
        return (IntPtr window_handle, IntPtr data) =>
        {
            callback((CrystalWindow)window_handle, (DragDropData)data);

        }
        ;
    }

    public delegate void Delegate_on_drag_receive_enter(CrystalWindow window_handle, DragDropData data);

    protected static Imports.Delegate_on_drag_receive_enter TranslateDelegate_on_drag_receive_enter(Delegate_on_drag_receive_enter callback)
    {
        return (IntPtr window_handle, IntPtr data) =>
        {
            callback((CrystalWindow)window_handle, (DragDropData)data);

        }
        ;
    }

    public delegate void Delegate_on_drag_receive_motion(CrystalWindow window_handle, DragDropData data, int x, int y, uint key_state);

    protected static Imports.Delegate_on_drag_receive_motion TranslateDelegate_on_drag_receive_motion(Delegate_on_drag_receive_motion callback)
    {
        return (IntPtr window_handle, IntPtr data, int x, int y, uint key_state) =>
        {
            callback((CrystalWindow)window_handle, (DragDropData)data, (int)x, (int)y, (uint)key_state);

        }
        ;
    }

    public delegate void Delegate_on_drag_receive_leave(CrystalWindow window_handle, DragDropData data);

    protected static Imports.Delegate_on_drag_receive_leave TranslateDelegate_on_drag_receive_leave(Delegate_on_drag_receive_leave callback)
    {
        return (IntPtr window_handle, IntPtr data) =>
        {
            callback((CrystalWindow)window_handle, (DragDropData)data);

        }
        ;
    }

    public delegate void Delegate_on_drag_receive_drop(CrystalWindow window_handle, DragDropData data);

    protected static Imports.Delegate_on_drag_receive_drop TranslateDelegate_on_drag_receive_drop(Delegate_on_drag_receive_drop callback)
    {
        return (IntPtr window_handle, IntPtr data) =>
        {
            callback((CrystalWindow)window_handle, (DragDropData)data);

        }
        ;
    }

    public delegate string Delegate_on_drag_receive_select(CrystalWindow window_handle, DragDropData data);

    protected static Imports.Delegate_on_drag_receive_select TranslateDelegate_on_drag_receive_select(Delegate_on_drag_receive_select callback)
    {
        return (IntPtr window_handle, IntPtr data) =>
        {
            return callback((CrystalWindow)window_handle, (DragDropData)data);

        }
        ;
    }

    public delegate void Delegate_on_drag_provide_status(CrystalWindow window_handle, DragDropData data);

    protected static Imports.Delegate_on_drag_provide_status TranslateDelegate_on_drag_provide_status(Delegate_on_drag_provide_status callback)
    {
        return (IntPtr window_handle, IntPtr data) =>
        {
            callback((CrystalWindow)window_handle, (DragDropData)data);

        }
        ;
    }

    public delegate void Delegate_on_drag_provide_chosen(CrystalWindow window_handle, DragDropData data, string format);

    protected static Imports.Delegate_on_drag_provide_chosen TranslateDelegate_on_drag_provide_chosen(Delegate_on_drag_provide_chosen callback)
    {
        return (IntPtr window_handle, IntPtr data, ref utf8_string_struct format) =>
        {
            callback((CrystalWindow)window_handle, (DragDropData)data, (string)format);

        }
        ;
    }

    public delegate void Delegate_on_drag_provide_finished(CrystalWindow window_handle, DragDropData data, bool success);

    protected static Imports.Delegate_on_drag_provide_finished TranslateDelegate_on_drag_provide_finished(Delegate_on_drag_provide_finished callback)
    {
        return (IntPtr window_handle, IntPtr data, bool success) =>
        {
            callback((CrystalWindow)window_handle, (DragDropData)data, (bool)success);

        }
        ;
    }

    public delegate void Delegate_on_clipboard_provide_chosen(CrystalWindow window_handle, DataInterchange data, string format);

    protected static Imports.Delegate_on_clipboard_provide_chosen TranslateDelegate_on_clipboard_provide_chosen(Delegate_on_clipboard_provide_chosen callback)
    {
        return (IntPtr window_handle, IntPtr data, ref utf8_string_struct format) =>
        {
            callback((CrystalWindow)window_handle, (DataInterchange)data, (string)format);

        }
        ;
    }

    public delegate void Delegate_on_clipboard_receive_data(CrystalWindow window_handle, DataInterchange data);

    protected static Imports.Delegate_on_clipboard_receive_data TranslateDelegate_on_clipboard_receive_data(Delegate_on_clipboard_receive_data callback)
    {
        return (IntPtr window_handle, IntPtr data) =>
        {
            callback((CrystalWindow)window_handle, (DataInterchange)data);

        }
        ;
    }

    public delegate void Delegate_on_idle(CrystalWindow window_handle);

    protected static Imports.Delegate_on_idle TranslateDelegate_on_idle(Delegate_on_idle callback)
    {
        return (IntPtr window_handle) =>
        {
            callback((CrystalWindow)window_handle);

        }
        ;
    }

}
