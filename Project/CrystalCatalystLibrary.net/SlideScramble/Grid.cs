using SkiaSharp;

namespace SlideScramble;

public class Grid
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public int TileSize { get; private set; }
    public Tile[,] Tiles { get; private set; }

    public Grid(int width, int height, int tileSize = 128)
    {
        Width = width;
        Height = height;
        TileSize = tileSize;
        Tiles = new Tile[height, width];
        GenerateTiles();
    }

    public static int WrapIndex(int index, int length)
    {
        int remainder = index % length;
        if (remainder < 0)
        {
            remainder += length;
        }
        return remainder;
    }

    public void GenerateTiles()
    {
        using var typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) ?? SKTypeface.Default;
        using var font = new SKFont(typeface, Math.Max(16f, TileSize * 0.32f));

        float cornerRadius = TileSize * 0.1f;
        float padding = 3f;

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var info = new SKImageInfo(TileSize, TileSize, SKColorType.Rgba8888, SKAlphaType.Premul);
                using var surface = SKSurface.Create(info);
                var canvas = surface.Canvas;

                canvas.Clear(SKColors.Transparent);

                var tileRect = new SKRect(padding, padding, TileSize - padding, TileSize - padding);
                var rrect = new SKRoundRect(tileRect, cornerRadius, cornerRadius);

                // Subtle base background
                using var basePaint = new SKPaint
                {
                    Color = new SKColor(15, 23, 42),
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                };
                canvas.DrawRoundRect(rrect, basePaint);

                // Color gradient based on solved position
                float hue = ((x + y * Width) * 320f / Math.Max(1, Width * Height - 1)) % 360f;
                SKColor col1 = SKColor.FromHsv(hue, 75, 90);
                SKColor col2 = SKColor.FromHsv((hue + 35) % 360f, 85, 75);

                using var shader = SKShader.CreateLinearGradient(
                    new SKPoint(tileRect.Left, tileRect.Top),
                    new SKPoint(tileRect.Right, tileRect.Bottom),
                    new[] { col1, col2 },
                    null,
                    SKShaderTileMode.Clamp);

                var innerRect = new SKRect(padding + 2, padding + 2, TileSize - padding - 2, TileSize - padding - 2);
                var innerRRect = new SKRoundRect(innerRect, Math.Max(2, cornerRadius - 2), Math.Max(2, cornerRadius - 2));

                using var fillPaint = new SKPaint
                {
                    Shader = shader,
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                };
                canvas.DrawRoundRect(innerRRect, fillPaint);

                // Inner highlight border
                using var strokePaint = new SKPaint
                {
                    Color = new SKColor(255, 255, 255, 140),
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 2,
                    IsAntialias = true
                };
                canvas.DrawRoundRect(innerRRect, strokePaint);

                // Coordinate label with drop shadow
                string text = $"{x + 1},{y + 1}";
                using var textShadowPaint = new SKPaint
                {
                    Color = new SKColor(0, 0, 0, 180),
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill,
                };
                using var textPaint = new SKPaint
                {
                    Color = SKColors.White,
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill,
                };

                font.MeasureText(text, out SKRect textBounds, textPaint);
                float tx = TileSize / 2f - textBounds.MidX;
                float ty = TileSize / 2f - textBounds.MidY;

                canvas.DrawText(text, tx + 1.5f, ty + 1.5f, font, textShadowPaint);
                canvas.DrawText(text, tx, ty, font, textPaint);

                Tiles[y, x] = new Tile(x, y, surface.Snapshot());
            }
        }
    }

    public void MoveColumn(int x, int delta)
    {
        int height = Tiles.GetLength(0);
        Tile[] work = new Tile[height];

        for (int y = 0; y < height; y++)
        {
            work[y] = Tiles[y, x];
        }

        for (int y = 0; y < height; y++)
        {
            int targetIndex = WrapIndex(y + delta, height);
            Tiles[y, x] = work[targetIndex];
        }
    }

    public void MoveRow(int y, int delta)
    {
        int width = Tiles.GetLength(1);
        Tile[] work = new Tile[width];

        for (int x = 0; x < width; x++)
        {
            work[x] = Tiles[y, x];
        }

        for (int x = 0; x < width; x++)
        {
            int targetIndex = WrapIndex(x + delta, width);
            Tiles[y, x] = work[targetIndex];
        }
    }

    public void MoveX(int x, int delta) => MoveColumn(x, delta);
    public void MoveY(int y, int delta) => MoveRow(y, delta);

    public bool IsSolved()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (Tiles[y, x].OriginX != x || Tiles[y, x].OriginY != y)
                    return false;
            }
        }
        return true;
    }

    public void Reset()
    {
        GenerateTiles();
    }
}
