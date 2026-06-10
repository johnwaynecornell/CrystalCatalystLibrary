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
            if (!Visible)
                return;

            canvas.Save();

            var m = ToSKMatrix(Transform);
            canvas.Concat(ref m);
            
            if (PixData)
                using (var bitmap = CrystalSkia.net.PixDataSkia.CreateBitmapView(PixData))
                {
                    if (Opacity >= 0f && bitmap != null)
                        canvas.DrawBitmap(bitmap, 0, 0);
                }

            foreach (var child in Children)
            {
                child.Render(canvas, context);
            }
        }
    }
}
