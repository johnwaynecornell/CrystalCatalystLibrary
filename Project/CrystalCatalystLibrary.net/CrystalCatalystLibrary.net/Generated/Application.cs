using System.Runtime.InteropServices;
using JWCEssentials.net;

namespace CrystalCatalystLibrary;

public class Application
{
    public static void Init(struct_array_struct<utf8_string_struct> args)
    {
        Imports.Application_Init(args);
    }

    public static int Run()
    {
        return Imports.Application_Run();
    }

    public static void SignalClose()
    {
        Imports.Application_SignalClose();
    }

    public static int ArgumentCount()
    {
        return Imports.Application_ArgumentCount();
    }

    public static string Argument(int index)
    {
        return Imports.Application_Argument(index);
    }

    public static void ArgumentRemove(int index)
    {
        Imports.Application_ArgumentRemove(index);
    }

    public static void WindowAdd(CrystalWindow window_handle)
    {
        Imports.Application_WindowAdd(window_handle.Handle);
    }

    public static void WindowRemove(CrystalWindow window_handle)
    {
        Imports.Application_WindowRemove(window_handle.Handle);
    }

    public class Imports
    {
        // void Application_Init(struct_array_struct<utf8_string_struct> args)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void Application_Init(struct_array_struct<utf8_string_struct> args);

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