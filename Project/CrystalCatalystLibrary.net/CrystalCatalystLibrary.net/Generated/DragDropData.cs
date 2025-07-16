using System.Runtime.InteropServices;
using JWCEssentials.net;

namespace CrystalCatalystLibrary;
public partial class DragDropData : DataInterchange
{
    public class Imports
    {
        // utf8_string_struct DragDropData_DragActionsString(DragActions actions)
        [DllImport("CrystalCatalystLibrary")]
        public static extern utf8_string_struct  DragDropData_DragActionsString(DragActions actions);

        // P_INSTANCE DragDropData DragDropData_Create()
        [DllImport("CrystalCatalystLibrary")]
        public static extern IntPtr  DragDropData_Create();

    }
    public DragDropData(IntPtr Handle) : base(Handle)
    {
    }
    public static string DragActionsString(DragActions  actions)
    {
        return (string) Imports.DragDropData_DragActionsString((DragActions)actions);
    }
    public static DragDropData Create()
    {
        return new DragDropData(Imports.DragDropData_Create());
    }
}
