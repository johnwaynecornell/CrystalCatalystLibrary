using System.Drawing;
using CrystalCatalystLibrary.net;
using SkiaSharp;

namespace CrystalSkia.net;

public sealed record CrystalMenuEntry(string Label, string? Shortcut = null, bool Enabled = true);

/// <summary>A keyboard- and pointer-operated command list for a Crystal popup.</summary>
public sealed class CrystalCommandMenu : ICrystalSkia
{
    private const float Width = 340;
    private const float HeaderHeight = 42;
    private const float RowHeight = 31;
    private const float Padding = 10;
    private readonly CrystalMenuEntry[] entries;
    private readonly string title;
    private readonly SKFont font;
    private int selected = -1;
    private int pressed = -1;
    private float Height => HeaderHeight + RowHeight * entries.Length + Padding;

    public CrystalCommandMenu(string title, IEnumerable<CrystalMenuEntry> entries, SKFont font)
    {
        this.title = title;
        this.entries = entries.ToArray();
        this.font = font;
        Bounds = new RectangleF(0, 0, Width, Height);
    }

    public RectangleF Bounds { get; set; }
    public event Action<int>? Activated;

    public RectangleF GetCenteredBounds(RectangleF area)
    {
        float scale = Math.Min(1, Math.Min(
            Math.Max(1, area.Width - 24) / Width,
            Math.Max(1, area.Height - 24) / Height));
        float width = Width * scale;
        float height = Height * scale;
        return new RectangleF(
            area.X + (area.Width - width) / 2,
            area.Y + (area.Height - height) / 2,
            width, height);
    }

    public void SetFocus(bool focused)
    {
        pressed = -1;
        selected = focused ? Array.FindIndex(entries, entry => entry.Enabled) : -1;
    }

    public void OnIdle(double timeSeconds) { }
    public void TextInput(string text) { }

    public void KeyDown(KeyCode key, bool shift, bool control)
    {
        if (control || entries.Length == 0) return;
        if (key is KeyCode.Enter or KeyCode.KP_Enter)
        {
            if (selected >= 0) Activated?.Invoke(selected);
            return;
        }

        int direction = key switch
        {
            KeyCode.Up or KeyCode.KP_Up => -1,
            KeyCode.Down or KeyCode.KP_Down => 1,
            _ => 0
        };
        if (direction == 0 || selected < 0) return;
        int next = selected;
        do
        {
            next = (next + direction + entries.Length) % entries.Length;
        } while (!entries[next].Enabled);
        selected = next;
    }

    private int HitTest(PointF position)
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0) return -1;
        float x = position.X * Width / Bounds.Width;
        float y = position.Y * Height / Bounds.Height;
        if (x < Padding || x >= Width - Padding || y < HeaderHeight) return -1;
        int index = (int)((y - HeaderHeight) / RowHeight);
        return index < entries.Length && entries[index].Enabled ? index : -1;
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
        if (index >= 0 && HitTest(position) == index) Activated?.Invoke(index);
    }

    public void OnDraw(SKCanvas canvas)
    {
        canvas.Save();
        canvas.Translate(Bounds.X, Bounds.Y);
        canvas.Scale(Bounds.Width / Width, Bounds.Height / Height);
        using var paint = new SKPaint { IsAntialias = true, Color = new SKColor(0, 0, 0, 70) };
        canvas.DrawRoundRect(new SKRect(2, 4, Width + 2, Height + 4), 15, 15, paint);
        paint.Color = new SKColor(22, 33, 52, 229);
        canvas.DrawRoundRect(new SKRect(0, 0, Width, Height), 14, 14, paint);
        paint.Color = new SKColor(163, 188, 223, 130);
        paint.Style = SKPaintStyle.Stroke;
        canvas.DrawRoundRect(new SKRect(0.5f, 0.5f, Width - 0.5f, Height - 0.5f), 14, 14, paint);
        paint.Style = SKPaintStyle.Fill;
        paint.Color = new SKColor(246, 249, 255);
        canvas.DrawText(title, 20, 27, font, paint);

        for (int i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            float top = HeaderHeight + RowHeight * i;
            if (i == selected)
            {
                paint.Color = new SKColor(63, 107, 155, 225);
                canvas.DrawRoundRect(new SKRect(Padding, top, Width - Padding, top + RowHeight - 2), 7, 7, paint);
            }
            paint.Color = entry.Enabled ? new SKColor(246, 249, 255) : new SKColor(165, 177, 196);
            float baseline = top + 20;
            float shortcutWidth = entry.Shortcut == null ? 0 : font.MeasureText(entry.Shortcut);
            canvas.Save();
            canvas.ClipRect(new SKRect(20, top, Math.Max(20, Width - 30 - shortcutWidth), top + RowHeight));
            canvas.DrawText(entry.Label, 20, baseline, font, paint);
            canvas.Restore();
            if (entry.Shortcut != null)
            {
                paint.Color = new SKColor(219, 230, 247);
                canvas.DrawText(entry.Shortcut, Width - 20 - shortcutWidth, baseline, font, paint);
            }
        }
        canvas.Restore();
    }
}
