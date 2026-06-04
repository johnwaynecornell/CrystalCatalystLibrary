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

    private static readonly ConditionalWeakTable<GL, VersionInfo> _versionCache = new();

    /// <summary>
    /// Draws elements from index data in unmanaged memory.
    /// </summary>
    public static void DrawElements(GL gl, PrimitiveType mode, uint count, DrawElementsType type, IntPtr indices)
    {
        GLBridges.DrawElements(gl, mode, count, type, indices);
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
}
