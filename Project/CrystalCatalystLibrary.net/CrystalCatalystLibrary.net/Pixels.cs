namespace CrystalCatalystLibrary.net;

public partial class Pixels
{
    /* See Project/CrystalCatalystLibrary.net/CrystalCatalystLibrary.net/Generated/Pixels.cs for the full implementation */

    public static void ValidateSameRawLayout(PixData src, PixData dest)
    {
        if (src.width != dest.width)
            throw new ArgumentException($"Source width ({src.width}) does not match destination width ({dest.width})");

        if (src.height != dest.height)
            throw new ArgumentException($"Source height ({src.height}) does not match destination height ({dest.height})");

        if (src.pix_format != dest.pix_format)
            throw new ArgumentException($"Source pixel format ({src.pix_format}) does not match destination pixel format ({dest.pix_format})");

        if (src.pix_data_length != dest.pix_data_length)
            throw new ArgumentException($"Source pixel data length ({src.pix_data_length}) does not match destination pixel data length ({dest.pix_data_length})");

        long expectedLength = PixFormats.GetExpectedByteLength(src.pix_format, src.width, src.height);

        if (src.pix_data_length != expectedLength)
            throw new ArgumentException($"Source pixel data length ({src.pix_data_length}) does not match expected pixel data length ({expectedLength})");
    }
    
    public static void BltRaw(PixData src, PixData dest)
    {
        ValidateSameRawLayout(src, dest);
        BltRawUnchecked(src, dest);
    }

    public static void BltRawUnchecked(PixData src, PixData dest)
    {
        CopyTo(ref dest, src.pix_data);
    }
}