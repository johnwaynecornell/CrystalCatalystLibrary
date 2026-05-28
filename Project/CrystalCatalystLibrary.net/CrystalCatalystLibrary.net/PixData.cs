using JWCEssentials.net;

namespace CrystalCatalystLibrary.net;

public struct PixData
{
    public utf8_string_struct pix_format;
    public int pix_data;
    public IntPtr pix_data_length;
    public int width;
    public int height;

    public delegate bool Pix_data_free(ref PixData pixdata);

    public Pix_data_free pix_data_free;
}