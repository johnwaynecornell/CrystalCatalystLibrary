using System;
using System.Collections.Generic;
using System.Numerics;
using SkiaSharp;

namespace CrystalCatalyst.SkiaScene.Experimental
{
    /// <summary>
    /// Base class for all elements in a retained-mode Skia scene graph.
    /// </summary>
    public abstract class SkiaElement
    {
        /// <summary>
        /// Identifier used to locate or debug this element.
        /// </summary>
        public string Identifier { get; set; } = string.Empty;

        /// <summary>
        /// Local transform relative to the parent.  Uses <see cref="Matrix3x2"/> for 2D transforms.
        /// </summary>
        public Matrix3x2 Transform { get; set; } = Matrix3x2.Identity;

        /// <summary>
        /// Whether this element should be drawn.  Invisible elements skip themselves and their children.
        /// </summary>
        public bool Visible { get; set; } = true;

        /// <summary>
        /// Local opacity multiplier applied to this element and its children.
        /// </summary>
        public float Opacity { get; set; } = 1.0f;

        /// <summary>
        /// Optional local bounds of the element for debug rendering or hit testing.
        /// </summary>
        public SKRect? LocalBounds { get; set; }

        /// <summary>
        /// Child elements of this element.
        /// </summary>
        public List<SkiaElement> Children { get; } = new List<SkiaElement>();

        /// <summary>
        /// Optional callback invoked during the update pass.  Use this to inject custom
        /// animation logic without subclassing every element.  The parameters are:
        /// <list type="bullet">
        ///   <item><description>The element being updated.</description></item>
        ///   <item><description>Time delta in seconds.</description></item>
        ///   <item><description>The current animation context.</description></item>
        /// </list>
        /// </summary>
        public Action<SkiaElement, double, AnimationContext>? OnUpdate { get; set; } = null;

        /// <summary>
        /// Update the element for the next frame.  Override to animate or update state.
        /// </summary>
        /// <param name="dt">Time delta in seconds.</param>
        /// <param name="context">Animation context containing global state.</param>
        public virtual void Update(double dt, AnimationContext context)
        {
            // Invoke any user-provided update callback before updating children.
            // This allows the element to adjust its own transform or other state prior to
            // propagating the update down the hierarchy.
            OnUpdate?.Invoke(this, dt, context);

            foreach (var child in Children)
            {
                child.Update(dt, context);
            }
        }

        /// <summary>
        /// Render the element onto the given Skia canvas.
        /// </summary>
        /// <param name="canvas">Skia canvas to draw into.</param>
        /// <param name="context">Render context containing global state.</param>
        public abstract void Render(SKCanvas canvas, RenderContext context);

        /// <summary>
        /// Draws debug information for the element, such as its local origin and bounds.
        /// </summary>
        protected virtual void DrawDebug(SKCanvas canvas, RenderContext context)
        {
            if (!context.DebugMode) return;

            // Draw local origin (X-axis in red, Y-axis in green)
            using var originPaint = new SKPaint { StrokeWidth = 2, IsAntialias = true };
            
            originPaint.Color = SKColors.Red;
            canvas.DrawLine(0, 0, 10, 0, originPaint);
            
            originPaint.Color = SKColors.Green;
            canvas.DrawLine(0, 0, 0, 10, originPaint);

            // Draw local bounds if available
            if (LocalBounds.HasValue)
            {
                using var boundsPaint = new SKPaint
                {
                    Color = SKColors.Magenta.WithAlpha(128),
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 1,
                    PathEffect = SKPathEffect.CreateDash(new float[] { 4, 4 }, 0)
                };
                canvas.DrawRect(LocalBounds.Value, boundsPaint);
            }
        }

        /// <summary>
        /// Convert a <see cref="Matrix3x2"/> into an <see cref="SKMatrix"/> for Skia.
        /// </summary>
        protected SKMatrix ToSKMatrix(Matrix3x2 m)
        {
            return new SKMatrix
            {
                ScaleX = m.M11,
                SkewX  = m.M21,
                TransX = m.M31,
                SkewY  = m.M12,
                ScaleY = m.M22,
                TransY = m.M32,
                Persp0 = 0f,
                Persp1 = 0f,
                Persp2 = 1f
            };
        }
    }
}
