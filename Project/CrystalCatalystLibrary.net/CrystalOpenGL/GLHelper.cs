using System;
using Silk.NET.OpenGL;

namespace CrystalOpenGL;

public static class GLHelper
{
    public static void DrawElements(GL gl, PrimitiveType mode, uint count, DrawElementsType type, IntPtr indices)
    {
        GLBridges.DrawElements(gl, mode, count, type, indices);
    }
}
