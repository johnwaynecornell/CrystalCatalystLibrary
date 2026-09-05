using System.Runtime.InteropServices;
using JWCEssentials.net;

namespace CrystalCatalystLibrary.net;
public partial class Application
{
    public static IntPtr Peek()
    {
        return Imports.Application_Peek();
    }

    public static void Init(string[] args)
    {
        var tmp = (from s in args select (utf8_string_struct)s).ToArray();
        var _args =
            (struct_array_struct<utf8_string_struct>)tmp;

        Imports.Application_Init(ref _args);
    }
    public static int Run()
    {
        int Ret = ( int ) Imports.Application_Run();
        return Ret;
    }
    public static void SignalClose()
    {
        Imports.Application_SignalClose();
    }
    public static int ArgumentCount()
    {
        int Ret = ( int ) Imports.Application_ArgumentCount();
        return Ret;
    }
    public static string Argument(int  index)
    {
        string Ret = ( string ) Imports.Application_Argument((int)index);
        return Ret;
    }
    public static void ArgumentRemove(int  index)
    {
        Imports.Application_ArgumentRemove((int)index);
    }
    public static void WindowAdd(CrystalWindow  window_handle)
    {
        Imports.Application_WindowAdd(window_handle.Handle);
    }
    public static void WindowRemove(CrystalWindow  window_handle)
    {
        Imports.Application_WindowRemove(window_handle.Handle);
    }

    public class Imports
    {
        //P_INSTANCE(CrystalApplication) Application_Peek()
        [DllImport("CrystalCatalystLibrary")]
        public static extern IntPtr Application_Peek();

        // void Application_Init(struct_array_struct<utf8_string_struct> args)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void Application_Init(ref struct_array_struct<utf8_string_struct> args);

        // int32_t Application_Run()
        [DllImport("CrystalCatalystLibrary")]
        public static extern int Application_Run();

        // void Application_SignalClose()
        [DllImport("CrystalCatalystLibrary")]
        public static extern void Application_SignalClose();

        // int32_t Application_ArgumentCount()
        [DllImport("CrystalCatalystLibrary")]
        public static extern int Application_ArgumentCount();

        // utf8_string_struct Application_Argument(int32_t index)
        [DllImport("CrystalCatalystLibrary")]
        public static extern utf8_string_struct Application_Argument(int index);

        // void Application_ArgumentRemove(int32_t index)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void Application_ArgumentRemove(int index);

        // void Application_WindowAdd(P_INSTANCE WindowHandle window_handle)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void Application_WindowAdd(IntPtr window_handle);

        // void Application_WindowRemove(P_INSTANCE WindowHandle window_handle)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void Application_WindowRemove(IntPtr window_handle);

    }
}
