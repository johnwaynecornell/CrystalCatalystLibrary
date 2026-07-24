using System;
using System.Runtime.CompilerServices;
using Silk.NET.OpenGL;

namespace CrystalOpenGL;

/// <summary>
/// Provides helper methods for common OpenGL operations.
/// </summary>
public static class GLHelper
{
    private class VersionInfo
    {
        public int Major;
        public int Minor;
    }
    
    private class HasDSAInfo
    {
        public bool HasDSA;
    }
    
    private static readonly ConditionalWeakTable<GL, VersionInfo> _versionCache = new();
    private static readonly ConditionalWeakTable<GL, HasDSAInfo?> _hasDSACache = new();
    private static readonly ConditionalWeakTable<GL, string[]> _extensionStringCache = new();

    /// <summary>
    /// Draws elements from index data in unmanaged memory.
    /// </summary>
    public static void DrawElements(GL gl, PrimitiveType mode, uint count, DrawElementsType type, IntPtr indices)
    {
        GLBridges.DrawElements(gl, mode, count, type, indices);
    }
    
    /// <summary>
    /// returns true if the specified extension is available
    /// </summary>
    
    public static bool HasExtension(GL gl, string extensionName)
    {
        string[] extensions = GetExtensions(gl);
        for (int i = 0; i < extensions.Length; i++)
        {
            if (string.Equals(extensions[i], extensionName, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Returns a cached GL extension string array     
    /// </summary>
    
    public static string[] GetExtensions(GL gl)
    {
        return _extensionStringCache.GetValue(gl, k =>
        {
            gl.GetInteger(GetPName.NumExtensions, out int count);
            string[] R = new string[count];

            for (uint i = 0; i < count; i++)
            {
                string? ext = gl.GetStringS(GLEnum.Extensions, i);
                R[i] = ext;
            }

            return R;
        });


    }

    
    /// <summary>
    /// Override the cached value for HasDSA enabling forcing false for debugging purposes, clearing the cache if state null
    /// </summary>

    public static void HasDSA_Override(GL gl, bool ? state)
    {
        _hasDSACache.Remove(gl);
        if (state == null) return;
        _hasDSACache.Add(gl, new HasDSAInfo() { HasDSA = state.Value });
        
    }
    
    /// <summary>
    /// Efficiently determines if Direct State Access is available from 4.5 or the GL_ARB_direct_state_access extension 
    /// </summary>
    public static bool HasDSA(GL gl)
    {
        // DSA is core in OpenGL 4.5, but may be available earlier through
        // GL_ARB_direct_state_access. Prefer feature detection over version-only checks.

        var info = _hasDSACache.GetValue(gl, k =>
        {
            return new HasDSAInfo() { HasDSA = (VersionCompare(gl, 4, 5) >= 0) || 
                                               HasExtension(gl, "GL_ARB_direct_state_access") };
        });

        return info.HasDSA;
    }

    /// <summary>
    /// Efficiently gets the OpenGL version and stores it in Major and Minor.
    /// </summary>

    public static void GetGLVersion(GL gl, out int Major, out int Minor)
    {
        var info = _versionCache.GetValue(gl, k =>
        {
            string? versionString = k.GetStringS(StringName.Version);
            int maj = 0;
            int min = 0;

            if (!string.IsNullOrEmpty(versionString))
            {
                // Split by dot, space, or dash to isolate numbers safely
                string[] tokens = versionString.Split(new[] { '.', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);

                // If using OpenGL ES, the string might start with "OpenGL ES 3.2"
                int index = 0;
                while (index < tokens.Length && !char.IsDigit(tokens[index][0]))
                {
                    index++;
                }

                if (index < tokens.Length) int.TryParse(tokens[index], out maj);
                if (index + 1 < tokens.Length) int.TryParse(tokens[index + 1], out min);
            }

            return new VersionInfo { Major = maj, Minor = min };
        });

        Major = info.Major;
        Minor = info.Minor;
    }
    
    /// <summary>
    /// Compares two versions by major and minor components.
    /// Returns 1 for A&gt;B, 0 for A==B, or -1 for A&lt;B
    /// </summary>

    public static int VersionCompare(int majorA, int minorA, int majorB, int minorB)
    {
        if (majorA > majorB) return 1;
        if (majorA < majorB) return -1;
        if (minorA > minorB) return 1;
        if (minorA < minorB) return -1;
        return 0;
    }

    /// <summary>
    /// Compares gl version to majorB, minorB.
    /// Returns 1 for A&gt;B, 0 for A==B, or -1 for A&lt;B
    /// </summary>

    public static int VersionCompare(GL gl, int majorB, int minorB)
    {
        GetGLVersion(gl, out var majorA, out var minorA);
        return VersionCompare(majorA, minorA, majorB, minorB);
    }

    /// <summary>
    /// Gets an integer value from OpenGL.
    /// </summary>
    public static int GetInteger(GL gl, GetPName name)
    {
        gl.GetInteger(name, out int value);
        return value;
    }
}
