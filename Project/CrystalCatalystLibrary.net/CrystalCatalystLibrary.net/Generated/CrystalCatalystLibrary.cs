using System.Runtime.InteropServices;
using JWCEssentials.net;

namespace CrystalCatalystLibrary.net;
public partial class CrystalCatalystLibrary
{
    public class Imports
    {
        // bool CrystalCatalystLibrary_Initialize()
        [DllImport("CrystalCatalystLibrary")]
        public static extern bool  CrystalCatalystLibrary_Initialize();

        // bool CrystalCatalystLibrary_Close()
        [DllImport("CrystalCatalystLibrary")]
        public static extern bool  CrystalCatalystLibrary_Close();

    }
    public static bool Initialize()
    {
        return (bool) Imports.CrystalCatalystLibrary_Initialize();
    }
    public static bool Close()
    {
        return (bool) Imports.CrystalCatalystLibrary_Close();
    }
}
