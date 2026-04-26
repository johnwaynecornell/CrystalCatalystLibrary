using System.Runtime.InteropServices;

namespace CrystalCatalystLibrary;

public class CrystalCatalystLibrary
{
    public static bool Initialize()
    {
        return Imports.CrystalCatalystLibrary_Initialize();
    }

    public static bool Close()
    {
        return Imports.CrystalCatalystLibrary_Close();
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