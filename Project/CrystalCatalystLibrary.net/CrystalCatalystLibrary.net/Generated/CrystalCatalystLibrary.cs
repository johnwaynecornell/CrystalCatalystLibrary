using System.Runtime.InteropServices;
using JWCEssentials.net;

namespace CrystalCatalystLibrary.net;
public partial class CrystalCatalystLibrary
{
    public static bool Initialize()
    {
        bool Ret = ( bool ) Imports.CrystalCatalystLibrary_Initialize();
        return Ret;
    }
    public static bool Close()
    {
        bool Ret = ( bool ) Imports.CrystalCatalystLibrary_Close();
        return Ret;
    }

    public class Imports
    {
        // bool CrystalCatalystLibrary_Initialize()
        [DllImport("CrystalCatalystLibrary")]
        public static extern bool CrystalCatalystLibrary_Initialize();

        // bool CrystalCatalystLibrary_Close()
        [DllImport("CrystalCatalystLibrary")]
        public static extern bool CrystalCatalystLibrary_Close();

    }
}
