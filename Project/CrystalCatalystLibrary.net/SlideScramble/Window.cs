using CrystalCatalystLibrary.net;
using CrystalOpenAL;
using SkiaSharp;

namespace SlideScramble;

public class Window
{
    public CrystalWindow wnd;
    public Grid MyGrid { get; protected set; }
    
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
        public float width;
        public float height;
    }
    
    public TileView? tileView;

    public GameState State { get; } = new GameState();
    public AudioEngine Audio { get; } = new AudioEngine();

    // Grid setup UI state
    public bool ShowGridSetup { get; set; } = false;
    private enum ActiveSlider { None, Columns, Rows }
    private ActiveSlider activeSlider = ActiveSlider.None;

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

    private readonly SKPaint panelBgPaint = new SKPaint
    {
        Color = new SKColor(30, 41, 59, 250), // Slate 800
        Style = SKPaintStyle.Fill,
        IsAntialias = true,
    };

    private readonly SKPaint panelBorderPaint = new SKPaint
    {
        Color = new SKColor(71, 85, 105), // Slate 600
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 2,
        IsAntialias = true,
    };

    private readonly SKPaint modalBackdropPaint = new SKPaint
    {
        Color = new SKColor(0, 0, 0, 120),
        Style = SKPaintStyle.Fill,
    };

    private readonly SKPaint sliderTrackPaint = new SKPaint
    {
        Color = new SKColor(51, 65, 85), // Slate 700
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 4,
        StrokeCap = SKStrokeCap.Round,
        IsAntialias = true,
    };

    private readonly SKPaint sliderFillPaint = new SKPaint
    {
        Color = new SKColor(56, 189, 248), // Sky 400
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 4,
        StrokeCap = SKStrokeCap.Round,
        IsAntialias = true,
    };

    private readonly SKPaint sliderThumbPaint = new SKPaint
    {
        Color = SKColors.White,
        Style = SKPaintStyle.Fill,
        IsAntialias = true,
    };

    private readonly SKPaint sliderThumbBorderPaint = new SKPaint
    {
        Color = new SKColor(56, 189, 248), // Sky 400
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 2,
        IsAntialias = true,
    };

    private readonly SKPaint hudChipPaint = new SKPaint
    {
        Color = new SKColor(51, 65, 85, 180),
        Style = SKPaintStyle.Fill,
        IsAntialias = true,
    };

    private readonly SKPaint hudChipBorderPaint = new SKPaint
    {
        Color = new SKColor(71, 85, 105),
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1.5f,
        IsAntialias = true,
    };

    private readonly SKFont hudFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal) ?? SKTypeface.Default, 14);
    private readonly SKFont hudBoldFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) ?? SKTypeface.Default, 15);
    private readonly SKFont bannerTitleFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) ?? SKTypeface.Default, 24);
    private readonly SKFont bannerTextFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal) ?? SKTypeface.Default, 14);
    private readonly SKFont footerFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal) ?? SKTypeface.Default, 12);
    private readonly SKFont panelTitleFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) ?? SKTypeface.Default, 17);

    public Window()
    {
        MyGrid = new Grid(4, 4);
        UpdateTileView(850, 700);

        wnd = CrystalWindow.Create(850, 700, "SlideScramble - CrystalCatalyst Showcase");
        wnd.ApplicationRetain();
        
        wnd.OnDraw = OnDraw;
        wnd.OnIdle = OnIdle;
        wnd.OnClose = (w) =>
        {
            Audio.Dispose();
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
            if (keycode == (int)KeyCode.Escape)
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
                    return;
                }

                if (ShowGridSetup)
                {
                    ShowGridSetup = false;
                    return;
                }

                return;
            }

            char c = (char)keycode;
            if ((c == 'g' || c == 'G'))
            {
                ShowGridSetup = !ShowGridSetup;
            }
            else if ((c == 's' || c == 'S') && State.Mode != GameMode.Scrambling && drag == null && Animations.Count == 0)
            {
                State.StartScrambling();
                ScrambleCount = (MyGrid.Width + MyGrid.Height) * 2;
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
                SetGridSize(3, 3);
            }
            else if (c == '2')
            {
                SetGridSize(4, 4);
            }
            else if (c == '3')
            {
                SetGridSize(6, 6);
            }
            else if (c == '4')
            {
                SetGridSize(8, 8);
            }
        };
    }

    public void UpdateTileView(int width = 0, int height = 0)
    {
        if (width <= 0 || height <= 0)
        {
            if (wnd != null)
            {
                wnd.GetSize(out width, out height);
            }
            else
            {
                width = 850;
                height = 700;
            }
        }

        if (tileView == null)
        {
            tileView = new TileView();
        }

        float topHeaderHeight = 56f;
        float footerHeight = 36f;
        float availableWidth = Math.Max(100f, width - 40f);
        float availableHeight = Math.Max(100f, height - topHeaderHeight - footerHeight - 20f);

        float tileSize = Math.Min(
            availableWidth / MyGrid.Width,
            availableHeight / MyGrid.Height);

        float boardWidth = tileSize * MyGrid.Width;
        float boardHeight = tileSize * MyGrid.Height;

        tileView.width = boardWidth;
        tileView.height = boardHeight;
        tileView.xx = (width - boardWidth) / 2f;
        tileView.yy = topHeaderHeight + (availableHeight - boardHeight) / 2f;
    }

    public void SetGridSize(int width, int height)
    {
        width = Math.Clamp(width, 2, 9);
        height = Math.Clamp(height, 2, 9);
        if (State.Mode != GameMode.Scrambling && drag == null && Animations.Count == 0)
        {
            MyGrid = new Grid(width, height);
            State.Reset();
            animationLockHoriz = null;
            animationTileOffset = 0;
            UpdateTileView();
        }
    }

    public void SetGridSize(int size) => SetGridSize(size, size);

    private static void DrawDropdownArrow(SKCanvas canvas, float x, float y, float size, SKPaint paint)
    {
        using var path = new SKPath();
        path.MoveTo(x - size, y - size * 0.5f);
        path.LineTo(x + size, y - size * 0.5f);
        path.LineTo(x, y + size * 0.7f);
        path.Close();
        canvas.DrawPath(path, paint);
    }

    private static void DrawStar(SKCanvas canvas, float cx, float cy, float outerRadius, float innerRadius, SKPaint fillPaint)
    {
        using var path = new SKPath();
        double angleStep = Math.PI / 5.0;
        double startAngle = -Math.PI / 2.0;

        for (int i = 0; i < 10; i++)
        {
            double angle = startAngle + i * angleStep;
            float r = (i % 2 == 0) ? outerRadius : innerRadius;
            float x = cx + (float)(r * Math.Cos(angle));
            float y = cy + (float)(r * Math.Sin(angle));
            if (i == 0)
                path.MoveTo(x, y);
            else
                path.LineTo(x, y);
        }
        path.Close();
        canvas.DrawPath(path, fillPaint);
    }

    private static void DrawCloseCross(SKCanvas canvas, float cx, float cy, float size, SKPaint strokePaint)
    {
        canvas.DrawLine(cx - size, cy - size, cx + size, cy + size, strokePaint);
        canvas.DrawLine(cx - size, cy + size, cx + size, cy - size, strokePaint);
    }

    private static int CalculateSliderValue(float mouseX, float trackStartX, float trackEndX)
    {
        float t = (mouseX - trackStartX) / (trackEndX - trackStartX);
        int val = (int)Math.Round(2 + t * 7);
        return Math.Clamp(val, 2, 9);
    }

    private void OnMouseMove(CrystalWindow windowHandle, int x, int y)
    {
        if (activeSlider != ActiveSlider.None)
        {
            windowHandle.GetSize(out int width, out int height);
            float panelWidth = 360f;
            float panelX = (width - panelWidth) / 2f;
            float trackStartX = panelX + 130;
            float trackEndX = panelX + 270;

            if (activeSlider == ActiveSlider.Columns)
            {
                int newCols = CalculateSliderValue(x, trackStartX, trackEndX);
                if (newCols != MyGrid.Width)
                {
                    SetGridSize(newCols, MyGrid.Height);
                }
            }
            else if (activeSlider == ActiveSlider.Rows)
            {
                int newRows = CalculateSliderValue(x, trackStartX, trackEndX);
                if (newRows != MyGrid.Height)
                {
                    SetGridSize(MyGrid.Width, newRows);
                }
            }
            return;
        }

        if (drag != null && tileView != null)
        {
            int xd = x - drag.startX;
            int yd = y - drag.startY;
            
            if (animationLockHoriz == null)
            {
                if (Math.Abs(xd) < Math.Abs(yd))
                {
                    // Vertical drag -> column move
                    animationLockHoriz = true;
                    int rawOrdinal = (int)((x - tileView.xx) * MyGrid.Width / tileView.width);
                    animationOrdinal = Math.Clamp(rawOrdinal, 0, MyGrid.Width - 1);
                }
                else
                {
                    // Horizontal drag -> row move
                    animationLockHoriz = false;
                    int rawOrdinal = (int)((y - tileView.yy) * MyGrid.Height / tileView.height);
                    animationOrdinal = Math.Clamp(rawOrdinal, 0, MyGrid.Height - 1);
                }
            }
                
            if (animationLockHoriz == false)
            {
                animationTileOffset = -xd * MyGrid.Width / tileView.width;
            }
            else if (animationLockHoriz == true)
            {
                animationTileOffset = -yd * MyGrid.Height / tileView.height;
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
                    if (State.CheckSolved(MyGrid, wnd.uptimeSeconds()))
                    {
                        Audio.PlayVictoryChime();
                    }
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
            if (activeSlider != ActiveSlider.None)
            {
                activeSlider = ActiveSlider.None;
                return;
            }

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
        if (button != (int)CrystalMouseButton.Left) return;

        windowHandle.GetSize(out int width, out int height);

        // Check HUD Grid button hotspot
        var hudGridButtonRect = new SKRect(20, 10, 125, 50);
        if (hudGridButtonRect.Contains(x, y))
        {
            ShowGridSetup = !ShowGridSetup;
            return;
        }

        // Handle Setup Overlay interactions
        if (ShowGridSetup)
        {
            float panelWidth = 360f;
            float panelHeight = 220f;
            float panelX = (width - panelWidth) / 2f;
            float panelY = (height - panelHeight) / 2f;

            var panelRect = new SKRect(panelX, panelY, panelX + panelWidth, panelY + panelHeight);
            var closeRect = new SKRect(panelX + panelWidth - 36, panelY + 10, panelX + panelWidth - 8, panelY + 38);

            float trackStartX = panelX + 130;
            float trackEndX = panelX + 270;
            var colsHitRect = new SKRect(panelX + 20, panelY + 55, panelX + panelWidth - 20, panelY + 95);
            var rowsHitRect = new SKRect(panelX + 20, panelY + 105, panelX + panelWidth - 20, panelY + 145);

            if (closeRect.Contains(x, y))
            {
                ShowGridSetup = false;
                return;
            }
            else if (colsHitRect.Contains(x, y))
            {
                activeSlider = ActiveSlider.Columns;
                int newCols = CalculateSliderValue(x, trackStartX, trackEndX);
                if (newCols != MyGrid.Width)
                {
                    SetGridSize(newCols, MyGrid.Height);
                }
                return;
            }
            else if (rowsHitRect.Contains(x, y))
            {
                activeSlider = ActiveSlider.Rows;
                int newRows = CalculateSliderValue(x, trackStartX, trackEndX);
                if (newRows != MyGrid.Height)
                {
                    SetGridSize(MyGrid.Width, newRows);
                }
                return;
            }
            else if (panelRect.Contains(x, y))
            {
                // Click inside panel consumed
                return;
            }
            else
            {
                // Click outside panel closes it
                ShowGridSetup = false;
                return;
            }
        }

        // Board dragging hit test
        if (State.Mode != GameMode.Scrambling && tileView != null && Animations.Count == 0 && drag == null)
        {
            // Check if mouse is inside the board bounds
            if (x >= tileView.xx && x <= tileView.xx + tileView.width &&
                y >= tileView.yy && y <= tileView.yy + tileView.height)
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
                Audio.PlayGameStartChime();
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
        }

        UpdateTileView(width, height);

        if (tileView == null) return;
        
        CrystalSkia.net.PixDataSkia.WithCanvasView(pixelBacking, (bitmap, canvas) =>
        {
            // Clear background
            canvas.DrawRect(new SKRect(0, 0, width, height), bgPaint);

            // Draw Board Container Background
            var boardRect = new SKRect(tileView.xx - 8, tileView.yy - 8, tileView.xx + tileView.width + 8, tileView.yy + tileView.height + 8);
            var boardRRect = new SKRoundRect(boardRect, 12, 12);
            canvas.DrawRoundRect(boardRRect, boardBgPaint);
            canvas.DrawRoundRect(boardRRect, boardBorderPaint);

            float tileW = tileView.width / MyGrid.Width;
            float tileH = tileView.height / MyGrid.Height;

            // Draw Static Grid Tiles
            for (int y = 0; y < MyGrid.Height; y++)
            {
                if (animationLockHoriz != null && animationLockHoriz.Value == false && animationOrdinal == y) continue; 
                
                for (int x = 0; x < MyGrid.Width; x++)
                {
                    if (animationLockHoriz != null && animationLockHoriz.Value == true && animationOrdinal == x) continue;
                    
                    SKImage image = MyGrid.Tiles[y, x].Image;

                    float _x = tileView.xx + tileW * x;
                    float _y = tileView.yy + tileH * y;

                    var dest = new SKRect(_x, _y, _x + tileW, _y + tileH);
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
                    // Active row moving horizontally
                    int y = animationOrdinal;
                    float rowTop = tileView.yy + tileH * y;
                    float rowHeight = tileH;
                    var clipRect = new SKRect(tileView.xx, rowTop, tileView.xx + tileView.width, rowTop + rowHeight);

                    canvas.Save();
                    canvas.ClipRect(clipRect);
                    
                    float offset = (float)(animationTileOffset % MyGrid.Width);
                    if (offset < 0) offset += MyGrid.Width;

                    for (int x = 0; x < MyGrid.Width; x++)
                    {
                        SKImage image = MyGrid.Tiles[y, x].Image;

                        float coord = x - offset;
                        float _x = tileView.xx + tileW * coord;
                        float _y = rowTop;

                        var src = new SKRect(0, 0, image.Width, image.Height);

                        var dest = new SKRect(_x, _y, _x + tileW, _y + rowHeight);
                        canvas.DrawImage(image, src, dest, paint: tilePaint);

                        float _xWrapped = _x + tileView.width;
                        var destWrapped = new SKRect(_xWrapped, _y, _xWrapped + tileW, _y + rowHeight);
                        canvas.DrawImage(image, src, destWrapped, paint: tilePaint);
                    }
                    
                    canvas.DrawRoundRect(new SKRoundRect(new SKRect(tileView.xx, rowTop, tileView.xx + tileView.width, rowTop + rowHeight), 6, 6), activeLinePaint);
                    
                    canvas.Restore();
                }
                else
                {
                    // Active column moving vertically
                    int x = animationOrdinal;
                    float colLeft = tileView.xx + tileW * x;
                    float colWidth = tileW;
                    var clipRect = new SKRect(colLeft, tileView.yy, colLeft + colWidth, tileView.yy + tileView.height);

                    canvas.Save();
                    canvas.ClipRect(clipRect);
                    
                    float offset = (float)(animationTileOffset % MyGrid.Height);
                    if (offset < 0) offset += MyGrid.Height;

                    for (int y = 0; y < MyGrid.Height; y++)
                    {
                        SKImage image = MyGrid.Tiles[y, x].Image;

                        float coord = y - offset;
                        float _x = colLeft;
                        float _y = tileView.yy + tileH * coord;

                        var src = new SKRect(0, 0, image.Width, image.Height);

                        var dest = new SKRect(_x, _y, _x + colWidth, _y + tileH);
                        canvas.DrawImage(image, src, dest, paint: tilePaint);

                        float _yWrapped = _y + tileView.height;
                        var destWrapped = new SKRect(_x, _yWrapped, _x + colWidth, _yWrapped + tileH);
                        canvas.DrawImage(image, src, destWrapped, paint: tilePaint);
                    }
                    
                    canvas.DrawRoundRect(new SKRoundRect(new SKRect(colLeft, tileView.yy, colLeft + colWidth, tileView.yy + tileView.height), 6, 6), activeLinePaint);

                    canvas.Restore();
                }
            }

            // Draw Top HUD Bar
            var hudRect = new SKRect(16, 10, width - 16, 50);
            var hudRRect = new SKRoundRect(hudRect, 8, 8);
            canvas.DrawRoundRect(hudRRect, hudBarPaint);

            // Left HUD: Clickable Grid Button & FPS
            var gridBtnRect = new SKRect(22, 14, 120, 46);
            var gridBtnRRect = new SKRoundRect(gridBtnRect, 6, 6);
            canvas.DrawRoundRect(gridBtnRRect, hudChipPaint);
            canvas.DrawRoundRect(gridBtnRRect, hudChipBorderPaint);

            string gridInfo = $"Grid: {MyGrid.Width}x{MyGrid.Height}";
            hudBoldFont.MeasureText(gridInfo, out SKRect gridBounds, hudAccentPaint);
            canvas.DrawText(gridInfo, 28, 35, hudBoldFont, hudAccentPaint);
            DrawDropdownArrow(canvas, 28 + gridBounds.Width + 8, 30, 4, hudAccentPaint);
            canvas.DrawText(fpsText, 132, 35, hudFont, hudMutedPaint);

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
                GameMode.Solved => "SOLVED!",
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
            float modeTextX = width - 36 - modeBounds.Width;
            canvas.DrawText(modeText, modeTextX, 35, hudBoldFont, statePaint);
            if (State.Mode == GameMode.Solved)
            {
                DrawStar(canvas, modeTextX - 12, 30, 5.5f, 2.5f, bannerBorderPaint);
            }

            // Victory Banner Overlay
            if (State.Mode == GameMode.Solved)
            {
                float bannerW = Math.Min(500f, width - 60f);
                float bannerH = 120f;
                float bx = (width - bannerW) / 2f;
                float by = tileView.yy + (tileView.height - bannerH) / 2f;

                var vRect = new SKRect(bx, by, bx + bannerW, by + bannerH);
                var vRRect = new SKRoundRect(vRect, 16, 16);
                canvas.DrawRoundRect(vRRect, bannerBgPaint);
                canvas.DrawRoundRect(vRRect, bannerBorderPaint);

                string winTitle = "PUZZLE SOLVED!";
                bannerTitleFont.MeasureText(winTitle, out SKRect wtBounds, bannerTitlePaint);
                float titleX = bx + (bannerW - wtBounds.Width) / 2f;
                canvas.DrawText(winTitle, titleX, by + 42, bannerTitleFont, bannerTitlePaint);
                DrawStar(canvas, titleX - 22, by + 34, 9, 4f, bannerTitlePaint);
                DrawStar(canvas, titleX + wtBounds.Width + 22, by + 34, 9, 4f, bannerTitlePaint);

                string stats = $"Time: {State.FormatTime(time)}  •  Moves: {State.MoveCount}";
                bannerTextFont.MeasureText(stats, out SKRect sBounds, bannerTextPaint);
                canvas.DrawText(stats, bx + (bannerW - sBounds.Width) / 2f, by + 74, bannerTextFont, bannerTextPaint);

                string prompt = "Press [S] to scramble or [G] to change grid size";
                footerFont.MeasureText(prompt, out SKRect pBounds, hudAccentPaint);
                canvas.DrawText(prompt, bx + (bannerW - pBounds.Width) / 2f, by + 100, footerFont, hudAccentPaint);
            }

            // Grid Setup Overlay Panel
            if (ShowGridSetup)
            {
                // Semi-transparent backdrop scrim
                canvas.DrawRect(new SKRect(0, 0, width, height), modalBackdropPaint);

                float panelWidth = 360f;
                float panelHeight = 220f;
                float panelX = (width - panelWidth) / 2f;
                float panelY = (height - panelHeight) / 2f;

                var panelRect = new SKRect(panelX, panelY, panelX + panelWidth, panelY + panelHeight);
                var panelRRect = new SKRoundRect(panelRect, 14, 14);
                canvas.DrawRoundRect(panelRRect, panelBgPaint);
                canvas.DrawRoundRect(panelRRect, panelBorderPaint);

                // Header Title
                string title = "Grid Setup";
                canvas.DrawText(title, panelX + 24, panelY + 36, panelTitleFont, hudTextPaint);

                // Close button icon
                using var closeIconPaint = new SKPaint
                {
                    Color = new SKColor(148, 163, 184),
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 2f,
                    StrokeCap = SKStrokeCap.Round,
                    IsAntialias = true
                };
                DrawCloseCross(canvas, panelX + panelWidth - 22, panelY + 24, 5.5f, closeIconPaint);

                float trackStartX = panelX + 130;
                float trackEndX = panelX + 270;

                // Row 1: Columns slider
                float row1Y = panelY + 80;
                canvas.DrawText("Columns", panelX + 24, row1Y + 5, hudBoldFont, hudTextPaint);
                canvas.DrawText("2", panelX + 112, row1Y + 5, hudFont, hudMutedPaint);
                canvas.DrawLine(trackStartX, row1Y, trackEndX, row1Y, sliderTrackPaint);
                
                float colT = (MyGrid.Width - 2) / 7f;
                float colThumbX = trackStartX + colT * (trackEndX - trackStartX);
                if (colT > 0.001f)
                {
                    canvas.DrawLine(trackStartX, row1Y, colThumbX, row1Y, sliderFillPaint);
                }
                canvas.DrawCircle(colThumbX, row1Y, 8, sliderThumbPaint);
                canvas.DrawCircle(colThumbX, row1Y, 8, sliderThumbBorderPaint);
                
                canvas.DrawText("9", panelX + 280, row1Y + 5, hudFont, hudMutedPaint);
                string colValText = $"{MyGrid.Width}";
                canvas.DrawText(colValText, panelX + 310, row1Y + 5, hudBoldFont, hudAccentPaint);

                // Row 2: Rows slider
                float row2Y = panelY + 130;
                canvas.DrawText("Rows", panelX + 24, row2Y + 5, hudBoldFont, hudTextPaint);
                canvas.DrawText("2", panelX + 112, row2Y + 5, hudFont, hudMutedPaint);
                canvas.DrawLine(trackStartX, row2Y, trackEndX, row2Y, sliderTrackPaint);

                float rowT = (MyGrid.Height - 2) / 7f;
                float rowThumbX = trackStartX + rowT * (trackEndX - trackStartX);
                if (rowT > 0.001f)
                {
                    canvas.DrawLine(trackStartX, row2Y, rowThumbX, row2Y, sliderFillPaint);
                }
                canvas.DrawCircle(rowThumbX, row2Y, 8, sliderThumbPaint);
                canvas.DrawCircle(rowThumbX, row2Y, 8, sliderThumbBorderPaint);

                canvas.DrawText("9", panelX + 280, row2Y + 5, hudFont, hudMutedPaint);
                string rowValText = $"{MyGrid.Height}";
                canvas.DrawText(rowValText, panelX + 310, row2Y + 5, hudBoldFont, hudAccentPaint);

                // Dimension Summary Pill
                string dimSummary = $"{MyGrid.Width} x {MyGrid.Height}";
                hudBoldFont.MeasureText(dimSummary, out SKRect dimBounds, hudAccentPaint);
                var badgeRect = new SKRect(panelX + (panelWidth - dimBounds.Width) / 2f - 16, panelY + 158, panelX + (panelWidth + dimBounds.Width) / 2f + 16, panelY + 184);
                canvas.DrawRoundRect(new SKRoundRect(badgeRect, 6, 6), hudChipPaint);
                canvas.DrawRoundRect(new SKRoundRect(badgeRect, 6, 6), hudChipBorderPaint);
                canvas.DrawText(dimSummary, panelX + (panelWidth - dimBounds.Width) / 2f, panelY + 177, hudBoldFont, hudAccentPaint);

                string hint = "[G] or [Esc] to Close";
                footerFont.MeasureText(hint, out SKRect hintBounds, hudMutedPaint);
                canvas.DrawText(hint, panelX + (panelWidth - hintBounds.Width) / 2f, panelY + 204, footerFont, hudMutedPaint);
            }

            // Bottom Footer Shortcuts
            string footerText = "[S] Scramble  •  [R] Reset  •  [G] Grid Setup  •  [1-4] Presets  •  [Esc] Cancel";
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
