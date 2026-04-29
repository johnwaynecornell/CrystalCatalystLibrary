using System.Runtime.InteropServices;
using JWCEssentials.net;

namespace CrystalCatalystLibrary;
public partial class SubMutex
{
    public class Imports
    {
        // bool SubMutex_Size(P_OUT size_t sz)
        [DllImport("CrystalCatalystLibrary")]
        public static extern bool  SubMutex_Size(IntPtr sz);

        // bool SubMutex_Init(P_INSTANCE void spiderMutex, utf8_string_struct name)
        [DllImport("CrystalCatalystLibrary")]
        public static extern bool  SubMutex_Init(IntPtr spiderMutex, ref utf8_string_struct name);

        // bool SubMutex_Close(P_INSTANCE void spiderMutex)
        [DllImport("CrystalCatalystLibrary")]
        public static extern bool  SubMutex_Close(IntPtr spiderMutex);

        // bool SubMutex_Lock(P_INSTANCE void spiderMutex)
        [DllImport("CrystalCatalystLibrary")]
        public static extern bool  SubMutex_Lock(IntPtr spiderMutex);

        // bool SubMutex_Unlock(P_INSTANCE void spiderMutex)
        [DllImport("CrystalCatalystLibrary")]
        public static extern bool  SubMutex_Unlock(IntPtr spiderMutex);

    }public IntPtr Handle;
    public SubMutex(IntPtr Handle)
    {
        this.Handle= Handle;
    }public static bool Size(IntPtr  sz)
    {
        return (bool) Imports.SubMutex_Size((IntPtr)sz);
    }public bool Init(string  name)
    {
        utf8_string_struct param_name = name;
        return (bool) Imports.SubMutex_Init(Handle,  ref param_name);
    }public bool Close()
    {
        return (bool) Imports.SubMutex_Close(Handle);
    }public bool Lock()
    {
        return (bool) Imports.SubMutex_Lock(Handle);
    }public bool Unlock()
    {
        return (bool) Imports.SubMutex_Unlock(Handle);
    }}