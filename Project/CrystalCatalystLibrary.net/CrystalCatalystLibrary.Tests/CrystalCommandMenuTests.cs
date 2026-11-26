using System.Drawing;
using CrystalCatalystLibrary.net;
using CrystalSkia.net;
using SkiaSharp;
using Xunit;

namespace CrystalCatalystLibrary.Tests;

public class CrystalCommandMenuTests
{
    [Fact]
    public void SelectionChangedTracksInitialKeyboardAndPointerHighlightsAndFocusLoss()
    {
        var menu = CreateMenu();
        var changes = new List<int>();
        menu.SelectionChanged += changes.Add;

        menu.SetFocus(true);
        menu.KeyDown(KeyCode.Down, false, false);
        menu.PointerMove(new PointF(20, 42 + 31 * 2 + 15));
        menu.SetFocus(false);

        Assert.Equal(new[] { 0, 1, 2, -1 }, changes);
        Assert.Equal(-1, menu.SelectedIndex);
    }

    [Fact]
    public void PointerActivationOccursOnceAndLosingFocusClearsSelection()
    {
        var menu = CreateMenu();
        int activatedIndex = -1;
        int activationCount = 0;
        menu.Activated += index =>
        {
            activatedIndex = index;
            activationCount++;
        };
        menu.SetFocus(true);

        var backItem = new PointF(20, 42 + 31 * 2 + 15);
        menu.PointerDown(backItem, CrystalMouseButton.Left);
        menu.PointerUp(backItem, CrystalMouseButton.Left);

        Assert.Equal(2, activatedIndex);
        Assert.Equal(1, activationCount);
        Assert.Equal(2, menu.SelectedIndex);

        menu.SetFocus(false);

        Assert.Equal(-1, menu.SelectedIndex);
    }

    [Fact]
    public void PopupCancellationClearsHighlightedPreset()
    {
        var menu = CreateMenu();
        var changes = new List<int>();
        menu.SelectionChanged += changes.Add;
        var popup = new CrystalPopupHost();
        popup.Show(menu, new RectangleF(0, 0, 800, 600));

        popup.Cancel();

        Assert.Equal(-1, menu.SelectedIndex);
        Assert.Equal(-1, changes[^1]);
    }

    private static CrystalCommandMenu CreateMenu() => new("Rule presets", new[]
    {
        new CrystalMenuEntry("Classic"),
        new CrystalMenuEntry("ShuffleLogic"),
        new CrystalMenuEntry("Back")
    }, new SKFont(SKTypeface.Default, 14));
}
