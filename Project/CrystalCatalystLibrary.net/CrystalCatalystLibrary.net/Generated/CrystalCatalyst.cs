using System.Runtime.InteropServices;
using JWCEssentials.net;

namespace CrystalCatalystLibrary.net;

public partial class CrystalCatalyst
{
    public class Fonts
    {
        public delegate void HasMSCoreFonts_callback(string OS, string Instructions);

        protected static Imports.HasMSCoreFonts_callback TranslateHasMSCoreFonts_callback(
            HasMSCoreFonts_callback callback)
        {
            return (ref OS, ref Instructions) => { callback((string)OS, (string)Instructions); }
                ;
        }

        public static bool HasMSCoreFonts(HasMSCoreFonts_callback callback)
        {
            var Ret = Imports.CrystalCatalyst_Fonts_HasMSCoreFonts(TranslateHasMSCoreFonts_callback(callback));
            return Ret;
        }

        public class Imports
        {
            // void (*)(utf8_string_struct OS, utf8_string_struct Instructions)
            public delegate void HasMSCoreFonts_callback(ref utf8_string_struct OS,
                ref utf8_string_struct Instructions);

            // bool CrystalCatalyst_Fonts_HasMSCoreFonts(void (*)(utf8_string_struct OS, utf8_string_struct Instructions) callback)
            [DllImport("CrystalCatalystLibrary")]
            public static extern bool CrystalCatalyst_Fonts_HasMSCoreFonts(HasMSCoreFonts_callback callback);
        }
    }
}