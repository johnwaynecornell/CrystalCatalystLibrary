using System.Drawing;
using CrystalCatalystLibrary.net;
using SkiaSharp;

namespace CrystalSkia.net;

public sealed record CrystalTextPopupButton(string Label, Action Execute);

/// <summary>Multiline ANSI text for a popup host or an owner-drawn passive preview.</summary>
public sealed class CrystalTextPopup : ICrystalSkia
{
    private const float ButtonHeight = 32;
    private const float ButtonGap = 8;
    private readonly AnsiSkiaRenderer renderer;
    private readonly CrystalTextPopupButton[] buttons;
    private readonly float padding;
    private readonly AnsiSkiaRenderer.ParsedContent source;
    private readonly SKFont? buttonFont;
    private readonly float naturalWidth;
    private AnsiSkiaRenderer.ParsedContent? wrapped;
    private float layoutWidth = -1;
    private int selected = -1;
    private int pressed = -1;
    private bool blinkState = true;

    /// <param name="text">The text to display, including optional ANSI styling.</param>
    /// <param name="renderer">The configured renderer. Keep its font settings stable while in use.</param>
    /// <param name="padding">The inset around the content in pixels.</param>
    /// <param name="buttons">Optional buttons, in keyboard navigation order.</param>
    public CrystalTextPopup(string text, AnsiSkiaRenderer renderer, float padding = 12,
        IEnumerable<CrystalTextPopupButton>? buttons = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(padding);
        this.renderer = renderer;
        this.padding = padding;
        this.buttons = buttons?.ToArray() ?? Array.Empty<CrystalTextPopupButton>();
        source = renderer.Parse(text);
        float buttonWidth = 0;
        if (this.buttons.Length > 0)
        {
            buttonFont = renderer.GetFont(renderer.GetTypeface(renderer.FontFamily), renderer.FontSize, false, false);
            buttonWidth = this.buttons.Max(button => buttonFont.MeasureText(button.Label) + 16) * this.buttons.Length +
                ButtonGap * (this.buttons.Length - 1);
        }
        naturalWidth = Math.Max(source.PixelSize.Width - 2 * AnsiSkiaRenderer.DefaultPadding, buttonWidth) + 2 * padding;
        var size = Measure();
        Bounds = new RectangleF(0, 0, size.Width, size.Height);
    }

    public RectangleF Bounds { get; set; }

    /// <summary>Measures at natural size or reflows the text at a constrained width.</summary>
    public SizeF Measure(float maxWidth = float.PositiveInfinity)
    {
        float width = Math.Clamp(naturalWidth, 0, Math.Max(0, maxWidth));
        return new SizeF(width, Reflow(width).PixelSize.Height + ButtonBandHeight);
    }

    /// <summary>Centers and constrains the modal popup. Escape is handled by the host, even with zero buttons.</summary>
    public void ShowModal(CrystalPopupHost host, RectangleF area)
    {
        var size = Measure(area.Width * 0.7f);
        float height = Math.Min(size.Height, area.Height);
        host.Show(this, area);
        host.SetLayout(area, new RectangleF(area.X + (area.Width - size.Width) / 2,
            area.Y + (area.Height - height) / 2, size.Width, height));
    }

    private float ButtonBandHeight => buttons.Length == 0 ? 0 : ButtonHeight + ButtonGap;

    private AnsiSkiaRenderer.ParsedContent Reflow(float width)
    {
        if (wrapped == null || layoutWidth != width)
        {
            wrapped = renderer.Wrap(source, width, padding);
            layoutWidth = width;
        }
        return wrapped;
    }

    private float ContentHeight => Reflow(Bounds.Width).PixelSize.Height + ButtonBandHeight;
    private float ContentScale => Math.Min(1, Bounds.Height / Math.Max(1, ContentHeight));
    private float ContentOffsetX => Bounds.Width * (1 - ContentScale) / 2;
    private float ContentOffsetY => Math.Max(0, Bounds.Height - ContentHeight) / 2;

    private SKRect ButtonBounds(int index)
    {
        float gap = Math.Min(ButtonGap, Bounds.Width / (2 * buttons.Length));
        float inset = Math.Min(padding, Bounds.Width / 4);
        float width = Math.Max(0, (Bounds.Width - 2 * inset - gap * (buttons.Length - 1)) / buttons.Length);
        float x = inset + index * (width + gap);
        float y = ContentHeight - padding - ButtonHeight;
        return new SKRect(x, y, x + width, y + ButtonHeight);
    }

    public void OnDraw(SKCanvas canvas)
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0) return;
        canvas.Save();
        canvas.Translate(Bounds.X, Bounds.Y);
        canvas.ClipRect(new SKRect(0, 0, Bounds.Width, Bounds.Height));
        using var paint = new SKPaint { IsAntialias = true, Color = new SKColor(22, 33, 52, 248) };
        canvas.DrawRoundRect(new SKRect(0, 0, Bounds.Width, Bounds.Height), 8, 8, paint);
        canvas.Translate(ContentOffsetX, ContentOffsetY);
        canvas.Scale(ContentScale);
        renderer.Render(Reflow(Bounds.Width), canvas, blinkState, padding, padding + renderer.FontSize);

        for (int i = 0; i < buttons.Length; i++)
        {
            var rect = ButtonBounds(i);
            paint.Color = i == selected ? new SKColor(63, 107, 155) : new SKColor(38, 59, 86);
            canvas.DrawRoundRect(rect, 5, 5, paint);
            paint.Color = SKColors.White;
            var font = buttonFont!;
            float textWidth = font.MeasureText(buttons[i].Label);
            float scale = Math.Min(1, Math.Max(0, rect.Width - 8) / Math.Max(1, textWidth));
            if (scale == 0) continue;
            canvas.Save();
            canvas.Translate(rect.MidX, rect.MidY - (font.Metrics.Ascent + font.Metrics.Descent) / 2);
            canvas.Scale(scale, 1);
            canvas.DrawText(buttons[i].Label, -textWidth / 2, 0, font, paint);
            canvas.Restore();
        }
        canvas.Restore();
    }

    public void SetFocus(bool focused)
    {
        selected = focused && buttons.Length > 0 ? 0 : -1;
        pressed = -1;
    }

    public void OnIdle(double timeSeconds) => blinkState = ((long)(timeSeconds * 2) & 1) == 0;
    public void TextInput(string text) { }

    public void KeyDown(KeyCode key, bool shift, bool control)
    {
        if (selected < 0 || control) return;
        if (key is KeyCode.Enter or KeyCode.KP_Enter)
            buttons[selected].Execute();
        else if (key is KeyCode.Left or KeyCode.KP_Left || (key == KeyCode.Tab && shift))
            selected = (selected + buttons.Length - 1) % buttons.Length;
        else if (key is KeyCode.Right or KeyCode.KP_Right or KeyCode.Tab)
            selected = (selected + 1) % buttons.Length;
    }

    private int HitTest(PointF position)
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0) return -1;
        float x = (position.X - ContentOffsetX) / ContentScale;
        float y = (position.Y - ContentOffsetY) / ContentScale;
        for (int i = 0; i < buttons.Length; i++)
            if (ButtonBounds(i).Contains(x, y)) return i;
        return -1;
    }

    public void PointerDown(PointF position, CrystalMouseButton button)
    {
        if (button != CrystalMouseButton.Left) return;
        pressed = HitTest(position);
        if (pressed >= 0) selected = pressed;
    }

    public void PointerMove(PointF position)
    {
        int index = HitTest(position);
        if (index >= 0) selected = index;
    }

    public void PointerUp(PointF position, CrystalMouseButton button)
    {
        if (button != CrystalMouseButton.Left) return;
        int index = pressed;
        pressed = -1;
        if (index >= 0 && HitTest(position) == index) buttons[index].Execute();
    }
}
