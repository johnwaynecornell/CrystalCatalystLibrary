using SkiaSharp;

namespace CrystalCatalyst.SkiaScene.Experimental;

public class SkiaPresence : SkiaScene
{
    public AnimationContext AnimationContext { get; } = new();
    public RenderContext RenderContext { get; } = new();

    public virtual void Update(double dt)
    {
        AnimationContext.Advance(dt);
        base.Update(dt, AnimationContext);
    }

    public virtual void Render(SKCanvas canvas)
    {
        RenderContext.TotalSeconds = AnimationContext.TotalSeconds;
        base.Render(canvas, RenderContext);
    }
}