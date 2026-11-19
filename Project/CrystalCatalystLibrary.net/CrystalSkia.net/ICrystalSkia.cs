using CrystalCatalystLibrary.net;
using SkiaSharp;

namespace CrystalSkia.net;

/// <summary>A localized interaction target that draws directly to a Skia canvas.</summary>
public interface ICrystalSkia : ICrystal
{
    void OnDraw(SKCanvas canvas);
}