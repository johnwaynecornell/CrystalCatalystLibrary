using System.Drawing;
using System.Globalization;
using System.Text;
using SkiaSharp;

namespace CrystalCatalystLibrary.net;

/// <summary>Single-line editing model. Indices are UTF-16 offsets at text-element boundaries.</summary>
public sealed class CrystalTextEditor : ICrystal
{
    private string text = "";
    private int caret;
    private int anchor;
    private bool dragging;
    private double? blinkStart;

    /// <summary>The caller owns the font and any typefaces returned by the fallback resolver.</summary>
    public CrystalTextEditor(RectangleF bounds, SKFont font)
    {
        Bounds = bounds;
        Font = font ?? throw new ArgumentNullException(nameof(font));
    }

    public RectangleF Bounds { get; set; }
    public SKFont Font { get; set; }
    public Func<string, SKTypeface?>? TypefaceForGlyph { get; set; }
    public Color TextColor { get; set; } = Color.White;
    public Color BackgroundColor { get; set; } = Color.Black;
    public Color AccentColor { get; set; } = Color.DodgerBlue;
    public bool IsFocused { get; private set; }
    public bool CaretVisible { get; private set; }
    public int Caret => caret;
    public int SelectionStart => Math.Min(anchor, caret);
    public int SelectionLength => Math.Abs(anchor - caret);
    public string SelectedText => text.Substring(SelectionStart, SelectionLength);
    public event Action? Changed;

    public string Text
    {
        get => text;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            ValidateSingleLine(value);
            if (text == value) return;
            text = value;
            caret = anchor = text.Length;
            ResetBlink();
            Changed?.Invoke();
        }
    }

    public void SetFocus(bool focused)
    {
        if (IsFocused == focused) return;
        IsFocused = focused;
        if (!focused) dragging = false;
        ResetBlink();
    }

    /// <summary>Updates the caret blink using the host's monotonic time in seconds.</summary>
    public void OnIdle(double timeSeconds)
    {
        if (!IsFocused) return;
        if (blinkStart == null || timeSeconds < blinkStart.Value) blinkStart = timeSeconds;
        CaretVisible = (timeSeconds - blinkStart.Value) % 1.0 < 0.5;
    }

    public void OnDraw(SKCanvas canvas)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(Font);
        using var paint = new SKPaint { IsAntialias = true };
        canvas.Save();
        try
        {
            canvas.Translate(Bounds.X, Bounds.Y);
            var area = new SKRect(0, 0, Bounds.Width, Bounds.Height);
            canvas.ClipRect(area);
            paint.Color = ToSkColor(BackgroundColor);
            canvas.DrawRect(area, paint);
            if (IsFocused && SelectionLength > 0)
            {
                float start = MeasureText(text[..SelectionStart]);
                float end = MeasureText(text[..(SelectionStart + SelectionLength)]);
                paint.Color = ToSkColor(AccentColor);
                canvas.DrawRect(new SKRect(start, 0, end, Bounds.Height), paint);
            }
            paint.Color = ToSkColor(TextColor);
            var metrics = Font.Metrics;
            DrawGlyphs(canvas, text, (Bounds.Height - metrics.Ascent - metrics.Descent) / 2, paint);
            if (CaretVisible)
            {
                float x = MeasureText(text[..caret]);
                canvas.DrawLine(x, Bounds.Height * .125f, x, Bounds.Height * .875f, paint);
            }
            paint.Style = SKPaintStyle.Stroke;
            paint.Color = ToSkColor(IsFocused ? AccentColor : TextColor);
            canvas.DrawRect(area, paint);
        }
        finally { canvas.Restore(); }
    }

    private static SKColor ToSkColor(Color color) => new(color.R, color.G, color.B, color.A);

    private float Glyphs(string value, SKCanvas? canvas = null, float baseline = 0, SKPaint? paint = null)
    {
        float x = 0;
        foreach (var rune in value.EnumerateRunes())
        {
            string glyph = rune.ToString();
            SKTypeface? fallback = Font.ContainsGlyphs(glyph) ? null : TypefaceForGlyph?.Invoke(glyph);
            using var fallbackFont = fallback == null ? null : new SKFont(fallback, Font.Size, Font.ScaleX, Font.SkewX);
            var font = fallbackFont ?? Font;
            if (canvas != null) canvas.DrawText(glyph, x, baseline, font, paint!);
            x += font.MeasureText(glyph);
        }
        return x;
    }

    private float MeasureText(string value) => Glyphs(value);
    private void DrawGlyphs(SKCanvas canvas, string value, float baseline, SKPaint paint) => Glyphs(value, canvas, baseline, paint);

    private void ResetBlink()
    {
        blinkStart = null;
        CaretVisible = IsFocused;
    }

    public void PointerDown(PointF position, CrystalMouseButton button)
    {
        if (button != CrystalMouseButton.Left) return;
        ResetBlink();
        caret = anchor = IndexAt(position.X);
        dragging = true;
    }

    public void PointerMove(PointF position)
    {
        if (dragging) { ResetBlink(); caret = IndexAt(position.X); }
    }

    public void PointerUp(PointF position, CrystalMouseButton button)
    {
        if (button != CrystalMouseButton.Left || !dragging) return;
        ResetBlink();
        caret = IndexAt(position.X);
        dragging = false;
    }

    public void KeyDown(KeyCode key, bool shift, bool control)
    {
        ResetBlink();
        if (control && key == KeyCode.A)
        {
            anchor = 0;
            caret = text.Length;
            return;
        }
        if (control) return;
        switch (key)
        {
            case KeyCode.Left:
                Move(!shift && SelectionLength > 0 ? SelectionStart : Previous(caret), shift);
                break;
            case KeyCode.Right:
                Move(!shift && SelectionLength > 0 ? SelectionStart + SelectionLength : Next(caret), shift);
                break;
            case KeyCode.Home: Move(0, shift); break;
            case KeyCode.End: Move(text.Length, shift); break;
            case KeyCode.BackSpace:
                if (SelectionLength == 0) anchor = Previous(caret);
                ReplaceSelection("");
                break;
            case KeyCode.Delete:
                if (SelectionLength == 0) anchor = Next(caret);
                ReplaceSelection("");
                break;
        }
    }

    public void TextInput(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        ValidateSingleLine(value);
        ResetBlink();
        if (value.Length != 0) ReplaceSelection(value);
    }

    private static void ValidateSingleLine(string value)
    {
        if (value.Any(char.IsControl) || value.Contains('\u2028') || value.Contains('\u2029'))
            throw new ArgumentException("Text must not contain control characters or line separators.", nameof(value));
    }

    private void ReplaceSelection(string value)
    {
        int start = SelectionStart;
        string updated = text.Remove(start, SelectionLength).Insert(start, value);
        if (updated == text) { caret = anchor = start + value.Length; return; }
        text = updated;
        // Insertion can combine with neighboring text elements; move to the next valid boundary.
        int end = start + value.Length;
        caret = anchor = Boundaries().First(i => i >= end);
        Changed?.Invoke();
    }

    private int[] Boundaries() => StringInfo.ParseCombiningCharacters(text).Append(text.Length).ToArray();
    private int Previous(int index) => Boundaries().LastOrDefault(i => i < index);
    private int Next(int index) => Boundaries().FirstOrDefault(i => i > index, text.Length);
    private void Move(int index, bool select) { caret = index; if (!select) anchor = caret; }

    private int IndexAt(float x)
    {
        var boundaries = Boundaries();
        for (int i = 1; i < boundaries.Length; i++)
        {
            float left = MeasureText(text[..boundaries[i - 1]]);
            float right = MeasureText(text[..boundaries[i]]);
            if (x < (left + right) / 2) return boundaries[i - 1];
        }
        return text.Length;
    }
}