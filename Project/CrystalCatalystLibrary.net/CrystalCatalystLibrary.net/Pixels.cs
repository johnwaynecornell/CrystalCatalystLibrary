namespace CrystalCatalystLibrary.net;

public partial class Pixels
{
    /* See Project/CrystalCatalystLibrary.net/CrystalCatalystLibrary.net/Generated/Pixels.cs for the full implementation */

    public static void BltRaw(PixData src, PixData dest)
    {
        if (src.width != dest.width)
            throw new ArgumentException($"Source width ({src.width}) does not match destination width ({dest.width})");

        if (src.height != dest.height)
            throw new ArgumentException($"Source height ({src.height}) does not match destination height ({dest.height})");

        if (src.pix_format != dest.pix_format)
            throw new ArgumentException($"Source pixel format ({src.pix_format}) does not match destination pixel format ({dest.pix_format})");
        
        CopyTo(ref dest, src.pix_data);
    }
}