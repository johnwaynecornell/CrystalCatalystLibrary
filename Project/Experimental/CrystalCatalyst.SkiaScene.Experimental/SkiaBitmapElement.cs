using SkiaSharp;

namespace CrystalCatalyst.SkiaScene.Experimental
{
    /// <summary>
    /// Renders an SKBitmap.
    /// </summary>
    public class SkiaBitmapElement : SkiaElement
    {
        public SKBitmap? Bitmap { get; set; }

        public override void Render(SKCanvas canvas, RenderContext context)
        {
            if (!Visible)
                return;

            canvas.Save();

            var m = ToSKMatrix(Transform);
            canvas.Concat(ref m);

            if (Opacity >= 0f && Bitmap != null) canvas.DrawBitmap(Bitmap, 0, 0);

            foreach (var child in Children)
            {
                child.Render(canvas, context);
            }

            canvas.Restore();
        }
    }
}
