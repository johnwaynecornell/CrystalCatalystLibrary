using System.Runtime.InteropServices;
using JWCEssentials.net;

namespace CrystalCatalystLibrary;

public class CrystalWindow
{
    public IntPtr Handle;

    public CrystalWindow(IntPtr Handle)
    {
        this.Handle = Handle;
    }

    public DataInterchange ClipboardPaste()
    {
        return new DataInterchange(Imports.CrystalWindow_ClipboardPaste(Handle));
    }

    public void ClipboardCopy(DataInterchange data)
    {
        Imports.CrystalWindow_ClipboardCopy(Handle, data.Handle);
    }

    public static void ClipboardCopyWithCallback(Imports.ClipboardCopyWithCallback_provide provide, DataInterchange data)
    {
        Imports.CrystalWindow_ClipboardCopyWithCallback((Imports.ClipboardCopyWithCallback_provide)provide, data.Handle);
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
        return new CrystalWindow(Imports.CrystalWindow_Create(width, height, ref param_title));
    }

    public static CrystalWindow CreateSimple(int width, int height, string title)
    {
        utf8_string_struct param_title = title;
        return new CrystalWindow(Imports.CrystalWindow_CreateSimple(width, height, ref param_title));
    }

    public void ApplicationRetain()
    {
        Imports.CrystalWindow_ApplicationRetain(Handle);
    }

    public void ApplicationRelease()
    {
        Imports.CrystalWindow_ApplicationRelease(Handle);
    }

    public void PresentImage(string pixformat, IntPtr pixdata, IntPtr pixdata_length, int width, int height)
    {
        utf8_string_struct param_pixformat = pixformat;
        Imports.CrystalWindow_PresentImage(Handle, ref param_pixformat, pixdata, pixdata_length, width, height);
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

    public void Show(bool restore)
    {
        Imports.CrystalWindow_Show(Handle, restore);
    }

    public void Close()
    {
        Imports.CrystalWindow_Close(Handle);
    }

    public void PostClose()
    {
        Imports.CrystalWindow_PostClose(Handle);
    }

    public bool SetMessaqeHandler(string handler_name, IntPtr handler)
    {
        utf8_string_struct param_handler_name = handler_name;
        return Imports.CrystalWindow_SetMessaqeHandler(Handle, ref param_handler_name, handler);
    }

    public double uptimeSeconds()
    {
        return Imports.CrystalWindow_uptimeSeconds(Handle);
    }

    public void uptimeReset()
    {
        Imports.CrystalWindow_uptimeReset(Handle);
    }

    public class Imports
    {
        // void (*)(P_INSTANCE DataInterchange data)
        public delegate void ClipboardCopyWithCallback_provide(IntPtr data);

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

        // void CrystalWindow_PresentImage(P_INSTANCE WindowHandle window_handle, utf8_string_struct pixformat, P_ELEMENTS void pixdata, size_t pixdata_length, int32_t width, int32_t height)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_PresentImage(IntPtr window_handle, ref utf8_string_struct pixformat,
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

        // void CrystalWindow_Show(P_INSTANCE WindowHandle window_handle, bool restore)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_Show(IntPtr window_handle, bool restore);

        // void CrystalWindow_Close(P_INSTANCE WindowHandle window_handle)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_Close(IntPtr window_handle);

        // void CrystalWindow_PostClose(P_INSTANCE WindowHandle window_handle)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_PostClose(IntPtr window_handle);

        // bool CrystalWindow_SetMessaqeHandler(P_INSTANCE WindowHandle window_handle, utf8_string_struct handler_name, P_INSTANCE void handler)
        [DllImport("CrystalCatalystLibrary")]
        public static extern bool CrystalWindow_SetMessaqeHandler(IntPtr window_handle,
            ref utf8_string_struct handler_name, IntPtr handler);

        // double CrystalWindow_uptimeSeconds(P_INSTANCE WindowHandle window_handle)
        [DllImport("CrystalCatalystLibrary")]
        public static extern double CrystalWindow_uptimeSeconds(IntPtr window_handle);

        // void CrystalWindow_uptimeReset(P_INSTANCE WindowHandle window_handle)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void CrystalWindow_uptimeReset(IntPtr window_handle);
    }
}