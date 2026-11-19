using System.Drawing;
using CrystalCatalystLibrary.net;
using CrystalSkia.net;
using SkiaSharp;
using Xunit;

namespace CrystalCatalystLibrary.Tests;

public class InteractionTests
{
    private static readonly SKFont TestFont = new(SKTypeface.Default, 14);
    private static CrystalTextEditor Editor() => new(new RectangleF(100, 50, 100, 20), TestFont);

    [Fact]
    public void MissingSupplementaryGlyphUsesFallbackForDrawingAndHitTesting()
    {
        const string missing = "\U0010FFFF";
        Assert.False(TestFont.ContainsGlyphs(missing));
        var editor = Editor();
        var requested = new List<string>();
        editor.TypefaceForGlyph = glyph => { requested.Add(glyph); return SKTypeface.Default; };
        editor.Text = "a" + missing + "b";
        using var bitmap = new SKBitmap(220, 100);
        using var canvas = new SKCanvas(bitmap);
        editor.OnDraw(canvas);
        Assert.Contains(missing, requested);
        Assert.DoesNotContain("a", requested);
        requested.Clear();
        float x = TestFont.MeasureText("a") + TestFont.MeasureText(missing) + 1;
        editor.PointerDown(new PointF(x, 0), CrystalMouseButton.Left);
        Assert.Equal(3, editor.Caret);
        Assert.Contains(missing, requested);
    }


    [Fact]
    public void DrawUsesLocalClippedCoordinatesAndConfiguredColors()
    {
        var editor = Editor();
        editor.Text = "abc";
        editor.TextColor = Color.White;
        editor.BackgroundColor = Color.Black;
        editor.AccentColor = Color.Blue;
        editor.SetFocus(true);
        editor.KeyDown(KeyCode.Left, true, false);
        using var bitmap = new SKBitmap(220, 100);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Red);
        int saveCount = canvas.SaveCount;
        ((ICrystalSkia)editor).OnDraw(canvas);
        Assert.Equal(saveCount, canvas.SaveCount);
        Assert.Equal(SKColors.Red, bitmap.GetPixel(99, 55));
        Assert.Equal(SKColors.Red, bitmap.GetPixel(200, 55));
        Assert.Equal(SKColors.Black, bitmap.GetPixel(150, 55));
        Assert.Equal(SKColors.Blue, bitmap.GetPixel(100 + (int)(TestFont.MeasureText("ab") + TestFont.MeasureText("c") / 2), 51));
        var router = new CrystalInteraction();
        router.Add(editor);
        router.OnIdle(0);
        router.OnIdle(.5);
        Assert.False(editor.CaretVisible);
    }

    [Fact]
    public void CaretBlinksWhileFocusedAndRestartsOnInteraction()
    {
        var editor = Editor();
        Assert.False(editor.CaretVisible);
        editor.SetFocus(true);
        Assert.True(editor.CaretVisible);
        editor.OnIdle(10);
        editor.OnIdle(10.5);
        Assert.False(editor.CaretVisible);
        editor.KeyDown(KeyCode.Left, false, false);
        Assert.True(editor.CaretVisible);
        editor.OnIdle(11);
        editor.OnIdle(11.5);
        Assert.False(editor.CaretVisible);
        editor.OnIdle(12);
        Assert.True(editor.CaretVisible);
        editor.OnIdle(12.5);
        editor.TextInput("a");
        Assert.True(editor.CaretVisible);
        editor.OnIdle(13);
        editor.OnIdle(13.5);
        editor.PointerDown(new PointF(0, 0), CrystalMouseButton.Left);
        Assert.True(editor.CaretVisible);
        editor.SetFocus(false);
        editor.OnIdle(14);
        Assert.False(editor.CaretVisible);
        editor.SetFocus(true);
        Assert.True(editor.CaretVisible);
    }

    [Fact]
    public void PointerCoordinatesAreLocalAndDragIsCapturedOutsideBounds()
    {
        var editor = Editor();
        editor.Text = "abcd";
        var router = new CrystalInteraction();
        router.Add(editor);
        router.PointerDown(100 + TestFont.MeasureText("a"), 55, CrystalMouseButton.Left);
        router.PointerMove(250, 55);
        router.PointerUp(250, 55, CrystalMouseButton.Left);
        Assert.True(editor.IsFocused);
        Assert.Equal("bcd", editor.SelectedText);
        router.PointerDown(200, 55, CrystalMouseButton.Left);
        Assert.Null(router.Focused);
        Assert.False(editor.IsFocused);
    }

    [Fact]
    public void LastRegisteredTargetWinsAndRemovalClearsFocus()
    {
        var first = Editor();
        var second = Editor();
        var router = new CrystalInteraction();
        router.Add(first);
        router.Add(second);
        router.PointerDown(100, 50, CrystalMouseButton.Left);
        Assert.Same(second, router.Focused);
        Assert.True(router.Remove(second));
        Assert.False(second.IsFocused);
        router.PointerDown(100, 50, CrystalMouseButton.Left);
        Assert.Same(first, router.Focused);
    }

    [Fact]
    public void EditingUsesTextElementsAndSelection()
    {
        var editor = Editor();
        editor.Text = "A👩‍💻e\u0301B";
        editor.KeyDown(KeyCode.Left, false, false);
        editor.KeyDown(KeyCode.BackSpace, false, false);
        Assert.Equal("A👩‍💻B", editor.Text);
        editor.KeyDown(KeyCode.Left, true, false);
        Assert.Equal("👩‍💻", editor.SelectedText);
        editor.TextInput("x");
        Assert.Equal("AxB", editor.Text);
        editor.KeyDown(KeyCode.A, false, true);
        editor.TextInput("new");
        Assert.Equal("new", editor.Text);
    }

    [Fact]
    public void InvalidInputDoesNotChangeTextOrSelection()
    {
        var editor = Editor();
        editor.Text = "abc";
        editor.KeyDown(KeyCode.A, false, true);
        Assert.Throws<ArgumentException>(() => editor.TextInput("x\ny"));
        Assert.Throws<ArgumentException>(() => editor.Text = "x\u2028y");
        Assert.Equal("abc", editor.SelectedText);
        Assert.Equal("abc", editor.Text);
    }

    [Fact]
    public void CombiningInsertionKeepsCaretAtTextElementBoundary()
    {
        var editor = Editor();
        editor.Text = "ab";
        editor.KeyDown(KeyCode.Home, false, false);
        editor.KeyDown(KeyCode.Right, false, false);
        editor.TextInput("\u0301");
        Assert.Equal(2, editor.Caret);
        editor.KeyDown(KeyCode.BackSpace, false, false);
        Assert.Equal("b", editor.Text);
    }
}