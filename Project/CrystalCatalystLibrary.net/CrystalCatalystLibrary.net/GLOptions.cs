using System.Runtime.InteropServices;

namespace CrystalCatalystLibrary.net;

/// <summary>
/// Specifies the OpenGL profile to use.
/// </summary>
public enum GLProfile : int
{
    /// <summary>Any profile.</summary>
    Any = 0,
    /// <summary>Core profile.</summary>
    Core = 1,
    /// <summary>Compatibility profile.</summary>
    Compatibility = 2
}

/// <summary>
/// Options for configuring the OpenGL context.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GLOptions
{
    /// <summary>Major version of OpenGL.</summary>
    public int major;
    /// <summary>Minor version of OpenGL.</summary>
    public int minor;
    /// <summary>OpenGL profile.</summary>
    public GLProfile profile;
    /// <summary>Enable debug context.</summary>
    [MarshalAs(UnmanagedType.Bool)] public bool debug;
    /// <summary>Enable forward-compatible context.</summary>
    [MarshalAs(UnmanagedType.Bool)] public bool forwardCompatible;
    /// <summary>Bits for depth buffer.</summary>
    public int depthBits;
    /// <summary>Bits for stencil buffer.</summary>
    public int stencilBits;
    /// <summary>Bits for alpha channel.</summary>
    public int alphaBits;
    /// <summary>Enable double buffering.</summary>
    [MarshalAs(UnmanagedType.Bool)] public bool doubleBuffer;
    /// <summary>Enable stereoscopic rendering.</summary>
    [MarshalAs(UnmanagedType.Bool)] public bool stereo;
    /// <summary>Enable strict version checking.</summary>
    [MarshalAs(UnmanagedType.Bool)] public bool strict;

    /// <summary>
    /// Gets the default OpenGL options (3.3 Core, 24-bit depth, 8-bit stencil/alpha, double buffered, strict).
    /// </summary>
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
        stereo = false,
        strict = true
    };
}
