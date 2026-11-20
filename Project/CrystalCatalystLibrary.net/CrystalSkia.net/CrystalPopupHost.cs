using System.Drawing;
using CrystalCatalystLibrary.net;
using SkiaSharp;

namespace CrystalSkia.net;

/// <summary>
/// Describes how a popup was closed.
/// </summary>
public enum PopupResultType
{
    /// <summary>The popup was accepted (OK/Enter).</summary>
    Accepted,

    /// <summary>The popup was cancelled (Cancel button or Escape).</summary>
    Cancelled,

    /// <summary>The popup was closed without explicit acceptance (e.g., external close).</summary>
    Closed
}

/// <summary>
/// Result returned when a popup closes.
/// </summary>
public sealed class PopupResult
{
    /// <summary>How the popup was closed.</summary>
    public PopupResultType Type { get; }

    /// <summary>The result text from the popup content (may be empty for Accepted).</summary>
    public string? ResultText { get; }

    /// <summary>The selected index from the popup content, or -1 if not applicable.</summary>
    public int SelectedIndex { get; }

    public PopupResult(PopupResultType type, string? resultText = null, int selectedIndex = -1)
    {
        Type = type;
        ResultText = resultText;
        SelectedIndex = selectedIndex;
    }
}

/// <summary>
/// Generic popup host that displays compatible Crystal interactive content as an in-window modal overlay.
/// 
/// The popup owns its lifecycle, routes localized pointer and keyboard input, coordinates focus,
/// and provides acceptance, cancellation, and closure behavior.
/// 
/// Use <see cref="Show"/> to display the popup, then read <see cref="Result"/> after <see cref="IsVisible"/>
/// becomes false.
/// </summary>
public sealed class CrystalPopupHost : ICrystalSkia
{
    private ICrystalSkia? content;
    private bool visible;
    private PopupResult? result;
    private bool captured;
    private RectangleF windowBounds;

    // Styling
    public Color BackdropColor { get; set; } = Color.FromArgb(0, 0, 0, 120);
    public Color BorderColor { get; set; } = Color.FromArgb(71, 85, 105);

    /// <summary>Whether the popup is currently visible.</summary>
    public bool IsVisible => visible;

    /// <summary>The result after the popup closes. Null while visible.</summary>
    public PopupResult? Result => result;

    /// <summary>Event raised when the popup closes.</summary>
    public event Action<PopupResult>? Closed;

    /// <summary>
    /// Shows the popup with the given content, centered within the provided window bounds.
    /// </summary>
    /// <param name="content">The interactive content to display.</param>
    /// <param name="windowBounds">The parent window bounds for centering.</param>
    public void Show(ICrystalSkia content, RectangleF windowBounds)
    {
        ArgumentNullException.ThrowIfNull(content);
        this.content = content;
        visible = true;
        result = null;
        captured = false;
        this.windowBounds = windowBounds;

        // Center the popup within the window
        float popupWidth = Math.Min(content.Bounds.Width, windowBounds.Width * 0.7f);
        float popupHeight = content.Bounds.Height;
        float x = windowBounds.X + (windowBounds.Width - popupWidth) / 2;
        float y = windowBounds.Y + (windowBounds.Height - popupHeight) / 2;

        // Clamp to window bounds
        x = Math.Max(windowBounds.X, Math.Min(x, windowBounds.X + windowBounds.Width - popupWidth));
        y = Math.Max(windowBounds.Y, Math.Min(y, windowBounds.Y + windowBounds.Height - popupHeight));

        var popupBounds = new RectangleF(x, y, popupWidth, popupHeight);
        Bounds = popupBounds;

        // Set content bounds so it draws at the popup position
        content.Bounds = popupBounds;

        // Give focus to the content
        content.SetFocus(true);
    }

    /// <summary>Positions visible content within a resized window.</summary>
    public void SetLayout(RectangleF windowBounds, RectangleF contentBounds)
    {
        this.windowBounds = windowBounds;
        Bounds = contentBounds;
        if (content != null) content.Bounds = contentBounds;
    }

    /// <summary>
    /// Closes the popup with an accepted result.
    /// </summary>
    /// <param name="resultText">The result text (from a selection control, for example).</param>
    /// <param name="selectedIndex">The selected index.</param>
    public void Accept(string? resultText = null, int selectedIndex = -1)
    {
        if (!visible) return;
        visible = false;
        Bounds = RectangleF.Empty;
        captured = false;
        this.result = new PopupResult(PopupResultType.Accepted, resultText, selectedIndex);
        content?.SetFocus(false);
        content = null;
        Closed?.Invoke(this.result);
    }

    /// <summary>
    /// Closes the popup with a cancelled result.
    /// </summary>
    public void Cancel()
    {
        if (!visible) return;
        visible = false;
        Bounds = RectangleF.Empty;
        captured = false;
        this.result = new PopupResult(PopupResultType.Cancelled);
        content?.SetFocus(false);
        content = null;
        Closed?.Invoke(this.result);
    }

    /// <summary>
    /// Closes the popup with a closed result (no explicit acceptance).
    /// </summary>
    public void Close()
    {
        if (!visible) return;
        visible = false;
        Bounds = RectangleF.Empty;
        captured = false;
        this.result = new PopupResult(PopupResultType.Closed);
        content?.SetFocus(false);
        content = null;
        Closed?.Invoke(this.result);
    }

    public RectangleF Bounds { get; set; }

    public void SetFocus(bool focused)
    {
        if (focused && visible)
        {
            content?.SetFocus(true);
        }
        else if (!focused)
        {
            content?.SetFocus(false);
        }
    }

    public void OnIdle(double timeSeconds)
    {
        content?.OnIdle(timeSeconds);
    }

    public void OnDraw(SKCanvas canvas)
    {
        if (!visible || content == null) return;

        using var backdropPaint = new SKPaint { Color = ToSkColor(BackdropColor), IsAntialias = true };
        using var borderPaint = new SKPaint { Color = ToSkColor(BorderColor), Style = SKPaintStyle.Stroke, StrokeWidth = 2, IsAntialias = true };

        // Draw backdrop (full window) — the caller should clip to window bounds
        canvas.DrawRect(new SKRect(windowBounds.Left, windowBounds.Top, windowBounds.Right, windowBounds.Bottom), backdropPaint);

        // Draw content border
        var borderRect = new SKRoundRect(
            new SKRect(Bounds.X, Bounds.Y, Bounds.X + Bounds.Width, Bounds.Y + Bounds.Height),
            8f, 8f);
        canvas.DrawRoundRect(borderRect, borderPaint);

        // Draw content — bounds already positioned, no translate needed
        content.OnDraw(canvas);
    }

    private static SKColor ToSkColor(Color color) => new(color.R, color.G, color.B, color.A);

    public void PointerDown(PointF position, CrystalMouseButton button)
    {
        if (!visible || content == null) return;

        // Check if click is within popup bounds
        if (Bounds.Contains(position.X, position.Y))
        {
            captured = true;
            // Convert to local coordinates for content
            PointF local = new(position.X - Bounds.X, position.Y - Bounds.Y);
            content.PointerDown(local, button);
        }
        else
        {
            // Click outside popup — do not close automatically; let the caller handle it
        }
    }

    public void PointerMove(PointF position)
    {
        if (!visible || content == null) return;

        if (captured || Bounds.Contains(position.X, position.Y))
        {
            PointF local = new(position.X - Bounds.X, position.Y - Bounds.Y);
            content.PointerMove(local);
        }
    }

    public void PointerUp(PointF position, CrystalMouseButton button)
    {
        if (!visible || content == null) return;

        if (captured || Bounds.Contains(position.X, position.Y))
        {
            PointF local = new(position.X - Bounds.X, position.Y - Bounds.Y);
            content.PointerUp(local, button);
        }

        if (button == CrystalMouseButton.Left)
        {
            captured = false;
        }
    }

    public void KeyDown(KeyCode key, bool shift, bool control)
    {
        if (!visible || content == null) return;

        // Handle popup-level keys first
        if (key == KeyCode.Escape)
        {
            Cancel();
            return;
        }
        // Forward all other keys to content
        content.KeyDown(key, shift, control);
    }

    public void TextInput(string text)
    {
        if (!visible || content == null) return;
        content.TextInput(text);
    }
}
