using System.Collections.Generic;
using SkiaSharp;

namespace CrystalCatalyst.SkiaScene.Experimental
{
    /// <summary>
    /// Represents a top-level scene containing a collection of elements.
    /// </summary>
    public class SkiaScene
    {
        /// <summary>
        /// Top-level elements in the scene.  They are rendered in order.
        /// </summary>
        public List<SkiaElement> Elements { get; } = new();

        /// <summary>
        /// Update all elements in the scene.
        /// </summary>
        public void Update(double dt, AnimationContext context)
        {
            foreach (var element in Elements)
            {
                element.Update(dt, context);
            }
        }

        /// <summary>
        /// Render all elements in the scene.
        /// </summary>
        public void Render(SKCanvas canvas, RenderContext context)
        {
            foreach (var element in Elements)
            {
                element.Render(canvas, context);
            }
        }
    }
}
