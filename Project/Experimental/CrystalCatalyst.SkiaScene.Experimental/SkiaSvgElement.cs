using System.Text;
using CrystalSkia.net;
using SkiaSharp;
using SKSvg = Svg.Skia.SKSvg;

namespace CrystalCatalyst.SkiaScene.Experimental
{
    /// <summary>
    /// Renders an SVG string using Svg.Skia.
    /// </summary>
    public class SkiaSvgElement : SkiaElement
    {
        /// <summary>
        /// SVG markup to render.  This may be loaded and cached on demand.
        /// </summary>
        public string Svg { get; set; } = string.Empty;

        private SKSvg? _parsed;

        public string? lastSvg { get; private set; } = null;
        
        private void EnsureParsed()
        {
            if (lastSvg == Svg)
                return;
            
            lastSvg = Svg;
            _parsed = null;
            
            if (string.IsNullOrEmpty(Svg)) return;
            _parsed = Crystal.SKSvgFromText(Svg);
        }

        public override void Render(SKCanvas canvas, RenderContext context)
        {
            if (!Visible || Opacity <= 0f)
                return;

            canvas.Save();

            var m = ToSKMatrix(Transform);
            canvas.Concat(ref m);

            EnsureParsed();

            if (_parsed?.Picture != null)
            {
                canvas.DrawPicture(_parsed.Picture);
            }

            foreach (var child in Children)
            {
                child.Render(canvas, context);
            }

            canvas.Restore();
        }
    }
}
