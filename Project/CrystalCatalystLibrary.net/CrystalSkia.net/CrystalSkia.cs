using System.Text;

namespace CrystalSkia.net;

public class Crystal
{
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