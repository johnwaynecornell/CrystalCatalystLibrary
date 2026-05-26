using System.Runtime.InteropServices;
using JWCEssentials.net;

namespace CrystalCatalystLibrary.net;

public class Pixels
{
    public static PixData ConvertPixels(string pixformat, string pixformat_dest, IntPtr pixdata, IntPtr pixdata_length,
        int width, int height)
    {
        utf8_string_struct param_pixformat = pixformat;
        utf8_string_struct param_pixformat_dest = pixformat_dest;
        return Imports.Pixels_ConvertPixels(ref param_pixformat, ref param_pixformat_dest, pixdata, pixdata_length,
            width, height);
    }

    public class Imports
    {
        // PixData Pixels_ConvertPixels(utf8_string_struct pixformat, utf8_string_struct pixformat_dest, P_ELEMENTS void pixdata, size_t pixdata_length, int32_t width, int32_t height)
        [DllImport("CrystalCatalystLibrary")]
        public static extern PixData Pixels_ConvertPixels(ref utf8_string_struct pixformat,
            ref utf8_string_struct pixformat_dest, IntPtr pixdata, IntPtr pixdata_length, int width, int height);
    }
}