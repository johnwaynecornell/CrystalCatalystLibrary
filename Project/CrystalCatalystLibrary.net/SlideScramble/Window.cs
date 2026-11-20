using CrystalCatalystLibrary.net;
using CrystalOpenAL;
using CrystalOpenGL;
using CrystalSkia.net;
using Silk.NET.OpenGL;
using SkiaSharp;

namespace SlideScramble;

public class Window
{
    /// <summary>
    /// Set to true to use OpenGL texture presentation instead of the legacy PresentPix path.
    /// When enabled, the window is created with an OpenGL context and the Skia-rendered
    /// PixData is uploaded to a GL texture each frame.
    /// </summary>
    public const bool UseOpenGLPresentation = true;

    /// <summary>
    /// Set to true to show frame diagnostics (FPS, upload time, draw time) on the HUD.
    /// </summary>
    public const bool ShowDiagnostics = false;

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
        public int cellX;
        public int cellY;
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
    private readonly CrystalInteraction interaction = new CrystalInteraction();
    private readonly CrystalTextEditor filenameEditor;
    private string filenameBeforeEdit = "session";
    private string sessionMessage = "";
    private SessionRecordingController sessions;
    private SessionRecording? savedSession;
    private double lastPlaybackTick;
    private (int X, int Y)? moveHighlight;
    private static readonly string[] mediaLabels = { "Rec", "Save", "Load", "|<", "<", ">", "Play", "Stop" };
    private bool showMedia;

    // Popup selection integration
    private readonly CrystalPopupHost popupHost = new();
    private enum SelectionKind { None, Session, Challenge }
    private SelectionKind selectionKind;
    private CrystalSelectionControl? selectionControl;
    private ChallengeCatalog? challengeCatalog;
    private Grid? previewGrid;
    private string selectionMessage = "";
    private SKRect previewArea;

    // OpenGL presentation
    private GL? _gl;
    private GLPixDataPresenter? _glPresenter;
    private bool _glAvailable = false;
    private string? _glError;

    private class MediaLayout
    {
        public float X, Y, Width, Height, Scale;
        public int Columns;

        public SKRect Button(int index)
        {
            float buttonWidth = (Width - 24) / Columns;
            float left = 12 + index % Columns * buttonWidth;
            float top = 68 + index / Columns * 30;
            return new SKRect(X + left * Scale, Y + top * Scale,
                X + (left + buttonWidth - 3) * Scale, Y + (top + 25) * Scale);
        }
    }

    private MediaLayout GetMediaLayout(int width, int height)
    {
        float w = Math.Max(1, width), h = Math.Max(1, height);
        float top = Math.Min(58, h / 3), bottom = Math.Max(top + 1, h - Math.Min(36, h / 3));
        float left = Math.Clamp(tileView!.xx - 8, 0, w);
        float right = Math.Clamp(tileView.xx + tileView.width + 8, 0, w);
        float boardTop = Math.Clamp(tileView.yy - 8, top, bottom);
        float boardBottom = Math.Clamp(tileView.yy + tileView.height + 8, top, bottom);
        SKRect[] gutters = {
            new SKRect(0, top, left, bottom), new SKRect(right, top, w, bottom),
            new SKRect(0, top, w, boardTop), new SKRect(0, boardBottom, w, bottom)
        };
        SKRect gutter = gutters.OrderByDescending(r => r.Width * r.Height).First();
        float panelWidth = Math.Min(320, Math.Max(40, w - 16));
        if (gutter.Width >= 160) panelWidth = Math.Min(panelWidth, gutter.Width - 8);
        int columns = Math.Clamp((int)((panelWidth - 24) / 65), 1, mediaLabels.Length);
        float panelHeight = 68 + (float)Math.Ceiling((double)mediaLabels.Length / columns) * 30 + 34;
        float scale = Math.Min(1, Math.Min(w / panelWidth, Math.Max(1, bottom - top) / panelHeight));
        float x = Math.Clamp(gutter.MidX - panelWidth * scale / 2, 0, Math.Max(0, w - panelWidth * scale));
        float y = Math.Clamp(gutter.MidY - panelHeight * scale / 2, top, Math.Max(top, bottom - panelHeight * scale));
        return new MediaLayout { X = x, Y = y, Width = panelWidth, Height = panelHeight, Scale = scale, Columns = columns };
    }

    private void UpdateMediaEditorBounds(MediaLayout layout)
    {
        editorFont.Size = hudFont.Size * layout.Scale;
        filenameEditor.Bounds = new System.Drawing.RectangleF(layout.X + 12 * layout.Scale, layout.Y + 34 * layout.Scale,
            Math.Max(1, layout.Width - 24) * layout.Scale, 24 * layout.Scale);
    }

    private void DrawMedia(SKCanvas canvas, MediaLayout layout)
    {
        canvas.Save();
        canvas.Translate(layout.X, layout.Y);
        canvas.Scale(layout.Scale);
        var panel = new SKRoundRect(new SKRect(0, 0, layout.Width, layout.Height), 8, 8);
        canvas.DrawRoundRect(panel, hudBarPaint);
        canvas.DrawRoundRect(panel, hudChipBorderPaint);
        canvas.DrawText("Session  [R] close", 12, 23, hudBoldFont, hudTextPaint);
        for (int i = 0; i < mediaLabels.Length; i++)
        {
            var screenRect = layout.Button(i);
            var buttonRect = new SKRect((screenRect.Left - layout.X) / layout.Scale, (screenRect.Top - layout.Y) / layout.Scale,
                (screenRect.Right - layout.X) / layout.Scale, (screenRect.Bottom - layout.Y) / layout.Scale);
            canvas.DrawRoundRect(new SKRoundRect(buttonRect, 4, 4), hudChipPaint);
            string label = i == 0 && sessions.Mode == RecordingControllerMode.Recording ? "Stop Rec" :
                i == 6 && sessions.IsAutoplaying ? "Pause" : mediaLabels[i];
            canvas.Save();
            canvas.ClipRect(buttonRect);
            canvas.DrawText(label, buttonRect.MidX - footerFont.MeasureText(label) / 2, buttonRect.Top + 17, footerFont, hudTextPaint);
            canvas.Restore();
        }
        canvas.Save();
        canvas.ClipRect(new SKRect(8, layout.Height - 30, layout.Width - 8, layout.Height));
        canvas.DrawText($"{sessions.Mode}  {sessions.Position}/{sessions.MoveCount}", 12, layout.Height - 18, footerFont, hudMutedPaint);
        canvas.DrawText(sessionMessage, 12, layout.Height - 4, footerFont, hudMutedPaint);
        canvas.Restore();
        canvas.Restore();
        if ((ICrystal)filenameEditor is ICrystalSkia drawable) drawable.OnDraw(canvas);
    }

    private string SessionPath()
    {
        string name = filenameEditor.Text.Trim();
        if (name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) name = name[..^5];
        if (name.Length == 0 || name.Length > 100 || name == "." || name == ".." ||
            name.Any(c => char.IsControl(c) || "<>:\"/\\|?*".Contains(c)) || name.EndsWith('.'))
            throw new ArgumentException("Enter a valid filename without a path.");
        return Path.Combine(AppContext.BaseDirectory, "sessions", name + ".json");
    }

    private void MediaAction(int index)
    {
        if (index != 2) State.TakeBackSolved(wnd.uptimeSeconds());

        if (drag != null || Animations.Count != 0 || State.Mode == GameMode.Scrambling)
        {
            sessionMessage = "Finish the current move first";
            return;
        }
        try
        {
            switch (index)
            {
                case 0:
                    if (sessions.Mode == RecordingControllerMode.Recording)
                    {
                        savedSession = sessions.StopRecording();
                        sessionMessage = "Recording stopped";
                    }
                    else if (sessions.Mode == RecordingControllerMode.Idle)
                    {
                        sessions.StartRecording();
                        sessionMessage = "Recording";
                    }
                    break;
                case 1:
                    if (sessions.Mode == RecordingControllerMode.Recording) throw new InvalidOperationException("Stop recording before saving.");
                    var toSave = savedSession ?? sessions.Playback ?? throw new InvalidOperationException("No session to save.");
                    string savePath = SessionPath();
                    Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
                    File.WriteAllText(savePath, toSave.ToJson());
                    sessionMessage = "Session saved";
                    break;
                case 2:
                    if (sessions.Mode == RecordingControllerMode.Recording) throw new InvalidOperationException("Stop recording before loading.");
                    ShowSessionSelectionPopup();
                    break;
                case 3:
                    if (sessions.Mode == RecordingControllerMode.Idle) sessions.ResumePlayback();
                    sessions.ResetPlayback();
                    moveHighlight = null;
                    break;
                case 4:
                    if (sessions.Mode == RecordingControllerMode.Idle) sessions.ResumePlayback();
                    QueuePlaybackAnimation(false);
                    break;
                case 5:
                    if (sessions.Mode == RecordingControllerMode.Idle) sessions.ResumePlayback();
                    sessions.SetAutoplay(false);
                    QueuePlaybackAnimation(true);
                    break;
                case 6:
                    if (sessions.Mode == RecordingControllerMode.Idle) sessions.ResumePlayback();
                    sessions.SetAutoplay(!sessions.IsAutoplaying);
                    lastPlaybackTick = wnd.uptimeSeconds();
                    break;
                case 7:
                    if (sessions.Mode == RecordingControllerMode.Playback) sessions.StopPlayback();
                    moveHighlight = null;
                    break;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or System.Text.Json.JsonException or NotSupportedException)
        {
            sessionMessage = ex.Message;
        }
    }

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

    private readonly CrystalSkia.net.CrystalFontFileFallback fontFallback = new CrystalSkia.net.CrystalFontFileFallback(new[]
    {
        Path.Combine(AppContext.BaseDirectory, "DejaVuSans.ttf"),
        "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
        "/usr/share/fonts/truetype/liberation2/LiberationSans-Regular.ttf",
        @"C:\Windows\Fonts\arial.ttf",
        "/System/Library/Fonts/Supplemental/Arial.ttf"
    });
    private readonly SKFont hudFont;
    private readonly SKFont editorFont;
    private readonly CrystalSkia.net.AnsiSkiaRenderer editorFontResolver = new();
    private readonly SKFont hudBoldFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) ?? SKTypeface.Default, 15);
    private readonly SKFont bannerTitleFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) ?? SKTypeface.Default, 24);
    private readonly SKFont bannerTextFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal) ?? SKTypeface.Default, 14);
    private readonly SKFont footerFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal) ?? SKTypeface.Default, 12);
    private readonly SKFont panelTitleFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) ?? SKTypeface.Default, 17);

    public Window()
    {
        hudFont = new SKFont(fontFallback.FindTypeface("SlideScramble 0123456789") ?? SKTypeface.Default, 14);
        editorFont = new SKFont(hudFont.Typeface, hudFont.Size);
        editorFontResolver.FontFileFallback = fontFallback;
        filenameEditor = new CrystalTextEditor(new System.Drawing.RectangleF(24, 58, 300, 24), editorFont);
        filenameEditor.TypefaceForGlyph = editorFontResolver.TypefaceForGlyph;
        filenameEditor.TextColor = System.Drawing.Color.FromArgb(hudTextPaint.Color.Alpha, hudTextPaint.Color.Red, hudTextPaint.Color.Green, hudTextPaint.Color.Blue);
        filenameEditor.BackgroundColor = System.Drawing.Color.FromArgb(hudChipPaint.Color.Alpha, hudChipPaint.Color.Red, hudChipPaint.Color.Green, hudChipPaint.Color.Blue);
        filenameEditor.AccentColor = System.Drawing.Color.FromArgb(hudAccentPaint.Color.Alpha, hudAccentPaint.Color.Red, hudAccentPaint.Color.Green, hudAccentPaint.Color.Blue);
        filenameEditor.Text = "session";
        interaction.Add(filenameEditor);
        interaction.Add(popupHost);
        MyGrid = new Grid(4, 4);
        sessions = new SessionRecordingController(MyGrid);
        UpdateTileView(850, 700);

        // Wire popup closed event
        popupHost.Closed += OnPopupClosed;

        wnd = CrystalWindow.Create(850, 700, "SlideScramble - CrystalCatalyst Showcase");
        wnd.ApplicationRetain();

        // Initialize OpenGL context if using GL presentation
        if (UseOpenGLPresentation)
        {
            try
            {
                bool success = wnd.GLInitVersioned(3, 3);
                if (success)
                {
                    wnd.GLGetVersion(out int major, out int minor);
                    _gl = GL.GetApi(wnd.GLGetProcAddress);
                    _glAvailable = true;
                    Console.WriteLine($"[GL] OpenGL {major}.{minor} context created successfully.");
                }
                else
                {
                    _glError = "GLInitVersioned(3,3) failed";
                    Console.WriteLine($"[GL] Warning: {_glError} — falling back to legacy presentation.");
                }
            }
            catch (Exception ex)
            {
                _glError = ex.Message;
                Console.WriteLine($"[GL] Warning: {_glError} — falling back to legacy presentation.");
            }
        }
        
        wnd.OnDraw = OnDraw;
        wnd.OnIdle = OnIdle;
        wnd.OnResize = OnResize;
        wnd.OnClose = (w) =>
        {
            // Clean up GL resources while context is still available
            if (_glAvailable && _glPresenter != null)
            {
                try
                {
                    w.GLMakeCurrent();
                    _glPresenter.Dispose();
                }
                catch { /* Ignore cleanup errors */ }
            }
            _glPresenter = null;
            _gl = null;

            Audio.Dispose();
            editorFont.Dispose();
            foreach (var typeface in editorFontResolver.TypefaceCache.Values) typeface.Dispose();
            hudFont.Dispose();
            fontFallback.Dispose();
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
            // Route to popup when visible
            if (popupHost.IsVisible)
            {
                // Printable characters must become TextInput, not KeyDown
                if (keycode >= 32 && keycode <= 126)
                {
                    popupHost.TextInput(((char)keycode).ToString());
                }
                else
                {
                    popupHost.KeyDown((KeyCode)keycode, false, false);
                }
                return;
            }
            if (filenameEditor.IsFocused)
            {
                if (keycode == (int)KeyCode.Escape || keycode == (int)KeyCode.Enter)
                {
                    if (keycode == (int)KeyCode.Escape) filenameEditor.Text = filenameBeforeEdit;
                    interaction.SetFocus(null);
                }
                else if (keycode >= 32 && keycode <= 126)
                    interaction.TextInput(((char)keycode).ToString());
                else
                    interaction.KeyDown((KeyCode)keycode);
                return;
            }
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
                        moveHighlight = null;
                    }
                    return;
                }

                if (ShowGridSetup)
                {
                    ShowGridSetup = false;
                    return;
                }

                if (showMedia)
                {
                    showMedia = false;
                    return;
                }

                return;
            }

            char c = (char)keycode;
            if ((c == 'g' || c == 'G'))
            {
                ShowGridSetup = !ShowGridSetup;
            }
            else if ((c == 's' || c == 'S') && sessions.Mode == RecordingControllerMode.Idle && State.Mode != GameMode.Scrambling && drag == null && Animations.Count == 0)
            {
                State.StartScrambling();
                ScrambleCount = (MyGrid.Width + MyGrid.Height) * 2;
            }
            else if (c == 'r' || c == 'R')
            {
                showMedia = !showMedia;
            }
            else if (c == 'c' || c == 'C')
            {
                ShowSelectionPopup(SelectionKind.Challenge);
            }
            else if ((c == 'x' || c == 'X') && sessions.Mode == RecordingControllerMode.Idle && State.Mode != GameMode.Scrambling && drag == null && Animations.Count == 0)
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

        float topHeaderHeight = 62f;
        float footerHeight = 36f;
        float availableWidth = Math.Max(1f, width - 40f);
        float availableHeight = Math.Max(1f, height - topHeaderHeight - footerHeight - 20f);

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
        if (sessions.Mode == RecordingControllerMode.Idle && State.Mode != GameMode.Scrambling && drag == null && Animations.Count == 0)
        {
            MyGrid = new Grid(width, height);
            sessions = new SessionRecordingController(MyGrid);
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
        if (popupHost.IsVisible)
        {
            popupHost.PointerMove(new System.Drawing.PointF(x, y));
            return;
        }
        interaction.PointerMove(x, y);
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
                    int rawOrdinal = drag.cellX;
                    animationOrdinal = Math.Clamp(rawOrdinal, 0, MyGrid.Width - 1);
                }
                else
                {
                    // Horizontal drag -> row move
                    animationLockHoriz = false;
                    int rawOrdinal = drag.cellY;
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

    private void QueueSnapAnimation(bool horizontal, int ordinal, float startOffset, float targetOffset, int delta, int cellX = 0, int cellY = 0)
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
                moveHighlight = null;
                if (delta != 0)
                {
                    bool committed;
                    if (!horizontal)
                    {
                        committed = sessions.MoveRow(ordinal, delta, cellX, cellY);
                    }
                    else
                    {
                        committed = sessions.MoveColumn(ordinal, delta, cellX, cellY);
                    }
                    if (committed) State.RecordMove(wnd.uptimeSeconds());
                    if (committed && State.CheckSolved(MyGrid, wnd.uptimeSeconds()))
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

    private void QueuePlaybackAnimation(bool forward)
    {
        if (sessions.Mode != RecordingControllerMode.Playback)
            throw new InvalidOperationException("Not in playback.");
        if (!forward) sessions.SetAutoplay(false);
        var playback = sessions.Playback!;
        int index = forward ? sessions.Position : sessions.Position - 1;
        if (index < 0 || index >= playback.Moves.Count)
        {
            if (forward) sessions.SetAutoplay(false);
            return;
        }

        State.TakeBackSolved(wnd.uptimeSeconds());
        var controller = sessions;
        var move = playback.Moves[index];
        bool column = move.Axis == RecordedMoveAxis.Column;
        int length = column ? MyGrid.Height : MyGrid.Width;
        int delta = move.Delta % length;
        if (!forward) delta = -delta;
        moveHighlight = forward ? (move.StartX, move.StartY) : column
            ? (move.StartX, (move.StartY + delta + length) % length)
            : ((move.StartX + delta + length) % length, move.StartY);
        animationLockHoriz = column;
        animationOrdinal = move.Index;
        animationTileOffset = 0;
        double start = -1;
        double duration = Math.Max(0.1, Math.Min(0.35, Math.Abs(delta) * 0.12));
        Animations.Enqueue(new Animation { animation = (window, animation) =>
        {
            double time = wnd.uptimeSeconds();
            if (start < 0) start = time;
            float t = Math.Clamp((float)((time - start) / duration), 0f, 1f);
            if (t >= 1f)
            {
                if (forward) controller.Step();
                else controller.StepBack();
                animationLockHoriz = null;
                animationTileOffset = 0;
                moveHighlight = null;
                lastPlaybackTick = time;
                return false;
            }
            animationTileOffset = delta * (t * t * (3f - 2f * t));
            return true;
        }});
    }

    private void OnMouseUp(CrystalWindow windowHandle, int button, int x, int y)
    {
        if (popupHost.IsVisible)
        {
            popupHost.PointerUp(new System.Drawing.PointF(x, y), (CrystalMouseButton)button);
            return;
        }
        interaction.PointerUp(x, y, (CrystalMouseButton)button);
        if (button == (int)CrystalMouseButton.Left)
        {
            if (activeSlider != ActiveSlider.None)
            {
                activeSlider = ActiveSlider.None;
                return;
            }

            if (drag != null && tileView != null)
            {
                int cellX = drag.cellX;
                int cellY = drag.cellY;
                drag = null;

                if (animationLockHoriz != null)
                {
                    bool horizontal = animationLockHoriz.Value;
                    int ordinal = animationOrdinal;
                    float currentOffset = animationTileOffset;
                    int delta = (int)Math.Round(currentOffset);

                    QueueSnapAnimation(horizontal, ordinal, currentOffset, delta, delta, cellX, cellY);
                }
                else
                {
                    animationLockHoriz = null;
                    animationTileOffset = 0;
                    moveHighlight = null;
                }
            }
        }
    }

    private void OnMouseDown(CrystalWindow windowHandle, int button, int x, int y)
    {
        if (button != (int)CrystalMouseButton.Left) return;

        // Route to popup when visible
        if (popupHost.IsVisible)
        {
            popupHost.PointerDown(new System.Drawing.PointF(x, y), CrystalMouseButton.Left);
            return;
        }

        windowHandle.GetSize(out int width, out int height);
        UpdateTileView(width, height);
        var media = GetMediaLayout(width, height);
        UpdateMediaEditorBounds(media);
        if (!ShowGridSetup && showMedia && filenameEditor.Bounds.Contains(x, y))
        {
            if (!filenameEditor.IsFocused) filenameBeforeEdit = filenameEditor.Text;
            interaction.PointerDown(x, y, CrystalMouseButton.Left);
            return;
        }
        interaction.SetFocus(null);
        if (!ShowGridSetup && showMedia)
        {
            for (int i = 0; i < mediaLabels.Length; i++)
            {
                if (!media.Button(i).Contains(x, y)) continue;
                MediaAction(i);
                return;
            }
            if (new SKRect(media.X, media.Y, media.X + media.Width * media.Scale, media.Y + media.Height * media.Scale).Contains(x, y)) return;
        }

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
        if (sessions.Mode != RecordingControllerMode.Playback && State.Mode != GameMode.Scrambling && tileView != null && Animations.Count == 0 && drag == null)
        {
            // Check if mouse is inside the board bounds
            if (x >= tileView.xx && x < tileView.xx + tileView.width &&
                y >= tileView.yy && y < tileView.yy + tileView.height)
            {
                drag = new Drag()
                {
                    startX = x,
                    startY = y,
                    cellX = (int)((x - tileView.xx) * MyGrid.Width / tileView.width),
                    cellY = (int)((y - tileView.yy) * MyGrid.Height / tileView.height),
                };
                moveHighlight = (drag.cellX, drag.cellY);
            }
        }
    }

    private void OnIdle(CrystalWindow windowHandle)
    {
        interaction.OnIdle(windowHandle.uptimeSeconds());
        if (popupHost.IsVisible)
        {
            popupHost.OnIdle(windowHandle.uptimeSeconds());
            windowHandle.QueueRedraw();
            return;
        }
        if (sessions.IsAutoplaying && Animations.Count == 0 && drag == null && wnd.uptimeSeconds() - lastPlaybackTick >= 0.4)
        {
            QueuePlaybackAnimation(true);
            lastPlaybackTick = wnd.uptimeSeconds();
        }
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

    private void OnResize(CrystalWindow windowHandle, int width, int height)
    {
        if (_glAvailable && _glPresenter != null)
        {
            _glPresenter.SetViewport(width, height);
        }
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

        // Initialize GL presenter on first draw
        if (_glAvailable && _glPresenter == null && _gl != null)
        {
            try
            {
                wnd.GLMakeCurrent();
                _glPresenter = new GLPixDataPresenter(_gl);
                _glPresenter.SetViewport(width, height);
                _glPresenter.CollectDiagnostics = true;
                Console.WriteLine("[GL] Presenter initialized successfully.");
            }
            catch (Exception ex)
            {
                _glError = ex.Message;
                _glAvailable = false;
                Console.WriteLine($"[GL] Warning: Presenter init failed ({_glError}) — falling back to legacy.");
            }
        }

        if (!pixelBacking || pixelBacking.width != width || pixelBacking.height != height)
        {
            if (pixelBacking)
            {
                pixelBacking.Dispose();
            }
            pixelBacking = CrystalSkia.net.FixedPixDataRenderer.CreateFixed(width, height, (canvas, info) => { });
        }

        UpdateTileView(width, height);
        var media = GetMediaLayout(width, height);
        UpdateMediaEditorBounds(media);

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

            if (moveHighlight is { } highlighted)
            {
                float x = tileView.xx + tileW * highlighted.X;
                float y = tileView.yy + tileH * highlighted.Y;
                float wrapX = 0, wrapY = 0;
                if (animationLockHoriz is { } column)
                {
                    int length = column ? MyGrid.Height : MyGrid.Width;
                    float offset = animationTileOffset % length;
                    if (offset < 0) offset += length;
                    if (column) { y -= tileH * offset; wrapY = tileView.height; }
                    else { x -= tileW * offset; wrapX = tileView.width; }
                }
                canvas.Save();
                canvas.ClipRect(new SKRect(tileView.xx, tileView.yy, tileView.xx + tileView.width, tileView.yy + tileView.height));
                canvas.DrawRoundRect(new SKRoundRect(new SKRect(x + 3, y + 3, x + tileW - 3, y + tileH - 3), 6, 6), activeLinePaint);
                if (wrapX != 0 || wrapY != 0)
                    canvas.DrawRoundRect(new SKRoundRect(new SKRect(x + wrapX + 3, y + wrapY + 3, x + wrapX + tileW - 3, y + wrapY + tileH - 3), 6, 6), activeLinePaint);
                canvas.Restore();
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
            if (width >= 650) canvas.DrawText(fpsText, 132, 35, hudFont, hudMutedPaint);

            // Diagnostics overlay
            if (ShowDiagnostics)
            {
                if (_glAvailable && _glPresenter != null)
                {
                    var diag = _glPresenter.LastFrameDiagnostics;
                    string diagText =
                        $"GL: {diag.TextureUploadMs:F1}ms upload + {diag.DrawSubmissionMs:F1}ms draw | {diag.TextureWidth}x{diag.TextureHeight} -> {diag.ViewportWidth}x{diag.ViewportHeight}";
                    canvas.DrawText(diagText, 16, height - 30, hudFont, hudMutedPaint);
                }
            }

            // Center HUD: Moves & Time
            string movesText = $"Moves: {State.MoveCount}";
            string timeText = $"Time: {State.FormatTime(time)}";
            string? recordingText = null;
            if (sessions.Mode == RecordingControllerMode.Playback)
            {
                recordingText = $"Move {sessions.Position} of {sessions.MoveCount}";
            }
            
            if (sessions.Mode == RecordingControllerMode.Recording)
            {
                recordingText = $"Move {sessions.MoveCount}";
            }
            
            hudBoldFont.MeasureText(movesText, out SKRect movesBounds, hudTextPaint);
            hudBoldFont.MeasureText(timeText, out SKRect timeBounds, hudAccentPaint);
            SKRect? recordingBounds = null;
            if (recordingText != null)
            {
                hudBoldFont.MeasureText(recordingText, out SKRect rb, hudMutedPaint);
                recordingBounds = rb;
            }

            float centerTotalWidth = movesBounds.Width + timeBounds.Width + 30;
            if (recordingBounds.HasValue) centerTotalWidth += recordingBounds.Value.Width + 24;
            float centerAreaStart = width / 2f - centerTotalWidth / 2f;
            if (width >= 500)
            {
                canvas.DrawText(movesText, centerAreaStart, 35, hudBoldFont, hudTextPaint);
                float timeX = centerAreaStart + movesBounds.Width + 24;
                canvas.DrawText(timeText, timeX, 35, hudBoldFont, hudAccentPaint);
                if (recordingText != null && recordingBounds.HasValue)
                {
                    float recX = timeX + timeBounds.Width + 24;
                    canvas.DrawText(recordingText, recX, 35, hudBoldFont, hudMutedPaint);
                }
            }

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
            if (width >= 650) canvas.DrawText(modeText, modeTextX, 35, hudBoldFont, statePaint);
            if (width >= 650 && State.Mode == GameMode.Solved)
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

            if (showMedia) DrawMedia(canvas, media);

            // Session Selection Popup
            if (popupHost.IsVisible)
            {
                LayoutSelectionPopup(width, height);
                ((ICrystalSkia)popupHost).OnDraw(canvas);
                DrawPreview(canvas);
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
            string footerText = "[S] Scramble   [X] Reset   [G] Grid   [1-4] Size   [R] Session   [C] Challenge   [Esc] Cancel";
            footerFont.MeasureText(footerText, out SKRect footerBounds, hudMutedPaint);
            canvas.Save();
            float footerScale = Math.Min(1, Math.Max(1, width - 16) / Math.Max(1, footerBounds.Width));
            canvas.Translate(width / 2f, height - 12);
            canvas.Scale(footerScale);
            canvas.DrawText(footerText, -footerBounds.Width / 2f, 0, footerFont, hudMutedPaint);
            canvas.Restore();
        });

        // Present via OpenGL or legacy path
        if (_glAvailable && _glPresenter != null)
        {
            wnd.GLMakeCurrent();
            bool presented = _glPresenter.Present(ref pixelBacking);
            if (presented)
            {
                wnd.GLPresent();
            }
            else
            {
                // Upload or draw failed — fall back to legacy presentation this frame
                Console.WriteLine($"[GL] Present failed: {_glPresenter.LastError} — using legacy fallback.");
                windowHandle.PresentPix(ref pixelBacking);
            }
        }
        else
        {
            windowHandle.PresentPix(ref pixelBacking);
        }
    }

    public void Show(bool visible = true)
    {
        wnd.Show(visible);
    }

    private void ShowSessionSelectionPopup()
    {
        ShowSelectionPopup(SelectionKind.Session);
    }

    private void ShowSelectionPopup(SelectionKind kind)
    {
        if (popupHost.IsVisible) return;
        if (sessions.Mode == RecordingControllerMode.Recording || drag != null || Animations.Count != 0 ||
            State.Mode == GameMode.Scrambling || ShowGridSetup || activeSlider != ActiveSlider.None)
        {
            sessionMessage = "Stop recording and finish the current move or setup first";
            showMedia = true;
            return;
        }

        try
        {
            var names = kind == SelectionKind.Session
                ? (Directory.Exists(Path.Combine(AppContext.BaseDirectory, "sessions"))
                    ? Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "sessions"), "*.json")
                        .Select(Path.GetFileNameWithoutExtension).Where(n => n != null).Select(n => n!).ToArray()
                    : Array.Empty<string>())
                : (challengeCatalog = ChallengeCatalog.LoadBundled()).Names.ToArray();
            Array.Sort(names, StringComparer.Ordinal);
            selectionKind = kind;
            selectionMessage = names.Length == 0 ? "No entries available" : "Select an entry";
            var control = new CrystalSelectionControl(new System.Drawing.RectangleF(0, 0, 360,
                CrystalSelectionControl.CalculateHeight(names.Length, 3, 360)), names, "",
                new[] { "Load", "Moves", "Cancel" }, editorFont);
            control.SetButtonEnabled(0, false);
            control.SetButtonEnabled(1, false);
            control.SelectionHint += (_, _) => UpdateSelectionPreview();
            control.ButtonActivated += OnSelectionButtonActivated;
            selectionControl = control;
            interaction.SetFocus(null);
            wnd.GetSize(out int width, out int height);
            popupHost.Show(control, new System.Drawing.RectangleF(0, 0, width, height));
            LayoutSelectionPopup(width, height);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or NotSupportedException)
        {
            selectionKind = SelectionKind.None;
            sessionMessage = ex.Message;
            showMedia = true;
        }
    }

    private SessionRecording ResolveSelection(string name)
    {
        SessionRecording recording;
        if (selectionKind == SelectionKind.Challenge)
        {
            if (challengeCatalog == null || !challengeCatalog.TryGet(name, out var challenge) || challenge == null)
                throw new InvalidOperationException("Challenge is unavailable.");
            recording = challenge;
        }
        else
        {
            string path = Path.Combine(AppContext.BaseDirectory, "sessions", name + ".json");
            if (new FileInfo(path).Length > 4_000_000) throw new InvalidOperationException("Session file is too large.");
            recording = SessionRecording.FromJson(File.ReadAllText(path));
        }
        if (recording.Width < 2 || recording.Height < 2 || recording.Width > 9 || recording.Height > 9)
            throw new InvalidOperationException("Board dimensions must be between 2 and 9.");
        return recording;
    }

    private static Grid CreateRecordingGrid(SessionRecording recording, out SessionRecordingController controller)
    {
        var grid = new Grid(recording.Width, recording.Height);
        controller = new SessionRecordingController(grid);
        controller.LoadPlayback(recording);
        return grid;
    }

    private void ClearPreview()
    {
        if (previewGrid != null)
        {
            foreach (var tile in previewGrid.Tiles) tile.Image.Dispose();
            previewGrid = null;
        }
    }

    private void UpdateSelectionPreview()
    {
        ClearPreview();
        var control = selectionControl!;
        control.SetButtonEnabled(0, false);
        control.SetButtonEnabled(1, false);
        selectionMessage = "Select an entry";
        if (control.SelectedIndex >= 0)
        {
            try
            {
                var recording = ResolveSelection(control.ResultText);
                previewGrid = CreateRecordingGrid(recording, out _);
                control.SetButtonEnabled(0, true);
                control.SetButtonEnabled(1, recording.Moves.Count > 0);
                selectionMessage = "Starting board";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or System.Text.Json.JsonException or NotSupportedException)
            {
                selectionMessage = ex.Message;
            }
        }
        wnd.QueueRedraw();
    }

    private void OnSelectionButtonActivated(SelectionButtonEventArgs e)
    {
        if (e.ButtonLabel == "Cancel") { popupHost.Cancel(); return; }
        if (e.SelectedIndex < 0) return;
        try
        {
            var recording = ResolveSelection(e.CurrentText);
            if (e.ButtonLabel == "Moves" && recording.Moves.Count == 0)
                throw new InvalidOperationException("This entry has no recorded moves.");
            var grid = CreateRecordingGrid(recording, out var controller);
            if (e.ButtonLabel == "Load") controller = new SessionRecordingController(grid);
            MyGrid = grid;
            sessions = controller;
            savedSession = e.ButtonLabel == "Moves" ? recording : null;
            State.Reset();
            animationLockHoriz = null;
            animationTileOffset = 0;
            moveHighlight = null;
            UpdateTileView();
            if (selectionKind == SelectionKind.Session) filenameEditor.Text = e.CurrentText;
            sessionMessage = e.ButtonLabel == "Moves" ? "Moves loaded" : "Board loaded";
            popupHost.Accept(e.CurrentText, e.SelectedIndex);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or System.Text.Json.JsonException or NotSupportedException)
        {
            selectionMessage = ex.Message;
        }
    }

    private void OnPopupClosed(PopupResult result)
    {
        selectionKind = SelectionKind.None;
        selectionControl = null;
        challengeCatalog = null;
        ClearPreview();
        selectionMessage = "";
        lastPlaybackTick = wnd.uptimeSeconds();
    }

    private void LayoutSelectionPopup(int width, int height)
    {
        if (selectionControl == null) return;
        float w = Math.Max(1, width), h = Math.Max(1, height);
        float margin = 12, gap = 12;
        float usableW = Math.Max(1, w - margin * 2), usableH = Math.Max(1, h - margin * 2 - 30);
        int cols = previewGrid?.Width ?? 4, rows = previewGrid?.Height ?? 4;
        float sideWidth = Math.Min(360, usableW * 0.48f);
        float sideTile = Math.Min(Math.Max(1, usableW - sideWidth - gap) / cols, usableH / rows);
        float stackHeight = Math.Min(240, usableH * 0.48f);
        float stackTile = Math.Min(usableW / cols, Math.Max(1, usableH - stackHeight - gap) / rows);
        bool side = usableW >= 400 && (sideTile >= stackTile * 0.85f || usableH < 300);
        System.Drawing.RectangleF popup;
        if (side)
        {
            popup = new System.Drawing.RectangleF(margin, margin, sideWidth, usableH);
            previewArea = new SKRect(margin + sideWidth + gap, margin, w - margin, margin + usableH);
        }
        else
        {
            popup = new System.Drawing.RectangleF(margin, margin, usableW, stackHeight);
            previewArea = new SKRect(margin, margin + stackHeight + gap, w - margin, margin + usableH);
        }
        popupHost.SetLayout(new System.Drawing.RectangleF(0, 0, w, h), popup);
    }

    private void DrawPreview(SKCanvas canvas)
    {
        if (previewGrid != null && previewArea.Width > 0 && previewArea.Height > 0)
        {
            float size = Math.Min(previewArea.Width / previewGrid.Width, previewArea.Height / previewGrid.Height);
            float left = previewArea.MidX - size * previewGrid.Width / 2;
            float top = previewArea.MidY - size * previewGrid.Height / 2;
            for (int y = 0; y < previewGrid.Height; y++)
                for (int x = 0; x < previewGrid.Width; x++)
                {
                    var image = previewGrid.Tiles[y, x].Image;
                    canvas.DrawImage(image, new SKRect(left + x * size, top + y * size, left + (x + 1) * size, top + (y + 1) * size), tilePaint);
                }
        }
        canvas.Save();
        canvas.ClipRect(new SKRect(8, previewArea.Bottom + 2, Math.Max(8, popupHost.Bounds.Right), previewArea.Bottom + 22));
        canvas.DrawText(selectionMessage, popupHost.Bounds.Left + 4, previewArea.Bottom + 16, footerFont, hudTextPaint);
        canvas.Restore();
    }
}
