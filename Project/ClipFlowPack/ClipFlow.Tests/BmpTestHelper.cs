using System.IO;

namespace ClipFlow.Tests;

public static class BmpTestHelper
{
    public static byte[] CreateMinimal24BppBmp(int width = 4, int height = 4)
    {
        int rowStride = (width * 3 + 3) & ~3;
        int imageSize = rowStride * height;
        int fileSize = 54 + imageSize;

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // BMP Header (14 bytes)
        bw.Write((byte)'B');
        bw.Write((byte)'M');
        bw.Write(fileSize);
        bw.Write((short)0); // reserved1
        bw.Write((short)0); // reserved2
        bw.Write(54); // data offset

        // DIB Header (BITMAPINFOHEADER - 40 bytes)
        bw.Write(40); // header size
        bw.Write(width);
        bw.Write(height);
        bw.Write((short)1); // planes
        bw.Write((short)24); // bit count
        bw.Write(0); // compression (BI_RGB)
        bw.Write(imageSize);
        bw.Write(2835); // x ppm
        bw.Write(2835); // y ppm
        bw.Write(0); // colors used
        bw.Write(0); // colors important

        // Pixel data (BGR format, bottom-to-top)
        byte[] rowData = new byte[rowStride];
        for (int x = 0; x < width; x++)
        {
            rowData[x * 3 + 0] = 255; // B
            rowData[x * 3 + 1] = 0;   // G
            rowData[x * 3 + 2] = 0;   // R
        }

        for (int y = 0; y < height; y++)
        {
            bw.Write(rowData);
        }

        return ms.ToArray();
    }
}
