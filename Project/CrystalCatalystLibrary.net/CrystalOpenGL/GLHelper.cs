using System;
using Silk.NET.OpenGL;

namespace CrystalOpenGL;

/// <summary>
/// Provides helper methods for common OpenGL operations.
/// </summary>
public static class GLHelper
{
    /// <summary>
    /// Draws elements from index data in unmanaged memory.
    /// </summary>
    public static void DrawElements(GL gl, PrimitiveType mode, uint count, DrawElementsType type, IntPtr indices)
    {
        GLBridges.DrawElements(gl, mode, count, type, indices);
    }
}
