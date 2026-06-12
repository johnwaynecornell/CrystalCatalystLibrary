using SkiaSharp;

namespace CrystalCatalyst.SkiaScene.Experimental
{
    /// <summary>
    /// A simple container element that renders nothing itself but renders all of its children.
    /// </summary>
    public class SkiaGroup : SkiaElement
    {
        public override void Render(SKCanvas canvas, RenderContext context)
        {
            if (!Visible || Opacity <= 0f)
                return;

            canvas.Save();

            // apply local transform
            var m = ToSKMatrix(Transform);
            canvas.Concat(ref m);

            // Draw debug info for this group
            DrawDebug(canvas, context);

            foreach (var child in Children)
            {
                child.Render(canvas, context);
            }

            canvas.Restore();
        }
    }
}
