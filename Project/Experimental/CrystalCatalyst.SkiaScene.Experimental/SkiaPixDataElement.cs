using CrystalCatalystLibrary.net;
using SkiaSharp;

namespace CrystalCatalyst.SkiaScene.Experimental
{
    /// <summary>
    /// Stub for a PixData-backed element.  Implementation depends on the PixData representation.
    /// This element currently does not draw anything but forwards Render calls to its children.
    /// </summary>
    public class SkiaPixDataElement : SkiaElement
    {
        /// <summary>
        /// Arbitrary PixData object.  The experiment does not define its structure yet.
        /// </summary>
        public PixData PixData { get; set; }

        public override void Render(SKCanvas canvas, RenderContext context)
        {
            // Respect visibility and opacity skip rules.
            if (!Visible || Opacity <= 0f)
                return;

            canvas.Save();
            try
            {
                // Apply local transform
                var m = ToSKMatrix(Transform);
                canvas.Concat(ref m);

                // Draw PixData if available by creating a transient bitmap view via CrystalSkia.
                if (PixData != null)
                {
                    // The CrystalSkia.net.PixDataSkia.CreateBitmapView method returns an SKBitmap wrapper
                    // over the PixData buffer without copying.  Dispose the bitmap after use.
                    using var bitmap = CrystalSkia.net.PixDataSkia.CreateBitmapView(PixData);
                    if (bitmap != null)
                    {
                        canvas.DrawBitmap(bitmap, 0, 0);
                    }
                }

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
