using System.Runtime.InteropServices;
using JWCEssentials.net;

namespace CrystalCatalystLibrary;
public partial class CrystalCatalyst
{
    public partial class Fonts
    {
        public class Imports
        {
            // void (*)(utf8_string_struct OS, utf8_string_struct Instructions)
            public delegate void  HasMSCoreFonts_callback(ref utf8_string_struct OS, ref utf8_string_struct Instructions);

            // bool CrystalCatalyst_Fonts_HasMSCoreFonts(void (*)(utf8_string_struct OS, utf8_string_struct Instructions) callback)
            [DllImport("CrystalCatalystLibrary")]
            public static extern bool  CrystalCatalyst_Fonts_HasMSCoreFonts(HasMSCoreFonts_callback callback);

        }
        public static bool HasMSCoreFonts(HasMSCoreFonts_callback  callback)
        {
            return (bool) Imports.CrystalCatalyst_Fonts_HasMSCoreFonts((HasMSCoreFonts_callback)callback);
        }
    }
}
