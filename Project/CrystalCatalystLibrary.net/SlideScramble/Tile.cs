using SkiaSharp;

namespace SlideScramble;

public class Tile
{
    public int OriginX { get; }
    public int OriginY { get; }
    public SKImage Image { get; set; }

    public Tile(int originX, int originY, SKImage image)
    {
        OriginX = originX;
        OriginY = originY;
        Image = image;
    }
}
