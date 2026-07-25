using System.Runtime.InteropServices;
using JWCEssentials.net;

namespace CrystalCatalystLibrary.net;
public partial class Pixels
{
    public static PixData CopyOrConvertPix(ref PixData  pix, string  pixformat_dest)
    {
        utf8_string_struct param_pixformat_dest = pixformat_dest;
        PixData Ret = ( PixData ) Imports.Pixels_CopyOrConvertPix(ref pix,  ref param_pixformat_dest);
        return Ret;
    }
    public static PixData CopyOrConvert(string  pixformat, string  pixformat_dest, IntPtr  pixdata, IntPtr  pixdata_length, int  width, int  height)
    {
        utf8_string_struct param_pixformat = pixformat;
        utf8_string_struct param_pixformat_dest = pixformat_dest;
        PixData Ret = ( PixData ) Imports.Pixels_CopyOrConvert( ref param_pixformat,  ref param_pixformat_dest, (IntPtr)pixdata, (IntPtr)pixdata_length, (int)width, (int)height);
        return Ret;
    }
    public static PixData ConvertPixelsPix(ref PixData  pix, string  pixformat_dest)
    {
        utf8_string_struct param_pixformat_dest = pixformat_dest;
        PixData Ret = ( PixData ) Imports.Pixels_ConvertPixelsPix(ref pix,  ref param_pixformat_dest);
        return Ret;
    }
    public static PixData ConvertPixels(string  pixformat, string  pixformat_dest, IntPtr  pixdata, IntPtr  pixdata_length, int  width, int  height)
    {
        utf8_string_struct param_pixformat = pixformat;
        utf8_string_struct param_pixformat_dest = pixformat_dest;
        PixData Ret = ( PixData ) Imports.Pixels_ConvertPixels( ref param_pixformat,  ref param_pixformat_dest, (IntPtr)pixdata, (IntPtr)pixdata_length, (int)width, (int)height);
        return Ret;
    }
    public static void CopyTo(ref PixData  pixFrom, IntPtr  pixdataTo)
    {
        Imports.Pixels_CopyTo(ref pixFrom, (IntPtr)pixdataTo);
    }
    public static void CopyFrom(ref PixData  pixTo, IntPtr  pixdataFrom)
    {
        Imports.Pixels_CopyFrom(ref pixTo, (IntPtr)pixdataFrom);
    }

    public class Imports
    {
        // PixData Pixels_CopyOrConvertPix(P_INSTANCE PixData pix, utf8_string_struct pixformat_dest)
        [DllImport("CrystalCatalystLibrary")]
        public static extern PixData Pixels_CopyOrConvertPix(ref PixData pix, ref utf8_string_struct pixformat_dest);

        // PixData Pixels_CopyOrConvert(utf8_string_struct pixformat, utf8_string_struct pixformat_dest, P_ELEMENTS void pixdata, size_t pixdata_length, int32_t width, int32_t height)
        [DllImport("CrystalCatalystLibrary")]
        public static extern PixData Pixels_CopyOrConvert(ref utf8_string_struct pixformat, ref utf8_string_struct pixformat_dest, IntPtr pixdata, IntPtr pixdata_length, int width, int height);

        // PixData Pixels_ConvertPixelsPix(P_INSTANCE PixData pix, utf8_string_struct pixformat_dest)
        [DllImport("CrystalCatalystLibrary")]
        public static extern PixData Pixels_ConvertPixelsPix(ref PixData pix, ref utf8_string_struct pixformat_dest);

        // PixData Pixels_ConvertPixels(utf8_string_struct pixformat, utf8_string_struct pixformat_dest, P_ELEMENTS void pixdata, size_t pixdata_length, int32_t width, int32_t height)
        [DllImport("CrystalCatalystLibrary")]
        public static extern PixData Pixels_ConvertPixels(ref utf8_string_struct pixformat, ref utf8_string_struct pixformat_dest, IntPtr pixdata, IntPtr pixdata_length, int width, int height);

        // void Pixels_CopyTo(P_INSTANCE PixData pixFrom, P_ELEMENTS void pixdataTo)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void Pixels_CopyTo(ref PixData pixFrom, IntPtr pixdataTo);

        // void Pixels_CopyFrom(P_INSTANCE PixData pixTo, P_ELEMENTS void pixdataFrom)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void Pixels_CopyFrom(ref PixData pixTo, IntPtr pixdataFrom);

    }
}
