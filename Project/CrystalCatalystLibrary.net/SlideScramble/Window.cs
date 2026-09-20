using CrystalCatalystLibrary.net;
using SkiaSharp;

namespace SlideScramble;

public class Window
{
    public CrystalWindow wnd;
    protected Grid MyGrid;
    
    public int ScrambleCount = 128;
    
    public class Animation
    {
        public Func<Window, Animation, bool>? animation;
    }
    
    public Random rand = new Random();

    public class Drag
    {
        public int startX;
        public int startY;
    }
    
    private Drag? drag = null;

    public Queue<Animation> Animations = new Queue<Animation>();

    public int sample_count = 0;
    public int sample_index = 0;
    public double[] sample_deltas = new double[256];
    public double last_time = 0;
    
    public PixData pixelBacking;

    public bool? animationLockHoriz = null;
    public int animationOrdinal = 0;
    public float animationTileOffset = 0;

    public class TileView
    {
        public float xx = 0;
        public float yy = 0;
        public float size;
    }
    
    public TileView? tileView;

    public GameState State { get; } = new GameState();

    // Cached Skia rendering resources to avoid per-frame allocations
    private readonly SKPaint bgPaint = new SKPaint
    {
        Color = new SKColor(15, 23, 42), // Slate 900
        Style = SKPaintStyle.Fill,
    };

    private readonly SKPaint boardBgPaint = new SKPaint
    {
        Color = new SKColor(30, 41, 59), // Slate 800
        Style = SKPaintStyle.Fill,
        IsAntialias = true,
    };

    private readonly SKPaint boardBorderPaint = new SKPaint
    {
        Color = new SKColor(51, 65, 85), // Slate 700
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 3,
        IsAntialias = true,
    };

    private readonly SKPaint tilePaint = new SKPaint
    {
        Style = SKPaintStyle.Fill,
        Color = SKColors.White,
        IsAntialias = true,
    };

    private readonly SKPaint activeLinePaint = new SKPaint
    {
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 4,
        Color = new SKColor(249, 115, 22), // Orange 500
        IsAntialias = true,
    };

    private readonly SKPaint hudBarPaint = new SKPaint
    {
        Color = new SKColor(30, 41, 59, 230),
        Style = SKPaintStyle.Fill,
        IsAntialias = true,
    };

    private readonly SKPaint hudTextPaint = new SKPaint
    {
        Color = new SKColor(241, 245, 249),
        IsAntialias = true,
        Style = SKPaintStyle.Fill,
    };

    private readonly SKPaint hudMutedPaint = new SKPaint
    {
        Color = new SKColor(148, 163, 184),
        IsAntialias = true,
        Style = SKPaintStyle.Fill,
    };

    private readonly SKPaint hudAccentPaint = new SKPaint
    {
        Color = new SKColor(56, 189, 248), // Sky 400
        IsAntialias = true,
        Style = SKPaintStyle.Fill,
    };

    private readonly SKPaint bannerBgPaint = new SKPaint
    {
        Color = new SKColor(6, 78, 59, 240), // Emerald 900
        Style = SKPaintStyle.Fill,
        IsAntialias = true,
    };

    private readonly SKPaint bannerBorderPaint = new SKPaint
    {
        Color = new SKColor(16, 185, 129), // Emerald 500
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 3,
        IsAntialias = true,
    };

    private readonly SKPaint bannerTitlePaint = new SKPaint
    {
        Color = new SKColor(253, 224, 71), // Yellow 300
        IsAntialias = true,
        Style = SKPaintStyle.Fill,
    };

    private readonly SKPaint bannerTextPaint = new SKPaint
    {
        Color = new SKColor(240, 253, 244),
        IsAntialias = true,
        Style = SKPaintStyle.Fill,
    };

    private readonly SKFont hudFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal) ?? SKTypeface.Default, 14);
    private readonly SKFont hudBoldFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) ?? SKTypeface.Default, 15);
    private readonly SKFont bannerTitleFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) ?? SKTypeface.Default, 24);
    private readonly SKFont bannerTextFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal) ?? SKTypeface.Default, 14);
    private readonly SKFont footerFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal) ?? SKTypeface.Default, 12);

    public Window()
    {
        MyGrid = new Grid(4, 4);

        wnd = CrystalWindow.Create(850, 700, "SlideScramble - CrystalCatalyst Showcase");
        wnd.ApplicationRetain();
        
        wnd.OnDraw = OnDraw;
        wnd.OnIdle = OnIdle;
        wnd.OnClose = (w) =>
        {
            if (pixelBacking)
            {
                pixelBacking.Dispose();
            }
            w.ApplicationRelease();
        };
        
        wnd.OnMouseDown = OnMouseDown;
        wnd.OnMouseUp = OnMouseUp;
        wnd.OnMouseMove = OnMouseMove;

        wnd.OnKeyDown = (handle, keycode) =>
        {
            if (keycode == 0xFF1B) // Escape
            {
                if (drag != null)
                {
                    drag = null;
                    if (animationLockHoriz != null && Math.Abs(animationTileOffset) > 0.001f)
                    {
                        bool horizontal = animationLockHoriz.Value;
                        int ordinal = animationOrdinal;
                        float startOffset = animationTileOffset;
                        QueueSnapAnimation(horizontal, ordinal, startOffset, 0, 0);
                    }
                    else
                    {
                        animationLockHoriz = null;
                        animationTileOffset = 0;
                    }
                }

                return;
            }

            char c = (char)keycode;
            if ((c == 's' || c == 'S') && State.Mode != GameMode.Scrambling && drag == null && Animations.Count == 0)
            {
                State.StartScrambling();
                ScrambleCount = MyGrid.Width * 3;
            }
            else if ((c == 'r' || c == 'R') && State.Mode != GameMode.Scrambling && drag == null && Animations.Count == 0)
            {
                MyGrid.Reset();
                State.Reset();
                animationLockHoriz = null;
                animationTileOffset = 0;
            }
            else if (c == '1')
            {
                SetGridSize(3);
            }
            else if (c == '2')
            {
                SetGridSize(4);
            }
            else if (c == '3')
            {
                SetGridSize(6);
            }
            else if (c == '4')
            {
                SetGridSize(8);
            }
        };
    }

    public void SetGridSize(int size)
    {
        if (State.Mode != GameMode.Scrambling && drag == null && Animations.Count == 0)
        {
            MyGrid = new Grid(size, size);
            State.Reset();
            animationLockHoriz = null;
            animationTileOffset = 0;
        }
    }

    private void OnMouseMove(CrystalWindow windowHandle, int x, int y)
    {
        if (drag != null && tileView != null)
        {
            int xd = x - drag.startX;
            int yd = y - drag.startY;
            
            if (animationLockHoriz == null)
            {
                if (Math.Abs(xd) < Math.Abs(yd))
                {
                    animationLockHoriz = true;
                    int rawOrdinal = (int)((x - tileView.xx) * MyGrid.Width / tileView.size);
                    animationOrdinal = Math.Clamp(rawOrdinal, 0, MyGrid.Width - 1);
                }
                else
                {
                    animationLockHoriz = false;
                    int rawOrdinal = (int)((y - tileView.yy) * MyGrid.Height / tileView.size);
                    animationOrdinal = Math.Clamp(rawOrdinal, 0, MyGrid.Height - 1);
                }
            }
                
            if (animationLockHoriz == false)
            {
                animationTileOffset = -xd * MyGrid.Width / tileView.size;
            }
            else if (animationLockHoriz == true)
            {
                animationTileOffset = -yd * MyGrid.Height / tileView.size;
            }
        }
    }

    private void QueueSnapAnimation(bool horizontal, int ordinal, float startOffset, float targetOffset, int delta)
    {
        Animation anim = new Animation();
        double start = -1;
        float diff = targetOffset - startOffset;
        double duration = Math.Max(0.08, Math.Min(0.25, Math.Abs(diff) * 0.2));

        anim.animation = (window, animation1) =>
        {
            double time = wnd.uptimeSeconds();
            if (start == -1) start = time;
            double elapsed = time - start;

            if (elapsed >= duration)
            {
                animationLockHoriz = null;
                animationTileOffset = 0;
                if (delta != 0)
                {
                    if (!horizontal)
                    {
                        MyGrid.MoveRow(ordinal, delta);
                    }
                    else
                    {
                        MyGrid.MoveColumn(ordinal, delta);
                    }
                    State.RecordMove(wnd.uptimeSeconds());
                    State.CheckSolved(MyGrid, wnd.uptimeSeconds());
                }
                return false;
            }

            float t = Math.Clamp((float)(elapsed / duration), 0f, 1f);
            float progress = 1f - MathF.Pow(1f - t, 3f); // cubic-out easing

            animationLockHoriz = horizontal;
            animationOrdinal = ordinal;
            animationTileOffset = startOffset + diff * progress;

            return true;
        };

        Animations.Enqueue(anim);
    }

    private void OnMouseUp(CrystalWindow windowHandle, int button, int x, int y)
    {
        if (button == (int)CrystalMouseButton.Left)
        {
            if (drag != null && tileView != null)
            {
                drag = null;

                if (animationLockHoriz != null)
                {
                    bool horizontal = animationLockHoriz.Value;
                    int ordinal = animationOrdinal;
                    float currentOffset = animationTileOffset;
                    int delta = (int)Math.Round(currentOffset);

                    QueueSnapAnimation(horizontal, ordinal, currentOffset, delta, delta);
                }
                else
                {
                    animationLockHoriz = null;
                    animationTileOffset = 0;
                }
            }
        }
    }

    private void OnMouseDown(CrystalWindow windowHandle, int button, int x, int y)
    {
        if (State.Mode != GameMode.Scrambling && tileView != null && button == (int)CrystalMouseButton.Left && Animations.Count == 0 && drag == null)
        {
            // Check if mouse is inside the board bounds
            if (x >= tileView.xx && x <= tileView.xx + tileView.size &&
                y >= tileView.yy && y <= tileView.yy + tileView.size)
            {
                drag = new Drag()
                {
                    startX = x,
                    startY = y,
                };
            }
        }
    }

    private void OnIdle(CrystalWindow windowHandle)
    {
        if (Animations.TryPeek(out Animation? animation))
        {
            if (animation?.animation != null && !animation.animation(this, animation))
            {
                Animations.Dequeue();
            }
        }
        else // no current animation
        {
            if (ScrambleCount <= 0 && State.Mode == GameMode.Scrambling)
            {
                State.FinishScrambling(wnd.uptimeSeconds());
            }
            
            if (State.Mode == GameMode.Scrambling)
            {
                bool horizontal = rand.Next(2) == 0;
                int ordinal = rand.Next(horizontal ? MyGrid.Width : MyGrid.Height);
                int delta_length = horizontal ? MyGrid.Height : MyGrid.Width;
                
                int delta = rand.Next(delta_length) - (delta_length >> 1);
                if (delta == 0) delta = 1;

                Animation anim = new Animation();
                double start = -1;
                double duration = Math.Max(0.1, Math.Abs(delta) * 0.12);
                anim.animation = (window, animation1) =>
                {
                    double time = wnd.uptimeSeconds();

                    if (start == -1) start = time;
                    double elapsed = time - start;

                    if (elapsed >= duration)
                    {
                        animationLockHoriz = null;
                        animationTileOffset = 0;
                        if (!horizontal)
                        {
                            MyGrid.MoveRow(ordinal, delta);
                        }
                        else
                        {
                            MyGrid.MoveColumn(ordinal, delta);
                        }
                        return false;
                    }
                    
                    animationLockHoriz = horizontal;
                    animationOrdinal = ordinal;
                    float t = Math.Clamp((float)(elapsed / duration), 0f, 1f);
                    float progress = t * t * (3f - 2f * t); // smoothstep
                    animationTileOffset = delta * progress;
                    
                    return true;
                };
                
                Animations.Enqueue(anim);
                ScrambleCount--;
            }
        }

        windowHandle.QueueRedraw();
    }

    private void OnDraw(CrystalWindow windowHandle)
    {
        windowHandle.GetSize(out int width, out int height);

        string fpsText;

        double time = wnd.uptimeSeconds();
        double time_delta = time - last_time;
        last_time = time;

        sample_deltas[sample_index] = time_delta;
        sample_index = (sample_index + 1) % sample_deltas.Length;
        if (sample_count < sample_deltas.Length) sample_count++;

        double total_delta = 0;
        for (int i = 0; i < sample_count; i++)
        {
            total_delta += sample_deltas[i];
        }

        fpsText = $"{sample_count / total_delta:0.0} FPS";

        if (!pixelBacking || pixelBacking.width != width || pixelBacking.height != height)
        {
            if (pixelBacking)
            {
                pixelBacking.Dispose();
            }
            pixelBacking = CrystalSkia.net.FixedPixDataRenderer.CreateFixed(width, height, (canvas, info) => { });

            tileView = new TileView();
            
            float topHeaderHeight = 56f;
            float footerHeight = 36f;
            float availableWidth = Math.Max(100f, width - 40f);
            float availableHeight = Math.Max(100f, height - topHeaderHeight - footerHeight - 20f);
            float boardSize = Math.Min(availableWidth, availableHeight);

            tileView.size = boardSize;
            tileView.xx = (width - boardSize) / 2f;
            tileView.yy = topHeaderHeight + (availableHeight - boardSize) / 2f;
        }

        if (tileView == null) return;
        
        CrystalSkia.net.PixDataSkia.WithCanvasView(pixelBacking, (bitmap, canvas) =>
        {
            // Clear background
            canvas.DrawRect(new SKRect(0, 0, width, height), bgPaint);

            // Draw Board Container Background
            var boardRect = new SKRect(tileView.xx - 8, tileView.yy - 8, tileView.xx + tileView.size + 8, tileView.yy + tileView.size + 8);
            var boardRRect = new SKRoundRect(boardRect, 12, 12);
            canvas.DrawRoundRect(boardRRect, boardBgPaint);
            canvas.DrawRoundRect(boardRRect, boardBorderPaint);

            // Draw Static Grid Tiles
            for (int y = 0; y < MyGrid.Height; y++)
            {
                if (animationLockHoriz != null && animationLockHoriz.Value == false && animationOrdinal == y) continue; 
                
                for (int x = 0; x < MyGrid.Width; x++)
                {
                    if (animationLockHoriz != null && animationLockHoriz.Value == true && animationOrdinal == x) continue;
                    
                    SKImage image = MyGrid.Tiles[y, x].Image;

                    float _x = tileView.xx + (tileView.size / MyGrid.Width) * x;
                    float _y = tileView.yy + (tileView.size / MyGrid.Height) * y;

                    var dest = new SKRect(_x, _y, _x + tileView.size / MyGrid.Width, _y + tileView.size / MyGrid.Height);
                    var src = new SKRect(0, 0, image.Width, image.Height);
                    
                    canvas.DrawImage(image, src, dest, paint: tilePaint);
                }
            }

            // Draw Active Animated Row / Column
            if (animationLockHoriz != null)
            {
                bool horiz = animationLockHoriz.Value;
                if (!horiz)
                {
                    int y = animationOrdinal;
                    float rowTop = tileView.yy + (tileView.size / MyGrid.Height) * y;
                    float rowHeight = tileView.size / MyGrid.Height;
                    var clipRect = new SKRect(tileView.xx, rowTop, tileView.xx + tileView.size, rowTop + rowHeight);

                    canvas.Save();
                    canvas.ClipRect(clipRect);
                    
                    float offset = (float)(animationTileOffset % MyGrid.Width);
                    if (offset < 0) offset += MyGrid.Width;

                    for (int x = 0; x < MyGrid.Width; x++)
                    {
                        SKImage image = MyGrid.Tiles[y, x].Image;

                        float coord = x - offset;
                        float _x = tileView.xx + (tileView.size / MyGrid.Width) * coord;
                        float _y = rowTop;

                        var src = new SKRect(0, 0, image.Width, image.Height);

                        var dest = new SKRect(_x, _y, _x + tileView.size / MyGrid.Width, _y + rowHeight);
                        canvas.DrawImage(image, src, dest, paint: tilePaint);

                        float _xWrapped = _x + tileView.size;
                        var destWrapped = new SKRect(_xWrapped, _y, _xWrapped + tileView.size / MyGrid.Width, _y + rowHeight);
                        canvas.DrawImage(image, src, destWrapped, paint: tilePaint);
                    }
                    
                    canvas.DrawRoundRect(new SKRoundRect(new SKRect(tileView.xx, rowTop, tileView.xx + tileView.size, rowTop + rowHeight), 6, 6), activeLinePaint);
                    
                    canvas.Restore();
                }
                else
                {
                    int x = animationOrdinal;
                    float colLeft = tileView.xx + (tileView.size / MyGrid.Width) * x;
                    float colWidth = tileView.size / MyGrid.Width;
                    var clipRect = new SKRect(colLeft, tileView.yy, colLeft + colWidth, tileView.yy + tileView.size);

                    canvas.Save();
                    canvas.ClipRect(clipRect);
                    
                    float offset = (float)(animationTileOffset % MyGrid.Height);
                    if (offset < 0) offset += MyGrid.Height;

                    for (int y = 0; y < MyGrid.Height; y++)
                    {
                        SKImage image = MyGrid.Tiles[y, x].Image;

                        float coord = y - offset;
                        float _x = colLeft;
                        float _y = tileView.yy + (tileView.size / MyGrid.Height) * coord;

                        var src = new SKRect(0, 0, image.Width, image.Height);

                        var dest = new SKRect(_x, _y, _x + colWidth, _y + tileView.size / MyGrid.Height);
                        canvas.DrawImage(image, src, dest, paint: tilePaint);

                        float _yWrapped = _y + tileView.size;
                        var destWrapped = new SKRect(_x, _yWrapped, _x + colWidth, _yWrapped + tileView.size / MyGrid.Height);
                        canvas.DrawImage(image, src, destWrapped, paint: tilePaint);
                    }
                    
                    canvas.DrawRoundRect(new SKRoundRect(new SKRect(colLeft, tileView.yy, colLeft + colWidth, tileView.yy + tileView.size), 6, 6), activeLinePaint);

                    canvas.Restore();
                }
            }

            // Draw Top HUD Bar
            var hudRect = new SKRect(16, 10, width - 16, 50);
            var hudRRect = new SKRoundRect(hudRect, 8, 8);
            canvas.DrawRoundRect(hudRRect, hudBarPaint);

            // Left HUD: Grid & FPS
            string gridInfo = $"Grid: {MyGrid.Width}x{MyGrid.Height}";
            canvas.DrawText(gridInfo, 30, 35, hudBoldFont, hudTextPaint);
            canvas.DrawText(fpsText, 120, 35, hudFont, hudMutedPaint);

            // Center HUD: Moves & Time
            string movesText = $"Moves: {State.MoveCount}";
            string timeText = $"Time: {State.FormatTime(time)}";
            hudBoldFont.MeasureText(movesText, out SKRect movesBounds, hudTextPaint);
            hudBoldFont.MeasureText(timeText, out SKRect timeBounds, hudAccentPaint);
            
            float centerAreaStart = width / 2f - (movesBounds.Width + timeBounds.Width + 30) / 2f;
            canvas.DrawText(movesText, centerAreaStart, 35, hudBoldFont, hudTextPaint);
            canvas.DrawText(timeText, centerAreaStart + movesBounds.Width + 24, 35, hudBoldFont, hudAccentPaint);

            // Right HUD: Game Mode
            string modeText = State.Mode switch
            {
                GameMode.Ready => "READY",
                GameMode.Scrambling => "SCRAMBLING...",
                GameMode.InGame => "IN PLAY",
                GameMode.Solved => "SOLVED! 🎉",
                _ => ""
            };

            SKPaint statePaint = State.Mode switch
            {
                GameMode.Solved => bannerBorderPaint,
                GameMode.Scrambling => activeLinePaint,
                GameMode.InGame => hudAccentPaint,
                _ => hudMutedPaint
            };

            hudBoldFont.MeasureText(modeText, out SKRect modeBounds, statePaint);
            canvas.DrawText(modeText, width - 36 - modeBounds.Width, 35, hudBoldFont, statePaint);

            // Victory Banner Overlay
            if (State.Mode == GameMode.Solved)
            {
                float bannerW = Math.Min(500f, width - 60f);
                float bannerH = 120f;
                float bx = (width - bannerW) / 2f;
                float by = tileView.yy + (tileView.size - bannerH) / 2f;

                var vRect = new SKRect(bx, by, bx + bannerW, by + bannerH);
                var vRRect = new SKRoundRect(vRect, 16, 16);
                canvas.DrawRoundRect(vRRect, bannerBgPaint);
                canvas.DrawRoundRect(vRRect, bannerBorderPaint);

                string winTitle = "★ PUZZLE SOLVED! ★";
                bannerTitleFont.MeasureText(winTitle, out SKRect wtBounds, bannerTitlePaint);
                canvas.DrawText(winTitle, bx + (bannerW - wtBounds.Width) / 2f, by + 42, bannerTitleFont, bannerTitlePaint);

                string stats = $"Time: {State.FormatTime(time)}  •  Moves: {State.MoveCount}";
                bannerTextFont.MeasureText(stats, out SKRect sBounds, bannerTextPaint);
                canvas.DrawText(stats, bx + (bannerW - sBounds.Width) / 2f, by + 74, bannerTextFont, bannerTextPaint);

                string prompt = "Press [S] to scramble or [1-4] to change difficulty";
                footerFont.MeasureText(prompt, out SKRect pBounds, hudAccentPaint);
                canvas.DrawText(prompt, bx + (bannerW - pBounds.Width) / 2f, by + 100, footerFont, hudAccentPaint);
            }

            // Bottom Footer Shortcuts
            string footerText = "[S] Scramble  •  [R] Reset  •  [1] 3x3  [2] 4x4  [3] 6x6  [4] 8x8  •  [Esc] Cancel Drag";
            footerFont.MeasureText(footerText, out SKRect footerBounds, hudMutedPaint);
            canvas.DrawText(footerText, (width - footerBounds.Width) / 2f, height - 12, footerFont, hudMutedPaint);
        });
        
        windowHandle.PresentPix(ref pixelBacking);
    }

    public void Show(bool visible = true)
    {
        wnd.Show(visible);
    }
}
