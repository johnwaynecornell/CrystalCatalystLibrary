using System;
using SkiaSharp;

namespace CrystalCatalyst.SkiaScene.Experimental
{
    /// <summary>
    /// An element that invokes a user-provided drawing callback during the render pass.  This
    /// allows procedural drawings such as lines, shapes, or other custom Skia commands to be
    /// composed into the retained-mode scene graph without creating a new subclass for each
    /// unique drawing.
    /// </summary>
    public class SkiaProceduralElement : SkiaElement
    {
        /// <summary>
        /// Optional callback invoked during the render pass.  The parameters are:
        /// <list type="bullet">
        ///   <item><description>The canvas to draw into.</description></item>
        ///   <item><description>The current element.</description></item>
        ///   <item><description>The render context containing global state.</description></item>
        /// </list>
        /// </summary>
        public Action<SKCanvas, SkiaProceduralElement, RenderContext>? OnRender { get; set; }

        public override void Render(SKCanvas canvas, RenderContext context)
        {
            if (!Visible || Opacity <= 0f)
                return;

            canvas.Save();
            try
            {
                // Apply local transform
                var m = ToSKMatrix(Transform);
                canvas.Concat(ref m);

                // Draw debug info
                DrawDebug(canvas, context);

                // Invoke custom draw callback if provided
                OnRender?.Invoke(canvas, this, context);

                // Render children
                foreach (var child in Children)
                {
                    child.Render(canvas, context);
                }
            }
            finally
            {
                canvas.Restore();
            }
        }
    }
}