using System.Text;

namespace CrystalSkia.net;

/// <summary>
/// Provides utility methods for Skia-based operations in Crystal.
/// </summary>
public class Crystal
{
    /// <summary>
    /// Loads an SVG from a text string.
    /// </summary>
    public static Svg.Skia.SKSvg SKSvgFromText(string svgText)
    {
        var Ret = new Svg.Skia.SKSvg();
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(svgText)))
        {
            Ret.Load(stream);
        }

        return Ret;
    }
}