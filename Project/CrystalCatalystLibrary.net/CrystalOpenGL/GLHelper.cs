using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Silk.NET.OpenGL;

namespace CrystalOpenGL;

public static class GLHelper
{
    private delegate void DrawElementsDelegate(GL gl, PrimitiveType mode, uint count, DrawElementsType type, IntPtr indices);
    private static readonly DrawElementsDelegate _drawElements;

    static GLHelper()
    {
        // Find the MethodInfo for DrawElements(PrimitiveType, uint, DrawElementsType, void*)
        var method = typeof(GL).GetMethods()
            .FirstOrDefault(m => m.Name == "DrawElements" && 
                                 m.GetParameters().Length == 4 && 
                                 m.GetParameters()[0].ParameterType == typeof(PrimitiveType) &&
                                 m.GetParameters()[1].ParameterType == typeof(uint) &&
                                 m.GetParameters()[2].ParameterType == typeof(DrawElementsType) &&
                                 m.GetParameters()[3].ParameterType.IsPointer);

        if (method == null)
        {
            // Fallback for different parameter types (e.g. GLEnum instead of PrimitiveType)
            method = typeof(GL).GetMethods()
                .FirstOrDefault(m => m.Name == "DrawElements" && 
                                     m.GetParameters().Length == 4 && 
                                     m.GetParameters()[3].ParameterType.IsPointer);
        }

        if (method == null)
        {
            throw new Exception("Could not find GL.DrawElements with a pointer parameter");
        }

        var dynamicMethod = new DynamicMethod(
            "DrawElementsIntPtr",
            null,
            new[] { typeof(GL), typeof(PrimitiveType), typeof(uint), typeof(DrawElementsType), typeof(IntPtr) },
            typeof(GLHelper).Module,
            true);

        var il = dynamicMethod.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0); // gl
        il.Emit(OpCodes.Ldarg_1); // mode
        il.Emit(OpCodes.Ldarg_2); // count
        il.Emit(OpCodes.Ldarg_3); // type
        il.Emit(OpCodes.Ldarg, 4); // indices (IntPtr)
        il.Emit(OpCodes.Callvirt, method);
        il.Emit(OpCodes.Ret);

        _drawElements = (DrawElementsDelegate)dynamicMethod.CreateDelegate(typeof(DrawElementsDelegate));
    }

    public static void DrawElements(GL gl, PrimitiveType mode, uint count, DrawElementsType type, IntPtr indices)
    {
        _drawElements(gl, mode, count, type, indices);
    }
}
