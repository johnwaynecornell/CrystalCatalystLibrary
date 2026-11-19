using System.Drawing;
using CrystalCatalystLibrary.net;
using SkiaSharp;

namespace CrystalSkia.net;

/// <summary>
/// Orientation of a <see cref="CrystalScrollBar"/>.
/// </summary>
public enum ScrollBarOrientation
{
    /// <summary>Vertical scroll bar (scrolls up/down).</summary>
    Vertical,
    /// <summary>Horizontal scroll bar (scrolls left/right).</summary>
    Horizontal
}

/// <summary>
/// Reusable scroll bar control with a normalized offset (0.0–1.0) and color styling.
/// Implements <see cref="ICrystalSkia"/> so it can be drawn and interacted with via <see cref="CrystalInteraction"/>.
/// </summary>
public sealed class CrystalScrollBar : ICrystalSkia
{
    private const float ThumbMinSize = 20f;
    private const float ThumbCornerRadius = 4f;

    private bool dragging;
    private float dragStartPosition;
    private float dragStartOffset;

    /// <summary>Overall bounds of the scroll bar track.</summary>
    public RectangleF Bounds { get; set; }

    /// <summary>Orientation of the scroll bar.</summary>
    public ScrollBarOrientation Orientation { get; set; } = ScrollBarOrientation.Vertical;

    /// <summary>
    /// Normalized scroll offset in the range [0.0, 1.0].
    /// 0.0 means scrolled to the start, 1.0 means scrolled to the end.
    /// Readable and writable by the consumer.
    /// </summary>
    public float Offset
    {
        get => scrollOffset;
        set
        {
            var clamped = Math.Clamp(value, 0f, 1f);
            if (scrollOffset != clamped)
            {
                scrollOffset = clamped;
                Changed?.Invoke();
            }
        }
    }

    /// <summary>
    /// The ratio of visible content to total content (0.0–1.0).
    /// For example, 0.5 means the viewport shows half the content, so the thumb occupies half the track.
    /// A value of 1.0 means no scrolling is needed.
    /// </summary>
    public float ViewportRatio
    {
        get => viewportRatio;
        set
        {
            viewportRatio = Math.Clamp(value, 0f, 1f);
            if (viewportRatio >= 1f)
            {
                scrollOffset = 0f;
            }
        }
    }

    /// <summary>Whether the scroll bar is visible (hidden when viewportRatio >= 1.0).</summary>
    public bool Visible => viewportRatio < 1f;

    /// <summary>Event raised when the offset changes.</summary>
    public event Action? Changed;

    // Color styling matching CrystalTextEditor conventions
    /// <summary>Background color of the scroll bar track.</summary>
    public Color BackgroundColor { get; set; } = Color.FromArgb(30, 41, 59);

    /// <summary>Color of the scroll bar thumb.</summary>
    public Color ThumbColor { get; set; } = Color.FromArgb(71, 85, 105);

    /// <summary>Color of the thumb when hovered or dragged.</summary>
    public Color ThumbHoverColor { get; set; } = Color.FromArgb(100, 116, 139);

    private float scrollOffset;
    private float viewportRatio;
    private bool hovered;

    /// <summary>
    /// Creates a new scroll bar.
    /// </summary>
    /// <param name="bounds">Bounds of the scroll bar track area.</param>
    /// <param name="orientation">Vertical or horizontal orientation.</param>
    public CrystalScrollBar(RectangleF bounds, ScrollBarOrientation orientation = ScrollBarOrientation.Vertical)
    {
        Bounds = bounds;
        Orientation = orientation;
    }

    public void SetFocus(bool focused) { }

    public void OnIdle(double timeSeconds) { }

    public void OnDraw(SKCanvas canvas)
    {
        if (!Visible) return;

        using var bgPaint = new SKPaint { Color = ToSkColor(BackgroundColor), IsAntialias = true };
        using var thumbPaint = new SKPaint { Color = ToSkColor(hovered ? ThumbHoverColor : ThumbColor), IsAntialias = true };

        canvas.Save();
        try
        {
            canvas.Translate(Bounds.X, Bounds.Y);

            // Draw track background
            var trackRect = new SKRect(0, 0, Bounds.Width, Bounds.Height);
            canvas.DrawRect(trackRect, bgPaint);

            // Calculate thumb position and size
            float trackLength, thumbLength, thumbPosition;

            if (Orientation == ScrollBarOrientation.Vertical)
            {
                trackLength = Bounds.Height;
                thumbLength = Math.Max(ThumbMinSize, trackLength * viewportRatio);
                float available = trackLength - thumbLength;
                thumbPosition = scrollOffset * available;

                var thumbRect = new SKRect(
                    0,
                    thumbPosition,
                    Bounds.Width,
                    thumbPosition + thumbLength);
                var thumbRRect = new SKRoundRect(thumbRect, ThumbCornerRadius, ThumbCornerRadius);
                canvas.DrawRoundRect(thumbRRect, thumbPaint);
            }
            else
            {
                trackLength = Bounds.Width;
                thumbLength = Math.Max(ThumbMinSize, trackLength * viewportRatio);
                float available = trackLength - thumbLength;
                thumbPosition = scrollOffset * available;

                var thumbRect = new SKRect(
                    thumbPosition,
                    0,
                    thumbPosition + thumbLength,
                    Bounds.Height);
                var thumbRRect = new SKRoundRect(thumbRect, ThumbCornerRadius, ThumbCornerRadius);
                canvas.DrawRoundRect(thumbRRect, thumbPaint);
            }
        }
        finally
        {
            canvas.Restore();
        }
    }

    private static SKColor ToSkColor(Color color) => new(color.R, color.G, color.B, color.A);

    public void PointerDown(PointF position, CrystalMouseButton button)
    {
        if (button != CrystalMouseButton.Left || !Visible) return;

        float localX = position.X - Bounds.X;
        float localY = position.Y - Bounds.Y;

        // Check if click is on the thumb
        if (IsPointOnThumb(localX, localY))
        {
            dragging = true;
            dragStartPosition = GetPositionAlongTrack(localX, localY);
            dragStartOffset = scrollOffset;
            return;
        }

        // Click on track: jump to clicked position
        float trackLength = Orientation == ScrollBarOrientation.Vertical ? Bounds.Height : Bounds.Width;
        float thumbLength = Math.Max(ThumbMinSize, trackLength * viewportRatio);
        float available = trackLength - thumbLength;

        float clickPosition = GetPositionAlongTrack(localX, localY);
        float newOffset = (clickPosition - thumbLength / 2) / available;
        Offset = newOffset;
    }

    public void PointerMove(PointF position)
    {
        float localX = position.X - Bounds.X;
        float localY = position.Y - Bounds.Y;

        if (dragging)
        {
            float trackLength = Orientation == ScrollBarOrientation.Vertical ? Bounds.Height : Bounds.Width;
            float thumbLength = Math.Max(ThumbMinSize, trackLength * viewportRatio);
            float available = trackLength - thumbLength;

            if (available <= 0) return;

            float currentPosition = GetPositionAlongTrack(localX, localY);
            float delta = currentPosition - dragStartPosition;
            float newOffset = dragStartOffset + delta / available;
            Offset = newOffset;
        }
        else
        {
            bool wasHovered = hovered;
            hovered = IsPointOnThumb(localX, localY) || Bounds.Contains(localX + Bounds.X, localY + Bounds.Y);
            if (hovered != wasHovered)
            {
                Changed?.Invoke();
            }
        }
    }

    public void PointerUp(PointF position, CrystalMouseButton button)
    {
        if (dragging && button == CrystalMouseButton.Left)
        {
            dragging = false;
        }
    }

    public void KeyDown(KeyCode key, bool shift, bool control) { }
    public void TextInput(string text) { }

    private float GetPositionAlongTrack(float localX, float localY)
    {
        return Orientation == ScrollBarOrientation.Vertical ? localY : localX;
    }

    private bool IsPointOnThumb(float localX, float localY)
    {
        float trackLength = Orientation == ScrollBarOrientation.Vertical ? Bounds.Height : Bounds.Width;
        float thumbLength = Math.Max(ThumbMinSize, trackLength * viewportRatio);
        float available = trackLength - thumbLength;
        float thumbStart = scrollOffset * available;

        if (Orientation == ScrollBarOrientation.Vertical)
        {
            return localX >= 0 && localX <= Bounds.Width &&
                   localY >= thumbStart && localY <= thumbStart + thumbLength;
        }
        else
        {
            return localX >= thumbStart && localX <= thumbStart + thumbLength &&
                   localY >= 0 && localY <= Bounds.Height;
        }
    }
}
