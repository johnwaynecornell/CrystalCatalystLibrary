using System.Drawing;
using SkiaSharp;

namespace CrystalCatalystLibrary.net;

/// <summary>A rectangular interaction target. Pointer coordinates passed to it are local to Bounds.</summary>
public interface ICrystal
{
    RectangleF Bounds { get; }
    void OnIdle(double timeSeconds);
    void OnDraw(SKCanvas canvas);
    void SetFocus(bool focused);
    void PointerDown(PointF position, CrystalMouseButton button);
    void PointerMove(PointF position);
    void PointerUp(PointF position, CrystalMouseButton button);
    void KeyDown(KeyCode key, bool shift, bool control);
    void TextInput(string text);
}


/// <summary>Routes window coordinates to targets, with focus and primary-button capture.</summary>
public sealed class CrystalInteraction
{
    private readonly List<ICrystal> targets = new();
    private ICrystal? focused;
    private ICrystal? captured;

    public ICrystal? Focused => focused;

    public void Add(ICrystal target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (targets.Contains(target)) throw new ArgumentException("Target is already registered.", nameof(target));
        targets.Add(target);
    }

    public bool Remove(ICrystal target)
    {
        if (!targets.Remove(target)) return false;
        if (captured == target) captured = null;
        if (focused == target) SetFocus(null);
        return true;
    }

    public void SetFocus(ICrystal? target)
    {
        if (target != null && !targets.Contains(target)) throw new ArgumentException("Target is not registered.", nameof(target));
        if (focused == target) return;
        var old = focused;
        focused = target;
        old?.SetFocus(false);
        focused?.SetFocus(true);
    }

    public void PointerDown(float x, float y, CrystalMouseButton button)
    {
        var target = HitTest(x, y);
        if (button == CrystalMouseButton.Left)
        {
            SetFocus(target);
            captured = target;
        }
        target?.PointerDown(Local(target, x, y), button);
    }

    public void PointerMove(float x, float y)
    {
        var target = captured ?? HitTest(x, y);
        target?.PointerMove(Local(target, x, y));
    }

    public void PointerUp(float x, float y, CrystalMouseButton button)
    {
        var target = button == CrystalMouseButton.Left ? captured : HitTest(x, y);
        if (button == CrystalMouseButton.Left) captured = null;
        target?.PointerUp(Local(target, x, y), button);
    }

    public void KeyDown(KeyCode key, bool shift = false, bool control = false) => focused?.KeyDown(key, shift, control);
    public void TextInput(string text) => focused?.TextInput(text);
    public void OnIdle(double timeSeconds)
    {
        foreach (var target in targets) target.OnIdle(timeSeconds);
    }

    private ICrystal? HitTest(float x, float y)
    {
        for (int i = targets.Count - 1; i >= 0; i--)
            if (targets[i].Bounds.Contains(x, y)) return targets[i];
        return null;
    }

    private static PointF Local(ICrystal target, float x, float y) => new(x - target.Bounds.X, y - target.Bounds.Y);
}