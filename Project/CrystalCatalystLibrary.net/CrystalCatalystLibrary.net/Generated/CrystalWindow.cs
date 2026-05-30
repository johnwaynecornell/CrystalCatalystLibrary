using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using JWCEssentials.net;

namespace CrystalCatalystLibrary.net;

public class CrystalWindow : IDisposable
{
    public delegate void ClipboardCopyWithCallback_provide(DataInterchange data);

    public delegate void Delegate_on_clipboard_provide_chosen(CrystalWindow window_handle, DataInterchange data,
        string format);

    public delegate void Delegate_on_clipboard_receive_data(CrystalWindow window_handle, DataInterchange data);

    public delegate void Delegate_on_close(CrystalWindow window_handle);

    public delegate void Delegate_on_drag_provide_chosen(CrystalWindow window_handle, DragDropData data, string format);

    public delegate void
        Delegate_on_drag_provide_finished(CrystalWindow window_handle, DragDropData data, bool success);

    public delegate void Delegate_on_drag_provide_status(CrystalWindow window_handle, DragDropData data);

    public delegate void Delegate_on_drag_receive_drop(CrystalWindow window_handle, DragDropData data);

    public delegate void Delegate_on_drag_receive_enter(CrystalWindow window_handle, DragDropData data);

    public delegate void Delegate_on_drag_receive_leave(CrystalWindow window_handle, DragDropData data);

    public delegate void Delegate_on_drag_receive_motion(CrystalWindow window_handle, DragDropData data, int x, int y,
        uint key_state);

    public delegate string Delegate_on_drag_receive_select(CrystalWindow window_handle, DragDropData data);

    public delegate void Delegate_on_drag_receive_start(CrystalWindow window_handle, DragDropData data);

    public delegate void Delegate_on_draw(CrystalWindow window_handle);

    public delegate void Delegate_on_focus_in(CrystalWindow window_handle);

    public delegate void Delegate_on_focus_out(CrystalWindow window_handle);

    public delegate void Delegate_on_idle(CrystalWindow window_handle);

    public delegate void Delegate_on_key_down(CrystalWindow window_handle, int keycode);

    public delegate void Delegate_on_key_up(CrystalWindow window_handle, int keycode);

    public delegate void Delegate_on_mouse_down(CrystalWindow window_handle, int button, int x, int y);

    public delegate void Delegate_on_mouse_move(CrystalWindow window_handle, int x, int y);

    public delegate void Delegate_on_mouse_up(CrystalWindow window_handle, int button, int x, int y);

    public delegate void Delegate_on_resize(CrystalWindow window_handle, int width, int height);

    // Thread-safe tracker mapping the native IntPtr to our managed wrapper.
    // We use WeakReference so we don't cause memory leaks if the user drops the reference.
    private static readonly ConcurrentDictionary<IntPtr, WeakReference<CrystalWindow>> InstanceCache = new();

    // Tracks whether this specific instance has been disposed
    private bool _disposed;
    private Delegate_on_clipboard_provide_chosen? delegate_on_clipboard_provide_chosen;
    private Delegate_on_clipboard_receive_data? delegate_on_clipboard_receive_data;
    private Delegate_on_close? delegate_on_close;
    private Delegate_on_drag_provide_chosen? delegate_on_drag_provide_chosen;
    private Delegate_on_drag_provide_finished? delegate_on_drag_provide_finished;
    private Delegate_on_drag_provide_status? delegate_on_drag_provide_status;
    private Delegate_on_drag_receive_drop? delegate_on_drag_receive_drop;
    private Delegate_on_drag_receive_enter? delegate_on_drag_receive_enter;
    private Delegate_on_drag_receive_leave? delegate_on_drag_receive_leave;
    private Delegate_on_drag_receive_motion? delegate_on_drag_receive_motion;
    private Delegate_on_drag_receive_select? delegate_on_drag_receive_select;
    private Delegate_on_drag_receive_start? delegate_on_drag_receive_start;
    private Delegate_on_draw? delegate_on_draw;
    private Delegate_on_focus_in? delegate_on_focus_in;
    private Delegate_on_focus_out? delegate_on_focus_out;
    private Delegate_on_idle? delegate_on_idle;
    private Delegate_on_key_down? delegate_on_key_down;
    private Delegate_on_key_up? delegate_on_key_up;
    private Delegate_on_mouse_down? delegate_on_mouse_down;
    private Delegate_on_mouse_move? delegate_on_mouse_move;
    private Delegate_on_mouse_up? delegate_on_mouse_up;
    private Delegate_on_resize? delegate_on_resize;
    public IntPtr Handle;
    public bool hasClosed;

    private Imports.Delegate_on_clipboard_provide_chosen? native_on_clipboard_provide_chosen;

    private Imports.Delegate_on_clipboard_receive_data? native_on_clipboard_receive_data;

    private Imports.Delegate_on_close? native_on_close;

    private Imports.Delegate_on_drag_provide_chosen? native_on_drag_provide_chosen;

    private Imports.Delegate_on_drag_provide_finished? native_on_drag_provide_finished;

    private Imports.Delegate_on_drag_provide_status? native_on_drag_provide_status;

    private Imports.Delegate_on_drag_receive_drop? native_on_drag_receive_drop;

    private Imports.Delegate_on_drag_receive_enter? native_on_drag_receive_enter;

    private Imports.Delegate_on_drag_receive_leave? native_on_drag_receive_leave;

    private Imports.Delegate_on_drag_receive_motion? native_on_drag_receive_motion;

    private Imports.Delegate_on_drag_receive_select? native_on_drag_receive_select;

    private Imports.Delegate_on_drag_receive_start? native_on_drag_receive_start;

    private Imports.Delegate_on_draw? native_on_draw;

    private Imports.Delegate_on_focus_in? native_on_focus_in;

    private Imports.Delegate_on_focus_out? native_on_focus_out;

    private Imports.Delegate_on_idle? native_on_idle;

    private Imports.Delegate_on_key_down? native_on_key_down;

    private Imports.Delegate_on_key_up? native_on_key_up;

    private Imports.Delegate_on_mouse_down? native_on_mouse_down;

    private Imports.Delegate_on_mouse_move? native_on_mouse_move;

    private Imports.Delegate_on_mouse_up? native_on_mouse_up;

    private Imports.Delegate_on_resize? native_on_resize;

    public CrystalWindow(IntPtr Handle)
    {
        this.Handle = Handle;
    }

    public Delegate_on_draw? OnDraw
    {
        get => delegate_on_draw;

        set
        {
            if (value == null)
            {
                native_on_draw = null;
                delegate_on_draw = null;
                SetMessageHandler("on_draw", IntPtr.Zero);
                return;
            }

            native_on_draw = TranslateDelegate_on_draw(value);
            delegate_on_draw = value;
            var pointerToNative = Marshal.GetFunctionPointerForDelegate(native_on_draw);
            SetMessageHandler("on_draw", pointerToNative);
        }
    }

    public Delegate_on_key_down? OnKeyDown
    {
        get => delegate_on_key_down;

        set
        {
            if (value == null)
            {
                native_on_key_down = null;
                delegate_on_key_down = null;
                SetMessageHandler("on_key_down", IntPtr.Zero);
                return;
            }

            native_on_key_down = TranslateDelegate_on_key_down(value);
            delegate_on_key_down = value;
            var pointerToNative = Marshal.GetFunctionPointerForDelegate(native_on_key_down);
            SetMessageHandler("on_key_down", pointerToNative);
        }
    }

    public Delegate_on_key_up? OnKeyUp
    {
        get => delegate_on_key_up;

        set
        {
            if (value == null)
            {
                native_on_key_up = null;
                delegate_on_key_up = null;
                SetMessageHandler("on_key_up", IntPtr.Zero);
                return;
            }

            native_on_key_up = TranslateDelegate_on_key_up(value);
            delegate_on_key_up = value;
            var pointerToNative = Marshal.GetFunctionPointerForDelegate(native_on_key_up);
            SetMessageHandler("on_key_up", pointerToNative);
        }
    }

    public Delegate_on_mouse_move? OnMouseMove
    {
        get => delegate_on_mouse_move;

        set
        {
            if (value == null)
            {
                native_on_mouse_move = null;
                delegate_on_mouse_move = null;
                SetMessageHandler("on_mouse_move", IntPtr.Zero);
                return;
            }

            native_on_mouse_move = TranslateDelegate_on_mouse_move(value);
            delegate_on_mouse_move = value;
            var pointerToNative = Marshal.GetFunctionPointerForDelegate(native_on_mouse_move);
            SetMessageHandler("on_mouse_move", pointerToNative);
        }
    }

    public Delegate_on_mouse_down? OnMouseDown
    {
        get => delegate_on_mouse_down;

        set
        {
            if (value == null)
            {
                native_on_mouse_down = null;
                delegate_on_mouse_down = null;
                SetMessageHandler("on_mouse_down", IntPtr.Zero);
                return;
            }

            native_on_mouse_down = TranslateDelegate_on_mouse_down(value);
            delegate_on_mouse_down = value;
            var pointerToNative = Marshal.GetFunctionPointerForDelegate(native_on_mouse_down);
            SetMessageHandler("on_mouse_down", pointerToNative);
        }
    }

    public Delegate_on_mouse_up? OnMouseUp
    {
        get => delegate_on_mouse_up;

        set
        {
            if (value == null)
            {
                native_on_mouse_up = null;
                delegate_on_mouse_up = null;
                SetMessageHandler("on_mouse_up", IntPtr.Zero);
                return;
            }

            native_on_mouse_up = TranslateDelegate_on_mouse_up(value);
            delegate_on_mouse_up = value;
            var pointerToNative = Marshal.GetFunctionPointerForDelegate(native_on_mouse_up);
            SetMessageHandler("on_mouse_up", pointerToNative);
        }
    }

    public Delegate_on_resize? OnResize
    {
        get => delegate_on_resize;

        set
        {
            if (value == null)
            {
                native_on_resize = null;
                delegate_on_resize = null;
                SetMessageHandler("on_resize", IntPtr.Zero);
                return;
            }

            native_on_resize = TranslateDelegate_on_resize(value);
            delegate_on_resize = value;
            var pointerToNative = Marshal.GetFunctionPointerForDelegate(native_on_resize);
            SetMessageHandler("on_resize", pointerToNative);
        }
    }

    public Delegate_on_close? OnClose
    {
        get => delegate_on_close;

        set
        {
            if (value == null)
            {
                native_on_close = null;
                delegate_on_close = null;
                SetMessageHandler("on_close", IntPtr.Zero);
                return;
            }

            native_on_close = TranslateDelegate_on_close(value);
            delegate_on_close = value;
            var pointerToNative = Marshal.GetFunctionPointerForDelegate(native_on_close);
            SetMessageHandler("on_close", pointerToNative);
        }
    }

    public Delegate_on_focus_in? OnFocusIn
    {
        get => delegate_on_focus_in;

        set
        {
            if (value == null)
            {
                native_on_focus_in = null;
                delegate_on_focus_in = null;
                SetMessageHandler("on_focus_in", IntPtr.Zero);
                return;
            }

            native_on_focus_in = TranslateDelegate_on_focus_in(value);
            delegate_on_focus_in = value;
            var pointerToNative = Marshal.GetFunctionPointerForDelegate(native_on_focus_in);
            SetMessageHandler("on_focus_in", pointerToNative);
        }
    }

    public Delegate_on_focus_out? OnFocusOut
    {
        get => delegate_on_focus_out;

        set
        {
            if (value == null)
            {
                native_on_focus_out = null;
                delegate_on_focus_out = null;
                SetMessageHandler("on_focus_out", IntPtr.Zero);
                return;
            }

            native_on_focus_out = TranslateDelegate_on_focus_out(value);
            delegate_on_focus_out = value;
            var pointerToNative = Marshal.GetFunctionPointerForDelegate(native_on_focus_out);
            SetMessageHandler("on_focus_out", pointerToNative);
        }
    }

    public Delegate_on_drag_receive_start? OnDragReceiveStart
    {
        get => delegate_on_drag_receive_start;

        set
        {
            if (value == null)
            {
                native_on_drag_receive_start = null;
                delegate_on_drag_receive_start = null;
                SetMessageHandler("on_drag_receive_start", IntPtr.Zero);
                return;
            }

            native_on_drag_receive_start = TranslateDelegate_on_drag_receive_start(value);
            delegate_on_drag_receive_start = value;
            var pointerToNative = Marshal.GetFunctionPointerForDelegate(native_on_drag_receive_start);
            SetMessageHandler("on_drag_receive_start", pointerToNative);
        }
    }

    public Delegate_on_drag_receive_enter? OnDragReceiveEnter
    {
        get => delegate_on_drag_receive_enter;

        set
        {
            if (value == null)
            {
                native_on_drag_receive_enter = null;
                delegate_on_drag_receive_enter = null;
                SetMessageHandler("on_drag_receive_enter", IntPtr.Zero);
                return;
            }

            native_on_drag_receive_enter = TranslateDelegate_on_drag_receive_enter(value);
            delegate_on_drag_receive_enter = value;
            var pointerToNative = Marshal.GetFunctionPointerForDelegate(native_on_drag_receive_enter);
            SetMessageHandler("on_drag_receive_enter", pointerToNative);
        }
    }

    public Delegate_on_drag_receive_motion? OnDragReceiveMotion
    {
        get => delegate_on_drag_receive_motion;

        set
        {
            if (value == null)
            {
                native_on_drag_receive_motion = null;
                delegate_on_drag_receive_motion = null;
                SetMessageHandler("on_drag_receive_motion", IntPtr.Zero);
                return;
            }

            native_on_drag_receive_motion = TranslateDelegate_on_drag_receive_motion(value);
            delegate_on_drag_receive_motion = value;
            var pointerToNative = Marshal.GetFunctionPointerForDelegate(native_on_drag_receive_motion);
            SetMessageHandler("on_drag_receive_motion", pointerToNative);
        }
    }

    public Delegate_on_drag_receive_leave? OnDragReceiveLeave
    {
        get => delegate_on_drag_receive_leave;

        set
        {
            if (value == null)
            {
                native_on_drag_receive_leave = null;
                delegate_on_drag_receive_leave = null;
                SetMessageHandler("on_drag_receive_leave", IntPtr.Zero);
                return;
            }

            native_on_drag_receive_leave = TranslateDelegate_on_drag_receive_leave(value);
            delegate_on_drag_receive_leave = value;
            var pointerToNative = Marshal.GetFunctionPointerForDelegate(native_on_drag_receive_leave);
            SetMessageHandler("on_drag_receive_leave", pointerToNative);
        }
    }

    public Delegate_on_drag_receive_drop? OnDragReceiveDrop
    {
        get => delegate_on_drag_receive_drop;

        set
        {
            if (value == null)
            {
                native_on_drag_receive_drop = null;
                delegate_on_drag_receive_drop = null;
                SetMessageHandler("on_drag_receive_drop", IntPtr.Zero);
                return;
            }

            native_on_drag_receive_drop = TranslateDelegate_on_drag_receive_drop(value);
            delegate_on_drag_receive_drop = value;
            var pointerToNative = Marshal.GetFunctionPointerForDelegate(native_on_drag_receive_drop);
            SetMessageHandler("on_drag_receive_drop", pointerToNative);
        }
    }

    public Delegate_on_drag_receive_select? OnDragReceiveSelect
    {
        get => delegate_on_drag_receive_select;

        set
        {
            if (value == null)
            {
                native_on_drag_receive_select = null;
                delegate_on_drag_receive_select = null;
                SetMessageHandler("on_drag_receive_select", IntPtr.Zero);
                return;
            }

            native_on_drag_receive_select = TranslateDelegate_on_drag_receive_select(value);
            delegate_on_drag_receive_select = value;
            var pointerToNative = Marshal.GetFunctionPointerForDelegate(native_on_drag_receive_select);
            SetMessageHandler("on_drag_receive_select", pointerToNative);
        }
    }

    public Delegate_on_drag_provide_status? OnDragProvideStatus
    {
        get => delegate_on_drag_provide_status;

        set
        {
            if (value == null)
            {
                native_on_drag_provide_status = null;
                delegate_on_drag_provide_status = null;
                SetMessageHandler("on_drag_provide_status", IntPtr.Zero);
                return;
            }

            native_on_drag_provide_status = TranslateDelegate_on_drag_provide_status(value);
            delegate_on_drag_provide_status = value;
            var pointerToNative = Marshal.GetFunctionPointerForDelegate(native_on_drag_provide_status);
            SetMessageHandler("on_drag_provide_status", pointerToNative);
        }
    }

    public Delegate_on_drag_provide_chosen? OnDragProvideChosen
    {
        get => delegate_on_drag_provide_chosen;

        set
        {
            if (value == null)
            {
                native_on_drag_provide_chosen = null;
                delegate_on_drag_provide_chosen = null;
                SetMessageHandler("on_drag_provide_chosen", IntPtr.Zero);
                return;
            }

            native_on_drag_provide_chosen = TranslateDelegate_on_drag_provide_chosen(value);
            delegate_on_drag_provide_chosen = value;
            var pointerToNative = Marshal.GetFunctionPointerForDelegate(native_on_drag_provide_chosen);
            SetMessageHandler("on_drag_provide_chosen", pointerToNative);
        }
    }

    public Delegate_on_drag_provide_finished? OnDragProvideFinished
    {
        get => delegate_on_drag_provide_finished;

        set
        {
            if (value == null)
            {
                native_on_drag_provide_finished = null;
                delegate_on_drag_provide_finished = null;
                SetMessageHandler("on_drag_provide_finished", IntPtr.Zero);
                return;
            }

            native_on_drag_provide_finished = TranslateDelegate_on_drag_provide_finished(value);
            delegate_on_drag_provide_finished = value;
            var pointerToNative = Marshal.GetFunctionPointerForDelegate(native_on_drag_provide_finished);
            SetMessageHandler("on_drag_provide_finished", pointerToNative);
        }
    }

    public Delegate_on_clipboard_provide_chosen? OnClipboardProvideChosen
    {
        get => delegate_on_clipboard_provide_chosen;

        set
        {
            if (value == null)
            {
                native_on_clipboard_provide_chosen = null;
                delegate_on_clipboard_provide_chosen = null;
                SetMessageHandler("on_clipboard_provide_chosen", IntPtr.Zero);
                return;
            }

            native_on_clipboard_provide_chosen = TranslateDelegate_on_clipboard_provide_chosen(value);
            delegate_on_clipboard_provide_chosen = value;
            var pointerToNative = Marshal.GetFunctionPointerForDelegate(native_on_clipboard_provide_chosen);
            SetMessageHandler("on_clipboard_provide_chosen", pointerToNative);
        }
    }

    public Delegate_on_clipboard_receive_data? OnClipboardReceiveData
    {
        get => delegate_on_clipboard_receive_data;

        set
        {
            if (value == null)
            {
                native_on_clipboard_receive_data = null;
                delegate_on_clipboard_receive_data = null;
                SetMessageHandler("on_clipboard_receive_data", IntPtr.Zero);
                return;
            }

            native_on_clipboard_receive_data = TranslateDelegate_on_clipboard_receive_data(value);
            delegate_on_clipboard_receive_data = value;
            var pointerToNative = Marshal.GetFunctionPointerForDelegate(native_on_clipboard_receive_data);
            SetMessageHandler("on_clipboard_receive_data", pointerToNative);
        }
    }

    public Delegate_on_idle? OnIdle
    {
        get => delegate_on_idle;

        set
        {
            if (value == null)
            {
                native_on_idle = null;
                delegate_on_idle = null;
                SetMessageHandler("on_idle", IntPtr.Zero);
                return;
            }

            native_on_idle = TranslateDelegate_on_idle(value);
            delegate_on_idle = value;
            var pointerToNative = Marshal.GetFunctionPointerForDelegate(native_on_idle);
            SetMessageHandler("on_idle", pointerToNative);
        }
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
                Close();
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
    public static explicit operator CrystalWindow(IntPtr handle)
    {
        if (handle == IntPtr.Zero) throw new NullReferenceException("IntPtr cannot be null");

        // Try to find an existing alive wrapper
        if (InstanceCache.TryGetValue(handle, out var weakRef) && weakRef.TryGetTarget(out var existingContext))
            return existingContext;

        // If we didn't find one, or it was garbage collected, create a new one
        var newContext = new CrystalWindow(handle);
        InstanceCache[handle] = new WeakReference<CrystalWindow>(newContext);

        return newContext;
    }

    public DataInterchange ClipboardPaste()
    {
        var Ret = (DataInterchange)Imports.CrystalWindow_ClipboardPaste(Handle);
        return Ret;
    }

    public void ClipboardCopy(DataInterchange data)
    {
        Imports.CrystalWindow_ClipboardCopy(Handle, data.Handle);
    }

    protected static Imports.ClipboardCopyWithCallback_provide TranslateClipboardCopyWithCallback_provide(
        ClipboardCopyWithCallback_provide callback)
    {
        return data => { callback((DataInterchange)data); }
            ;
    }

    public static void ClipboardCopyWithCallback(ClipboardCopyWithCallback_provide provide, DataInterchange data)
    {
        Imports.CrystalWindow_ClipboardCopyWithCallback(TranslateClipboardCopyWithCallback_provide(provide),
            data.Handle);
    }

    public void ClipboardCopyPersist(DataInterchange data)
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

    public void DragStart(DragDropData data, int x, int y)
    {
        Imports.CrystalWindow_DragStart(Handle, data.Handle, x, y);
    }

    public void DragChoose(DragDropData data, string fmt)
    {
        utf8_string_struct param_fmt = fmt;
        Imports.CrystalWindow_DragChoose(Handle, data.Handle, ref param_fmt);
    }

    public static CrystalWindow Create(int width, int height, string title)
    {
        utf8_string_struct param_title = title;
        var Ret = (CrystalWindow)Imports.CrystalWindow_Create(width, height, ref param_title);
        return Ret;
    }

    public static CrystalWindow CreateSimple(int width, int height, string title)
    {
        utf8_string_struct param_title = title;
        var Ret = (CrystalWindow)Imports.CrystalWindow_CreateSimple(width, height, ref param_title);
        return Ret;
    }

    public void ApplicationRetain()
    {
        Imports.CrystalWindow_ApplicationRetain(Handle);
    }

    public void ApplicationRelease()
    {
        Imports.CrystalWindow_ApplicationRelease(Handle);
    }

    public void PresentPix(ref PixData pix)
    {
        Imports.CrystalWindow_PresentPix(Handle, ref pix);
    }

    public void PresentImage(string pixformat, IntPtr pixdata, IntPtr pixdata_length, int width, int height)
    {
        utf8_string_struct param_pixformat = pixformat;
        Imports.CrystalWindow_PresentImage(Handle, ref param_pixformat, pixdata, pixdata_length, width, height);
    }

    public void CursorPix(ref PixData pix, int hot_x, int hot_y)
    {
        Imports.CrystalWindow_CursorPix(Handle, ref pix, hot_x, hot_y);
    }

    public void SetCursor(string pixformat, IntPtr pixdata, IntPtr pixdata_length, int width, int height, int hot_x,
        int hot_y)
    {
        utf8_string_struct param_pixformat = pixformat;
        Imports.CrystalWindow_SetCursor(Handle, ref param_pixformat, pixdata, pixdata_length, width, height, hot_x,
            hot_y);
    }

    public void SetStandardCursor(CrystalCursor cursor_enum)
    {
        Imports.CrystalWindow_SetStandardCursor(Handle, cursor_enum);
    }

    public void IconPix(ref PixData pix)
    {
        Imports.CrystalWindow_IconPix(Handle, ref pix);
    }

    public void SetIcon(string pixformat, IntPtr pixdata, IntPtr pixdata_length, int width, int height)
    {
        utf8_string_struct param_pixformat = pixformat;
        Imports.CrystalWindow_SetIcon(Handle, ref param_pixformat, pixdata, pixdata_length, width, height);
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

    public void GLMakeCurrent()
    {
        Imports.CrystalWindow_GLMakeCurrent(Handle);
    }

    public void GLPresent()
    {
        Imports.CrystalWindow_GLPresent(Handle);
    }

    public IntPtr GLGetProcAddress(string name)
    {
        return Imports.CrystalWindow_GLGetProcAddress(Handle, name);
    }

    public void Show(bool restore)
    {
        Imports.CrystalWindow_Show(Handle, restore);
    }

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

    public void SetSize(int width, int height)
    {
        Imports.CrystalWindow_SetSize(Handle, width, height);
    }

    public void GetSize(out int width, out int height)
    {
        Imports.CrystalWindow_GetSize(Handle, out width, out height);
    }

    public void SetLocation(int x, int y)
    {
        Imports.CrystalWindow_SetLocation(Handle, x, y);
    }

    public void GetLocation(out int x, out int y)
    {
        Imports.CrystalWindow_GetLocation(Handle, out x, out y);
    }

    public void SetTitle(string title)
    {
        utf8_string_struct param_title = title;
        Imports.CrystalWindow_SetTitle(Handle, ref param_title);
    }

    public void GetTitle(out string title)
    {
        utf8_string_struct param_title;
        Imports.CrystalWindow_GetTitle(Handle, out param_title);
        title = param_title;
    }

    public static void GetDefaultStockIcon(out string pixformat, out IntPtr pixdata, out IntPtr pixdata_length,
        out int width, out int height)
    {
        utf8_string_struct param_pixformat;
        Imports.CrystalWindow_GetDefaultStockIcon(out param_pixformat, out pixdata, out pixdata_length, out width,
            out height);
        pixformat = param_pixformat;
    }

    public bool SetMessageHandler(string handler_name, IntPtr handler)
    {
        utf8_string_struct param_handler_name = handler_name;
        var Ret = Imports.CrystalWindow_SetMessageHandler(Handle, ref param_handler_name, handler);
        return Ret;
    }

    public double uptimeSeconds()
    {
        var Ret = Imports.CrystalWindow_uptimeSeconds(Handle);
        return Ret;
    }

    public void uptimeReset()
    {
        Imports.CrystalWindow_uptimeReset(Handle);
    }

    protected static Imports.Delegate_on_draw TranslateDelegate_on_draw(Delegate_on_draw callback)
    {
        return window_handle => { callback((CrystalWindow)window_handle); }
            ;
    }

    protected static Imports.Delegate_on_key_down TranslateDelegate_on_key_down(Delegate_on_key_down callback)
    {
        return (window_handle, keycode) => { callback((CrystalWindow)window_handle, keycode); }
            ;
    }

    protected static Imports.Delegate_on_key_up TranslateDelegate_on_key_up(Delegate_on_key_up callback)
    {
        return (window_handle, keycode) => { callback((CrystalWindow)window_handle, keycode); }
            ;
    }

    protected static Imports.Delegate_on_mouse_move TranslateDelegate_on_mouse_move(Delegate_on_mouse_move callback)
    {
        return (window_handle, x, y) => { callback((CrystalWindow)window_handle, x, y); }
            ;
    }

    protected static Imports.Delegate_on_mouse_down TranslateDelegate_on_mouse_down(Delegate_on_mouse_down callback)
    {
        return (window_handle, button, x, y) => { callback((CrystalWindow)window_handle, button, x, y); }
            ;
    }

    protected static Imports.Delegate_on_mouse_up TranslateDelegate_on_mouse_up(Delegate_on_mouse_up callback)
    {
        return (window_handle, button, x, y) => { callback((CrystalWindow)window_handle, button, x, y); }
            ;
    }

    protected static Imports.Delegate_on_resize TranslateDelegate_on_resize(Delegate_on_resize callback)
    {
        return (window_handle, width, height) => { callback((CrystalWindow)window_handle, width, height); }
            ;
    }

    protected static Imports.Delegate_on_close TranslateDelegate_on_close(Delegate_on_close callback)
    {
        return window_handle => { callback((CrystalWindow)window_handle); }
            ;
    }

    protected static Imports.Delegate_on_focus_in TranslateDelegate_on_focus_in(Delegate_on_focus_in callback)
    {
        return window_handle => { callback((CrystalWindow)window_handle); }
            ;
    }

    protected static Imports.Delegate_on_focus_out TranslateDelegate_on_focus_out(Delegate_on_focus_out callback)
    {
        return window_handle => { callback((CrystalWindow)window_handle); }
            ;
    }

    protected static Imports.Delegate_on_drag_receive_start TranslateDelegate_on_drag_receive_start(
        Delegate_on_drag_receive_start callback)
    {
        return (window_handle, data) => { callback((CrystalWindow)window_handle, (DragDropData)data); }
            ;
    }

    protected static Imports.Delegate_on_drag_receive_enter TranslateDelegate_on_drag_receive_enter(
        Delegate_on_drag_receive_enter callback)
    {
        return (window_handle, data) => { callback((CrystalWindow)window_handle, (DragDropData)data); }
            ;
    }

    protected static Imports.Delegate_on_drag_receive_motion TranslateDelegate_on_drag_receive_motion(
        Delegate_on_drag_receive_motion callback)
    {
        return (window_handle, data, x, y, key_state) =>
            {
                callback((CrystalWindow)window_handle, (DragDropData)data, x, y, key_state);
            }
            ;
    }

    protected static Imports.Delegate_on_drag_receive_leave TranslateDelegate_on_drag_receive_leave(
        Delegate_on_drag_receive_leave callback)
    {
        return (window_handle, data) => { callback((CrystalWindow)window_handle, (DragDropData)data); }
            ;
    }

    protected static Imports.Delegate_on_drag_receive_drop TranslateDelegate_on_drag_receive_drop(
        Delegate_on_drag_receive_drop callback)
    {
        return (window_handle, data) => { callback((CrystalWindow)window_handle, (DragDropData)data); }
            ;
    }

    protected static Imports.Delegate_on_drag_receive_select TranslateDelegate_on_drag_receive_select(
        Delegate_on_drag_receive_select callback)
    {
        return (window_handle, data) => { return callback((CrystalWindow)window_handle, (DragDropData)data); }
            ;
    }

    protected static Imports.Delegate_on_drag_provide_status TranslateDelegate_on_drag_provide_status(
        Delegate_on_drag_provide_status callback)
    {
        return (window_handle, data) => { callback((CrystalWindow)window_handle, (DragDropData)data); }
            ;
    }

    protected static Imports.Delegate_on_drag_provide_chosen TranslateDelegate_on_drag_provide_chosen(
        Delegate_on_drag_provide_chosen callback)
    {
        return (window_handle, data, ref format) =>
            {
                callback((CrystalWindow)window_handle, (DragDropData)data, (string)format);
            }
            ;
    }

    protected static Imports.Delegate_on_drag_provide_finished TranslateDelegate_on_drag_provide_finished(
        Delegate_on_drag_provide_finished callback)
    {
        return (window_handle, data, success) =>
            {
                callback((CrystalWindow)window_handle, (DragDropData)data, success);
            }
            ;
    }

    protected static Imports.Delegate_on_clipboard_provide_chosen TranslateDelegate_on_clipboard_provide_chosen(
        Delegate_on_clipboard_provide_chosen callback)
    {
        return (window_handle, data, ref format) =>
            {
                callback((CrystalWindow)window_handle, (DataInterchange)data, (string)format);
            }
            ;
    }

    protected static Imports.Delegate_on_clipboard_receive_data TranslateDelegate_on_clipboard_receive_data(
        Delegate_on_clipboard_receive_data callback)
    {
        return (window_handle, data) => { callback((CrystalWindow)window_handle, (DataInterchange)data); }
            ;
    }

    protected static Imports.Delegate_on_idle TranslateDelegate_on_idle(Delegate_on_idle callback)
    {
        return window_handle => { callback((CrystalWindow)window_handle); }
            ;
    }

    public class Imports
    {
        // void (*)(P_INSTANCE DataInterchange data)
        public delegate void ClipboardCopyWithCallback_provide(IntPtr data);

        // void (*on_clipboard_provide_chosen)(P_INSTANCE WindowHandle window_handle, P_INSTANCE DataInterchange data, utf8_string_struct format)
        public delegate void Delegate_on_clipboard_provide_chosen(IntPtr window_handle, IntPtr data,
            ref utf8_string_struct format);

        // void (*on_clipboard_receive_data)(P_INSTANCE WindowHandle window_handle, P_INSTANCE DataInterchange data)
        public delegate void Delegate_on_clipboard_receive_data(IntPtr window_handle, IntPtr data);

        // void (*on_close)(P_INSTANCE WindowHandle window_handle)
        public delegate void Delegate_on_close(IntPtr window_handle);

        // void (*on_drag_provide_chosen)(P_INSTANCE WindowHandle window_handle, P_INSTANCE DragDropData data, utf8_string_struct format)
        public delegate void Delegate_on_drag_provide_chosen(IntPtr window_handle, IntPtr data,
            ref utf8_string_struct format);

        // void (*on_drag_provide_finished)(P_INSTANCE WindowHandle window_handle, P_INSTANCE DragDropData data, bool success)
        public delegate void Delegate_on_drag_provide_finished(IntPtr window_handle, IntPtr data, bool success);

        // void (*on_drag_provide_status)(P_INSTANCE WindowHandle window_handle, P_INSTANCE DragDropData data)
        public delegate void Delegate_on_drag_provide_status(IntPtr window_handle, IntPtr data);

        // void (*on_drag_receive_drop)(P_INSTANCE WindowHandle window_handle, P_INSTANCE DragDropData data)
        public delegate void Delegate_on_drag_receive_drop(IntPtr window_handle, IntPtr data);

        // void (*on_drag_receive_enter)(P_INSTANCE WindowHandle window_handle, P_INSTANCE DragDropData data)
        public delegate void Delegate_on_drag_receive_enter(IntPtr window_handle, IntPtr data);

        // void (*on_drag_receive_leave)(P_INSTANCE WindowHandle window_handle, P_INSTANCE DragDropData data)
        public delegate void Delegate_on_drag_receive_leave(IntPtr window_handle, IntPtr data);

        // void (*on_drag_receive_motion)(P_INSTANCE WindowHandle window_handle, P_INSTANCE DragDropData data, int32_t x, int32_t y, uint32_t key_state)
        public delegate void Delegate_on_drag_receive_motion(IntPtr window_handle, IntPtr data, int x, int y,
            uint key_state);

        // utf8_string_struct (*on_drag_receive_select)(P_INSTANCE WindowHandle window_handle, P_INSTANCE DragDropData data)
        public delegate string Delegate_on_drag_receive_select(IntPtr window_handle, IntPtr data);

        // void (*on_drag_receive_start)(P_INSTANCE WindowHandle window_handle, P_INSTANCE DragDropData data)
        public delegate void Delegate_on_drag_receive_start(IntPtr window_handle, IntPtr data);

        // void (*on_draw)(P_INSTANCE WindowHandle window_handle)
        public delegate void Delegate_on_draw(IntPtr window_handle);

        // void (*on_focus_in)(P_INSTANCE WindowHandle window_handle)
        public delegate void Delegate_on_focus_in(IntPtr window_handle);

        // void (*on_focus_out)(P_INSTANCE WindowHandle window_handle)
        public delegate void Delegate_on_focus_out(IntPtr window_handle);

        // void (*on_idle)(P_INSTANCE WindowHandle window_handle)
        public delegate void Delegate_on_idle(IntPtr window_handle);

        // void (*on_key_down)(P_INSTANCE WindowHandle window_handle, int32_t keycode)
        public delegate void Delegate_on_key_down(IntPtr window_handle, int keycode);

        // void (*on_key_up)(P_INSTANCE WindowHandle window_handle, int32_t keycode)
        public delegate void Delegate_on_key_up(IntPtr window_handle, int keycode);

        // void (*on_mouse_down)(P_INSTANCE WindowHandle window_handle, int32_t button, int32_t x, int32_t y)
        public delegate void Delegate_on_mouse_down(IntPtr window_handle, int button, int x, int y);

        // void (*on_mouse_move)(P_INSTANCE WindowHandle window_handle, int32_t x, int32_t y)
        public delegate void Delegate_on_mouse_move(IntPtr window_handle, int x, int y);

        // void (*on_mouse_up)(P_INSTANCE WindowHandle window_handle, int32_t button, int32_t x, int32_t y)
        public delegate void Delegate_on_mouse_up(IntPtr window_handle, int button, int x, int y);

        // void (*on_resize)(P_INSTANCE WindowHandle window_handle, int32_t width, int32_t height)
        public delegate void Delegate_on_resize(IntPtr window_handle, int width, int height);

        // P_INSTANCE DataInterchange CrystalWindow_ClipboardPaste(P_INSTANCE WindowHandle handle)
        [DllImport("CrystalCatalystLibrary")]
        public static extern IntPtr CrystalWindow_ClipboardPaste(IntPtr handle);

        // void CrystalWindow_ClipboardCopy(P_INSTANCE WindowHandle handle, P_INSTANCE DataInterchange data)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_ClipboardCopy(IntPtr handle, IntPtr data);

        // void CrystalWindow_ClipboardCopyWithCallback(void (*)(P_INSTANCE DataInterchange data) provide, P_INSTANCE DataInterchange data)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_ClipboardCopyWithCallback(ClipboardCopyWithCallback_provide provide,
            IntPtr data);

        // void CrystalWindow_ClipboardCopyPersist(P_INSTANCE WindowHandle handle, P_INSTANCE DataInterchange data)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_ClipboardCopyPersist(IntPtr handle, IntPtr data);

        // void CrystalWindow_ClipboardClear()
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_ClipboardClear();

        // void CrystalWindow_RegisterDragTarget(P_INSTANCE WindowHandle window_handle)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_RegisterDragTarget(IntPtr window_handle);

        // void CrystalWindow_DragStart(P_INSTANCE WindowHandle handle, P_INSTANCE DragDropData data, int32_t x, int32_t y)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_DragStart(IntPtr handle, IntPtr data, int x, int y);

        // void CrystalWindow_DragChoose(P_INSTANCE WindowHandle handle, P_INSTANCE DragDropData data, utf8_string_struct fmt)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_DragChoose(IntPtr handle, IntPtr data, ref utf8_string_struct fmt);

        // P_INSTANCE WindowHandle CrystalWindow_Create(int32_t width, int32_t height, utf8_string_struct title)
        [DllImport("CrystalCatalystLibrary")]
        public static extern IntPtr CrystalWindow_Create(int width, int height, ref utf8_string_struct title);

        // P_INSTANCE WindowHandle CrystalWindow_CreateSimple(int32_t width, int32_t height, utf8_string_struct title)
        [DllImport("CrystalCatalystLibrary")]
        public static extern IntPtr CrystalWindow_CreateSimple(int width, int height, ref utf8_string_struct title);

        // void CrystalWindow_ApplicationRetain(P_INSTANCE WindowHandle window_handle)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_ApplicationRetain(IntPtr window_handle);

        // void CrystalWindow_ApplicationRelease(P_INSTANCE WindowHandle window_handle)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_ApplicationRelease(IntPtr window_handle);

        // void CrystalWindow_PresentPix(P_INSTANCE WindowHandle window_handle, P_INSTANCE PixData pix)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_PresentPix(IntPtr window_handle, ref PixData pix);

        // void CrystalWindow_PresentImage(P_INSTANCE WindowHandle window_handle, utf8_string_struct pixformat, P_ELEMENTS void pixdata, size_t pixdata_length, int32_t width, int32_t height)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_PresentImage(IntPtr window_handle, ref utf8_string_struct pixformat,
            IntPtr pixdata, IntPtr pixdata_length, int width, int height);

        // void CrystalWindow_CursorPix(P_INSTANCE WindowHandle window_handle, P_INSTANCE PixData pix, int32_t hot_x, int32_t hot_y)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_CursorPix(IntPtr window_handle, ref PixData pix, int hot_x, int hot_y);

        // void CrystalWindow_SetCursor(P_INSTANCE WindowHandle window_handle, utf8_string_struct pixformat, P_ELEMENTS void pixdata, size_t pixdata_length, int32_t width, int32_t height, int32_t hot_x, int32_t hot_y)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_SetCursor(IntPtr window_handle, ref utf8_string_struct pixformat,
            IntPtr pixdata, IntPtr pixdata_length, int width, int height, int hot_x, int hot_y);

        // void CrystalWindow_SetStandardCursor(P_INSTANCE WindowHandle window_handle, CrystalCursor cursor_enum)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_SetStandardCursor(IntPtr window_handle, CrystalCursor cursor_enum);

        // void CrystalWindow_IconPix(P_INSTANCE WindowHandle window_handle, P_INSTANCE PixData pix)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_IconPix(IntPtr window_handle, ref PixData pix);

        // void CrystalWindow_SetIcon(P_INSTANCE WindowHandle window_handle, utf8_string_struct pixformat, P_ELEMENTS void pixdata, size_t pixdata_length, int32_t width, int32_t height)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_SetIcon(IntPtr window_handle, ref utf8_string_struct pixformat,
            IntPtr pixdata, IntPtr pixdata_length, int width, int height);

        // void CrystalWindow_QueueRedraw(P_INSTANCE WindowHandle window_handle)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_QueueRedraw(IntPtr window_handle);

        // void CrystalWindow_MouseCapture(P_INSTANCE WindowHandle window_handle)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_MouseCapture(IntPtr window_handle);

        // void CrystalWindow_MouseRelease(P_INSTANCE WindowHandle window_handle)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_MouseRelease(IntPtr window_handle);

        // void CrystalWindow_GLInit(P_INSTANCE WindowHandle window_handle)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_GLInit(IntPtr window_handle);

        // void CrystalWindow_GLMakeCurrent(P_INSTANCE WindowHandle window_handle)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_GLMakeCurrent(IntPtr window_handle);

        // void CrystalWindow_GLPresent(P_INSTANCE WindowHandle window_handle)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_GLPresent(IntPtr window_handle);

        // void* CrystalWindow_GLGetProcAddress(P_INSTANCE WindowHandle window_handle, const char* name)
        [DllImport("CrystalCatalystLibrary")]
        public static extern IntPtr CrystalWindow_GLGetProcAddress(IntPtr window_handle, [MarshalAs(UnmanagedType.LPStr)] string name);

        // void CrystalWindow_Show(P_INSTANCE WindowHandle window_handle, bool restore)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_Show(IntPtr window_handle, bool restore);

        // void CrystalWindow_Close(P_INSTANCE WindowHandle window_handle)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_Close(IntPtr window_handle);

        // void CrystalWindow_PostClose(P_INSTANCE WindowHandle window_handle)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_PostClose(IntPtr window_handle);

        // void CrystalWindow_SetSize(P_INSTANCE WindowHandle window_handle, int32_t width, int32_t height)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_SetSize(IntPtr window_handle, int width, int height);

        // void CrystalWindow_GetSize(P_INSTANCE WindowHandle window_handle, P_OUT int32_t width, P_OUT int32_t height)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_GetSize(IntPtr window_handle, out int width, out int height);

        // void CrystalWindow_SetLocation(P_INSTANCE WindowHandle window_handle, int32_t x, int32_t y)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_SetLocation(IntPtr window_handle, int x, int y);

        // void CrystalWindow_GetLocation(P_INSTANCE WindowHandle window_handle, P_OUT int32_t x, P_OUT int32_t y)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_GetLocation(IntPtr window_handle, out int x, out int y);

        // void CrystalWindow_SetTitle(P_INSTANCE WindowHandle window_handle, utf8_string_struct title)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_SetTitle(IntPtr window_handle, ref utf8_string_struct title);

        // void CrystalWindow_GetTitle(P_INSTANCE WindowHandle window_handle, P_OUT utf8_string_struct title)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_GetTitle(IntPtr window_handle, out utf8_string_struct title);

        // void CrystalWindow_GetDefaultStockIcon(P_OUT utf8_string_struct pixformat, P_OUT P_ELEMENTS void pixdata, P_OUT size_t pixdata_length, P_OUT int32_t width, P_OUT int32_t height)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_GetDefaultStockIcon(out utf8_string_struct pixformat,
            out IntPtr pixdata, out IntPtr pixdata_length, out int width, out int height);

        // bool CrystalWindow_SetMessageHandler(P_INSTANCE WindowHandle window_handle, utf8_string_struct handler_name, P_INSTANCE void handler)
        [DllImport("CrystalCatalystLibrary")]
        public static extern bool CrystalWindow_SetMessageHandler(IntPtr window_handle,
            ref utf8_string_struct handler_name, IntPtr handler);

        // double CrystalWindow_uptimeSeconds(P_INSTANCE WindowHandle window_handle)
        [DllImport("CrystalCatalystLibrary")]
        public static extern double CrystalWindow_uptimeSeconds(IntPtr window_handle);

        // void CrystalWindow_uptimeReset(P_INSTANCE WindowHandle window_handle)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_uptimeReset(IntPtr window_handle);
    }
}