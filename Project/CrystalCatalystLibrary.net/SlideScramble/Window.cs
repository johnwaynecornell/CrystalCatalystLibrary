using CrystalCatalystLibrary.net;
using CrystalOpenAL;
using CrystalOpenGL;
using CrystalSkia.net;
using Silk.NET.OpenGL;
using SkiaSharp;

namespace SlideScramble;

public partial class Window
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
    private PuzzleRuleSet activeRules = PuzzleRuleSet.CreateClassic();
    private readonly PortableLensInteractionController lensInteraction = new();
    private bool controlPressed;
    private SessionRecording? savedSession;
    private int? goal;
    private double lastPlaybackTick;
    private (int X, int Y)? moveHighlight;
    private static readonly string[] mediaLabels = { "Rec", "Save", "Load", "|<", "<", ">", "Play", "Stop", "Continue" };
    private ContinueRecordingDialog? continueRecordingDialog;
    private bool showMedia;

    internal static string FormatMovesText(int moves, int? goal) =>
        goal.HasValue ? $"Moves: {moves}  Goal: {goal.Value}" : $"Moves: {moves}";

    private string FormatGameScoreText(int moves, int? goal) => IsShuffleLogic
        ? goal.HasValue ? $"Logical moves: {moves}  Goal: {goal.Value}" : $"Logical moves: {moves}"
        : FormatMovesText(moves, goal);

    // Popup selection integration
    private readonly CrystalPopupHost popupHost = new();
    private readonly BoardCommand[] boardCommands;
    private CrystalCommandMenu? commandMenu;

    private sealed record BoardCommand(
        CrystalMenuEntry Entry, Action? Execute = null, Func<ShortcutKeys, int, bool>? MatchShortcut = null)
    {
        public bool Matches(ShortcutKeys keys, int key) =>
            MatchShortcut?.Invoke(keys, key) ??
            (keys.AllowOrdinary && key is >= 32 and <= 126 && Entry.Shortcut is { Length: 1 } shortcut &&
             char.ToUpperInvariant((char)key) == shortcut[0]);
    }

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
        sessions = new SessionRecordingController(MyGrid, activeRules);
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

        boardCommands = new BoardCommand[]
        {
            new(new CrystalMenuEntry("Edit Board"), OpenBoardEditor),
            new(new CrystalMenuEntry("Game Mode (Classic / ShuffleLogic)"), ToggleGamePreset),
            new(new CrystalMenuEntry("Position Portable Lens"), BeginPortableLensPlacement),
            new(new CrystalMenuEntry("Scramble", "S"), ScrambleBoard),
            new(new CrystalMenuEntry("Generate Challenge"), ShowChallengeGenerator),
            new(new CrystalMenuEntry("Reset Board", "X"), ResetBoard),
            new(new CrystalMenuEntry("Grid Setup", "G"), () => ShowGridSetup = !ShowGridSetup),
            new(new CrystalMenuEntry("3 × 3 Grid", "1"), () => SetGridSize(3)),
            new(new CrystalMenuEntry("4 × 4 Grid", "2"), () => SetGridSize(4)),
            new(new CrystalMenuEntry("6 × 6 Grid", "3"), () => SetGridSize(6)),
            new(new CrystalMenuEntry("8 × 8 Grid", "4"), () => SetGridSize(8)),
            new(new CrystalMenuEntry("Session Controls", "R"), () => showMedia = !showMedia),
            new(new CrystalMenuEntry("Challenges", "C"), () => ShowSelectionPopup(SelectionKind.Challenge)),
            new(new CrystalMenuEntry("Time Reversal", "Ctrl+Shift+R"), ReverseSession, (keys, key) => keys.IsReverse(key)),
            new(new CrystalMenuEntry("Close Menu", "Space / Esc"),
                MatchShortcut: (keys, key) => keys.AllowOrdinary && !keys.Shift && key == (int)KeyCode.Space)
        };
        var modifiers = new ShortcutKeys();
        bool spaceHeld = false;
        wnd.OnKeyUp = (handle, keycode) =>
        {
            modifiers.Release(keycode);
            controlPressed = modifiers.Control;
            if (keycode == (int)KeyCode.Space) spaceHeld = false;
        };
        wnd.OnFocusOut = handle =>
        {
            modifiers.Clear();
            controlPressed = false;
            lensInteraction.CancelGesture();
            spaceHeld = false;
        };
        wnd.OnKeyDown = (handle, keycode) =>
        {
            bool modifierKey = modifiers.Press(keycode);
            controlPressed = modifiers.Control;
            if (modifierKey) return;
            bool control = modifiers.Control;
            bool shift = modifiers.Shift;
            int commandKey = control && keycode >= 'A' && keycode <= 'Z' ? keycode + ('a' - 'A') : keycode;
            if (modifiers.Other) return;
            if (boardDraft != null)
            {
                if (keycode == (int)KeyCode.Escape && !control) CloseBoardEditor();
                return;
            }
            if (keycode == (int)KeyCode.Space && !control && !shift &&
                !filenameEditor.IsFocused && (commandMenu != null || !popupHost.IsVisible))
            {
                if (spaceHeld) return;
                spaceHeld = true;
                if (commandMenu != null) DispatchBoardCommand(modifiers, keycode);
                else ShowCommandMenu();
                return;
            }
            if (commandMenu != null)
            {
                if (!DispatchBoardCommand(modifiers, keycode))
                    popupHost.KeyDown((KeyCode)commandKey, shift, control);
                return;
            }
            // Route to popup when visible
            if (popupHost.IsVisible)
            {
                // Printable characters must become TextInput, not KeyDown
                if (!control && !modifiers.Other && keycode >= 32 && keycode <= 126)
                {
                    popupHost.TextInput(((char)keycode).ToString());
                }
                else
                {
                    popupHost.KeyDown((KeyCode)commandKey, shift, control);
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
                else if (!control && !modifiers.Other && keycode >= 32 && keycode <= 126)
                    interaction.TextInput(((char)keycode).ToString());
                else
                    interaction.KeyDown((KeyCode)commandKey, shift, control);
                return;
            }
            if ((keycode == (int)KeyCode.Enter || keycode == (int)KeyCode.KP_Enter) &&
                lensInteraction.Phase == PortableLensInteractionPhase.Positioning)
            {
                if (lensInteraction.AcceptPosition())
                    sessionMessage = "Lens accepted. Drag between neighboring symbols; Escape cancels.";
                wnd.QueueRedraw();
                return;
            }
            if (keycode == (int)KeyCode.Escape && lensInteraction.Phase != PortableLensInteractionPhase.Inactive)
            {
                lensInteraction.Cancel();
                sessionMessage = "Portable Lens cancelled";
                wnd.QueueRedraw();
                return;
            }
            if (keycode == (int)KeyCode.Escape && !control)
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

            DispatchBoardCommand(modifiers, keycode);
        };
    }


    private bool CanChangeBoard =>
        sessions.Mode == RecordingControllerMode.Idle && State.Mode != GameMode.Scrambling &&
        drag == null && Animations.Count == 0 && lensInteraction.Phase == PortableLensInteractionPhase.Inactive;

    private void ScrambleBoard()
    {
        if (!CanChangeBoard) return;
        if (activeRules.Name == "ShuffleLogic")
        {
            BeginShuffleChallenge(SingleScrambleMoves);
            return;
        }
        goal = null;
        State.StartScrambling();
        ScrambleCount = SingleScrambleMoves;
    }

    private void ResetBoard()
    {
        if (!CanChangeBoard) return;
        MyGrid.Reset();
        State.Reset();
        goal = null;
        animationLockHoriz = null;
        animationTileOffset = 0;
        lensInteraction.Cancel();
    }








    public void Show(bool visible = true)
    {
        wnd.Show(visible);
    }

}
