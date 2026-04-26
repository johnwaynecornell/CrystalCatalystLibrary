using System.Runtime.InteropServices;
using JWCEssentials.net;

namespace CrystalCatalystLibrary;

public class DragDropData : DataInterchange
{
    public DragDropData(IntPtr Handle) : base(Handle)
    {
    }

    public static string DragActionsString(DragActions actions)
    {
        return Imports.DragDropData_DragActionsString(actions);
    }

    public static DragDropData Create()
    {
        return new DragDropData(Imports.DragDropData_Create());
    }

    public class Imports
    {
        // utf8_string_struct DragDropData_DragActionsString(DragActions actions)
        [DllImport("CrystalCatalystLibrary")]
        public static extern utf8_string_struct DragDropData_DragActionsString(DragActions actions);

        // P_INSTANCE DragDropData DragDropData_Create()
        [DllImport("CrystalCatalystLibrary")]
        public static extern IntPtr DragDropData_Create();
    }
}