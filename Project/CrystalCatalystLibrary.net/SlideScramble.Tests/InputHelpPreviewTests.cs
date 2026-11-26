using System.Drawing;
using CrystalCatalystLibrary.net;
using CrystalSkia.net;
using SkiaSharp;
using Xunit;

namespace SlideScramble.Tests;

public class InputHelpPreviewTests
{
    [Fact]
    public void KeyboardAndMousePreviewUntilExplicitActivationOrCancellation()
    {
        using var font = new SKFont(SKTypeface.Default, 14);
        var (menu, help) = CreateMenu(font);
        var host = new CrystalPopupHost();
        var area = new RectangleF(0, 0, 900, 600);
        var enabled = new HashSet<int>();
        menu.Activated += index =>
        {
            host.Accept(selectedIndex: index);
            if (index < 4) enabled.Add(index);
        };
        host.Show(menu, area);
        host.SetLayout(area, help.Layout(area, menu));
        Assert.NotNull(help.Current);
        var slide = help.Current;

        host.KeyDown(KeyCode.Down, false, false);
        Assert.NotNull(help.Current);
        Assert.NotSame(slide, help.Current);
        var shuffle = help.Current;
        var swap = RowPosition(menu, 2);
        host.PointerMove(swap);
        Assert.NotNull(help.Current);
        Assert.NotSame(shuffle, help.Current);
        Assert.Empty(enabled);

        host.PointerMove(RowPosition(menu, 4));
        Assert.Null(help.Current);
        host.PointerDown(swap, CrystalMouseButton.Left);
        Assert.NotNull(help.Current);
        Assert.Empty(enabled);
        host.PointerUp(swap, CrystalMouseButton.Left);
        Assert.Equal(new[] { 2 }, enabled);
        Assert.Null(help.Current);

        host.Show(menu, area);
        Assert.Same(slide, help.Current);
        host.SetFocus(false);
        Assert.Null(help.Current);
        host.SetFocus(true);
        Assert.Same(slide, help.Current);
        host.KeyDown(KeyCode.Escape, false, false);
        Assert.Null(help.Current);
        Assert.Equal(new[] { 2 }, enabled);

        host.Show(menu, area);
        host.KeyDown(KeyCode.Enter, false, false);
        Assert.Equal(0, host.Result!.SelectedIndex);
        Assert.Contains(0, enabled);
        host.Show(menu, area);
        host.KeyDown(KeyCode.Up, false, false);
        host.KeyDown(KeyCode.Enter, false, false);
        Assert.Equal(4, host.Result!.SelectedIndex);
        Assert.Null(help.Current);
        Assert.Equal(2, enabled.Count);
    }

    [Theory]
    [InlineData(900, false)]
    [InlineData(320, true)]
    public void PreviewFitsAndMenuPlacementStaysStable(float width, bool stacked)
    {
        using var font = new SKFont(SKTypeface.Default, 14);
        var (menu, help) = CreateMenu(font);
        var area = new RectangleF(0, 0, width, 450);
        menu.SetFocus(true);
        RectangleF first = help.Layout(area, menu);
        for (int i = 0; i < 4; i++)
        {
            Assert.Equal(first, help.Layout(area, menu));
            var panel = help.Current!;
            Assert.True(area.Contains(panel.Bounds));
            Assert.True(stacked ? panel.Bounds.Bottom <= first.Top : panel.Bounds.Left >= first.Right);
            Assert.True(panel.Measure(panel.Bounds.Width).Height <= panel.Bounds.Height);
            menu.KeyDown(KeyCode.Down, false, false);
        }
        Assert.Null(help.Current);
    }

    private static PointF RowPosition(CrystalCommandMenu menu, int index) =>
        new(menu.Bounds.Left + menu.Bounds.Width / 2,
            menu.Bounds.Top + (42 + 31 * index + 15) * menu.Bounds.Height / menu.PreferredHeight);

    private static (CrystalCommandMenu, InputHelpPreview) CreateMenu(SKFont font)
    {
        string[] ids = { PuzzleOperationIds.Slide, PuzzleOperationIds.Shuffle,
            PuzzleOperationIds.ColumnSwap, PuzzleOperationIds.RowSwap };
        var menu = new CrystalCommandMenu("Live input preferences",
            ids.Append("Back").Select(id => new CrystalMenuEntry(id)), font);
        return (menu, new InputHelpPreview(menu, ids, new AnsiSkiaRenderer { FontSize = 14 }));
    }
}
