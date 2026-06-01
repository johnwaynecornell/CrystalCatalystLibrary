using System.Runtime.InteropServices;

namespace CrystalCatalystLibrary.net;

public enum GLProfile : int
{
    Any = 0,
    Core = 1,
    Compatibility = 2
}

[StructLayout(LayoutKind.Sequential)]
public struct GLOptions
{
    public int major;
    public int minor;
    public GLProfile profile;
    [MarshalAs(UnmanagedType.U1)] public bool debug;
    [MarshalAs(UnmanagedType.U1)] public bool forwardCompatible;
    public int depthBits;
    public int stencilBits;
    public int alphaBits;
    [MarshalAs(UnmanagedType.U1)] public bool doubleBuffer;
    [MarshalAs(UnmanagedType.U1)] public bool stereo;

    public static GLOptions Default => new GLOptions
    {
        major = 3,
        minor = 3,
        profile = GLProfile.Core,
        debug = false,
        forwardCompatible = false,
        depthBits = 24,
        stencilBits = 8,
        alphaBits = 8,
        doubleBuffer = true,
        stereo = false
    };
}
