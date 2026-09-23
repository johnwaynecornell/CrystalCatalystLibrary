using System;
using SkiaSharp;

namespace StreamerTrial;

/// <summary>
/// Interactive slider control with high-DPI Skia rendering and smooth mouse drag interaction.
/// </summary>
public class SliderControl
{
    public string Id { get; set; }
    public string Label { get; set; }
    public float Min { get; set; }
    public float Max { get; set; }
    public float Value { get; set; }
    public Func<float, string> Formatter { get; set; }
    public SKRect Bounds { get; set; }
    public SKColor AccentColor { get; set; } = new SKColor(56, 189, 248); // Sky 400
    public Action<float>? OnValueChanged { get; set; }
    public bool IsDragging { get; set; }

    private static readonly SKPaint TrackPaint = new SKPaint
    {
        Color = new SKColor(51, 65, 85), // Slate 700
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 4,
        StrokeCap = SKStrokeCap.Round,
        IsAntialias = true
    };

    private static readonly SKPaint FillPaint = new SKPaint
    {
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 4,
        StrokeCap = SKStrokeCap.Round,
        IsAntialias = true
    };

    private static readonly SKPaint ThumbPaint = new SKPaint
    {
        Color = SKColors.White,
        Style = SKPaintStyle.Fill,
        IsAntialias = true
    };

    private static readonly SKPaint ThumbBorderPaint = new SKPaint
    {
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 2.5f,
        IsAntialias = true
    };

    private static readonly SKPaint LabelPaint = new SKPaint
    {
        Color = new SKColor(203, 213, 225), // Slate 300
        IsAntialias = true,
        Style = SKPaintStyle.Fill
    };

    private static readonly SKPaint ValuePaint = new SKPaint
    {
        Color = new SKColor(241, 245, 249), // Slate 100
        IsAntialias = true,
        Style = SKPaintStyle.Fill
    };

    public SliderControl(string id, string label, float min, float max, float initialValue, Func<float, string> formatter, Action<float>? onValueChanged = null)
    {
        Id = id;
        Label = label;
        Min = min;
        Max = max;
        Value = Math.Clamp(initialValue, min, max);
        Formatter = formatter;
        OnValueChanged = onValueChanged;
    }

    public void Draw(SKCanvas canvas, SKFont labelFont, SKFont valueFont)
    {
        float trackY = Bounds.Top + 24;
        float trackLeft = Bounds.Left;
        float trackRight = Bounds.Right;
        float trackWidth = Math.Max(1f, trackRight - trackLeft);

        // Header: Label on left, formatted value on right
        canvas.DrawText(Label, Bounds.Left, Bounds.Top + 13, labelFont, LabelPaint);
        string valStr = Formatter(Value);
        valueFont.MeasureText(valStr, out SKRect valBounds, ValuePaint);
        canvas.DrawText(valStr, Bounds.Right - valBounds.Width, Bounds.Top + 13, valueFont, ValuePaint);

        // Track Background
        canvas.DrawLine(trackLeft, trackY, trackRight, trackY, TrackPaint);

        // Filled Track Portion
        float fraction = Math.Clamp((Value - Min) / (Max - Min), 0f, 1f);
        float thumbX = trackLeft + fraction * trackWidth;

        FillPaint.Color = AccentColor;
        if (fraction > 0.001f)
        {
            canvas.DrawLine(trackLeft, trackY, thumbX, trackY, FillPaint);
        }

        // Thumb Knob
        ThumbBorderPaint.Color = AccentColor;
        float thumbRadius = IsDragging ? 7.5f : 6.0f;
        canvas.DrawCircle(thumbX, trackY, thumbRadius, ThumbPaint);
        canvas.DrawCircle(thumbX, trackY, thumbRadius, ThumbBorderPaint);
    }

    public bool Contains(float x, float y)
    {
        // Generous vertical hit region around the slider track
        return x >= Bounds.Left - 8 && x <= Bounds.Right + 8 &&
               y >= Bounds.Top && y <= Bounds.Bottom + 4;
    }

    public bool HandleMouseDown(float x, float y)
    {
        if (Contains(x, y))
        {
            IsDragging = true;
            UpdateValueFromMouse(x);
            return true;
        }
        return false;
    }

    public void HandleMouseMove(float x, float y)
    {
        if (IsDragging)
        {
            UpdateValueFromMouse(x);
        }
    }

    public void HandleMouseUp()
    {
        IsDragging = false;
    }

    private void UpdateValueFromMouse(float x)
    {
        float trackLeft = Bounds.Left;
        float trackRight = Bounds.Right;
        float trackWidth = Math.Max(1f, trackRight - trackLeft);

        float fraction = Math.Clamp((x - trackLeft) / trackWidth, 0f, 1f);
        float newValue = Min + fraction * (Max - Min);
        Value = newValue;
        OnValueChanged?.Invoke(newValue);
    }
}

/// <summary>
/// Clickable button or toggle button with modern styling and responsive interaction.
/// </summary>
public class ButtonControl
{
    public string Id { get; set; }
    public string Label { get; set; }
    public SKRect Bounds { get; set; }
    public bool IsActive { get; set; }
    public Action? OnClick { get; set; }
    public SKColor ActiveColor { get; set; } = new SKColor(16, 185, 129); // Emerald 500
    public SKColor InactiveColor { get; set; } = new SKColor(51, 65, 85); // Slate 700

    private static readonly SKPaint BgPaint = new SKPaint
    {
        Style = SKPaintStyle.Fill,
        IsAntialias = true
    };

    private static readonly SKPaint BorderPaint = new SKPaint
    {
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1.5f,
        IsAntialias = true
    };

    private static readonly SKPaint TextPaint = new SKPaint
    {
        Color = SKColors.White,
        IsAntialias = true,
        Style = SKPaintStyle.Fill
    };

    public ButtonControl(string id, string label, Action? onClick = null)
    {
        Id = id;
        Label = label;
        OnClick = onClick;
    }

    public void Draw(SKCanvas canvas, SKFont font)
    {
        var rrect = new SKRoundRect(Bounds, 6, 6);

        BgPaint.Color = IsActive ? new SKColor(ActiveColor.Red, ActiveColor.Green, ActiveColor.Blue, 180) : new SKColor(30, 41, 59, 220);
        BorderPaint.Color = IsActive ? ActiveColor : new SKColor(71, 85, 105);

        canvas.DrawRoundRect(rrect, BgPaint);
        canvas.DrawRoundRect(rrect, BorderPaint);

        font.MeasureText(Label, out SKRect textBounds, TextPaint);
        float textX = Bounds.Left + (Bounds.Width - textBounds.Width) / 2f;
        float textY = Bounds.Top + (Bounds.Height + textBounds.Height) / 2f - 2f;
        canvas.DrawText(Label, textX, textY, font, TextPaint);
    }

    public bool Contains(float x, float y)
    {
        return Bounds.Contains(x, y);
    }

    public bool HandleClick(float x, float y)
    {
        if (Contains(x, y))
        {
            OnClick?.Invoke();
            return true;
        }
        return false;
    }
}
