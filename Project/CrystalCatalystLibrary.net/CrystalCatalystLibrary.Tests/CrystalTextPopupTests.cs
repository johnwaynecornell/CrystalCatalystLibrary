using System.Drawing;
using CrystalCatalystLibrary.net;
using CrystalSkia.net;
using SkiaSharp;
using Xunit;

namespace CrystalCatalystLibrary.Tests;

public class CrystalTextPopupTests
{
    [Fact]
    public void MeasurementIncludesExplicitLinesWrapAndPadding()
    {
        var renderer = new AnsiSkiaRenderer { FontSize = 14 };
        const string line = "Drag to a neighboring symbol.";
        var single = new CrystalTextPopup(line, renderer);
        var multiline = new CrystalTextPopup(line + "\n\nPress Escape.", renderer);
        var padded = new CrystalTextPopup(line, renderer, padding: 20);
        var natural = single.Measure();

        Assert.Equal(natural.Width, multiline.Measure().Width);
        Assert.Equal(2 * (renderer.FontSize + 5), multiline.Measure().Height - natural.Height);
        Assert.Equal(16, padded.Measure().Width - natural.Width);
        Assert.Equal(16, padded.Measure().Height - natural.Height);
        var constrained = multiline.Measure(110);
        Assert.Equal(110, constrained.Width);
        Assert.True(constrained.Height > multiline.Measure().Height);
    }

    [Fact]
    public void ButtonlessPopupDrawsAndEscapeCancels()
    {
        var text = new CrystalTextPopup("First line\nSecond line", new AnsiSkiaRenderer());
        var host = new CrystalPopupHost();
        var area = new RectangleF(0, 0, 180, 100);
        text.ShowModal(host, area);
        using var bitmap = new SKBitmap(180, 100);
        using var canvas = new SKCanvas(bitmap);
        host.OnDraw(canvas);

        Assert.True(area.Contains(host.Bounds));
        Assert.Contains(bitmap.Pixels, pixel => pixel.Red > 180);
        host.KeyDown(KeyCode.Enter, false, false);
        Assert.True(host.IsVisible);
        host.KeyDown(KeyCode.Escape, false, false);
        Assert.False(host.IsVisible);
        Assert.Equal(PopupResultType.Cancelled, host.Result!.Type);
    }

    [Fact]
    public void OptionalButtonsRequireExplicitActivation()
    {
        var host = new CrystalPopupHost();
        var renderer = new AnsiSkiaRenderer();
        var plain = new CrystalTextPopup("Choose", renderer);
        var text = new CrystalTextPopup("Choose", renderer, buttons: new[]
        {
            new CrystalTextPopupButton("First", host.Cancel),
            new CrystalTextPopupButton("Second", () => host.Accept("second"))
        });
        Assert.True(text.Measure().Height > plain.Measure().Height);

        text.ShowModal(host, new RectangleF(0, 0, 400, 200));
        host.KeyDown(KeyCode.Tab, false, false);
        Assert.True(host.IsVisible);
        host.KeyDown(KeyCode.Enter, false, false);
        Assert.Equal(PopupResultType.Accepted, host.Result!.Type);
        Assert.Equal("second", host.Result.ResultText);
    }
}
