using System.Drawing;
using CrystalCatalystLibrary.net;
using CrystalSkia.net;
using SkiaSharp;
using Xunit;

namespace CrystalCatalystLibrary.Tests;

public class SelectionControlHintTests
{
    private static CrystalSelectionControl Create(string[] items, string text = "") =>
        new(new RectangleF(0, 0, 300, CrystalSelectionControl.CalculateHeight(items.Length, 2, 300)),
            items, text, new[] { "OK", "Cancel" }, new SKFont(SKTypeface.Default, 14));

    [Fact]
    public void SelectionChanged_ReportsOnlyResolvedChangesAcrossInputPaths()
    {
        var control = Create(new[] { "a", "b", "b" });
        var changes = new List<(string, int)>();
        control.SelectionHint += (text, index) => changes.Add((text, index));
        control.TextInput("a");
        control.KeyDown(KeyCode.Down, false, false);
        control.PointerDown(new PointF(20, 106), CrystalMouseButton.Left);
        control.Editor.Text = "other";
        control.Editor.Text = "another";
        control.PointerDown(new PointF(20, 50), CrystalMouseButton.Left);
        control.PointerDown(new PointF(20, 50), CrystalMouseButton.Left);
        Assert.Equal(new[] { ("a", 0), ("b", 1), ("other", -1), ("a", 0) }, changes);
    }

    [Fact]
    public void DisabledButton_DoesNotActivateAndOtherButtonStillWorks()
    {
        var control = Create(new[] { "a" }, "a");
        var labels = new List<string>();
        control.ButtonActivated += e => labels.Add(e.ButtonLabel);
        Assert.True(control.IsButtonEnabled(0));
        control.SetButtonEnabled(0, false);
        float y = control.Bounds.Height - 20;
        control.PointerMove(new PointF(50, y));
        control.PointerDown(new PointF(50, y), CrystalMouseButton.Left);
        control.KeyDown(KeyCode.Enter, false, false);
        control.PointerDown(new PointF(240, y), CrystalMouseButton.Left);
        Assert.Equal(new[] { "Cancel" }, labels);
        control.SetButtonEnabled(0, true);
        control.PointerDown(new PointF(50, y), CrystalMouseButton.Left);
        Assert.Equal(new[] { "Cancel", "OK" }, labels);
        Assert.Throws<ArgumentOutOfRangeException>(() => control.SetButtonEnabled(2, false));
    }

    [Fact]
    public void Resize_UpdatesScrollbarBoundsAndViewportImmediately()
    {
        var control = Create(Enumerable.Range(0, 20).Select(i => i.ToString()).ToArray());
        control.Bounds = new RectangleF(10, 20, 200, 148);
        var field = typeof(CrystalSelectionControl).GetField("scrollBar", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var scrollBar = (CrystalScrollBar)field.GetValue(control)!;
        Assert.Equal(new RectangleF(182, 8, 10, 132), scrollBar.Bounds);
        Assert.Equal(0.1f, scrollBar.ViewportRatio);
    }

    [Fact]
    public void EmptyList_PageNavigationDoesNotThrow()
    {
        var control = Create(Array.Empty<string>());
        control.KeyDown(KeyCode.PageUp, false, false);
        control.KeyDown(KeyCode.PageDown, false, false);
        Assert.Equal(-1, control.SelectedIndex);
    }

    [Fact]
    public void Keyboard_CyclesEnabledButtonsAndEditorInBothDirections()
    {
        var control = Create(new[] { "a", "b" }, "a");
        var activated = new List<SelectionButtonEventArgs>();
        control.ButtonActivated += activated.Add;
        control.SetFocus(true);
        control.SetButtonEnabled(0, false);
        control.KeyDown(KeyCode.Tab, false, false);
        Assert.True(control.IsFocused);
        control.KeyDown(KeyCode.Enter, false, false);
        Assert.Equal("Cancel", Assert.Single(activated).ButtonLabel);
        Assert.Equal("a", activated[0].CurrentText);
        Assert.Equal(0, activated[0].SelectedIndex);
        control.KeyDown(KeyCode.Tab, false, false);
        control.KeyDown(KeyCode.Down, false, false);
        Assert.Equal("b", control.ResultText);
        control.KeyDown(KeyCode.Tab, true, false);
        control.KeyDown(KeyCode.Enter, false, false);
        Assert.Equal(2, activated.Count);
        control.SetButtonEnabled(1, false);
        control.KeyDown(KeyCode.Enter, false, false);
        Assert.Equal(2, activated.Count);
        control.TextInput("x");
        Assert.Equal("bx", control.ResultText);
    }

    [Fact]
    public void Keyboard_NoEnabledButtonsKeepsEditorAndNeverActivates()
    {
        var control = Create(Array.Empty<string>());
        var activated = new List<SelectionButtonEventArgs>();
        control.ButtonActivated += activated.Add;
        control.SetFocus(true);
        control.SetButtonEnabled(0, false);
        control.SetButtonEnabled(1, false);
        control.KeyDown(KeyCode.Tab, false, false);
        control.TextInput("x");
        control.KeyDown(KeyCode.Enter, false, false);
        Assert.Equal("x", control.ResultText);
        Assert.Empty(activated);
    }

    [Fact]
    public void PointerEditor_RestoresFocusAfterKeyboardButtonFocus()
    {
        var control = Create(new[] { "a" }, "a");
        control.SetFocus(true);
        control.KeyDown(KeyCode.Tab, false, false);
        Assert.False(control.Editor.IsFocused);
        control.PointerDown(new PointF(20, 20), CrystalMouseButton.Left);
        Assert.True(control.Editor.IsFocused);
        Assert.True(control.IsFocused);
    }

    [Fact]
    public void ShortBounds_UseFittingRowsForButtonsAndPaging()
    {
        var control = Create(Enumerable.Range(0, 20).Select(i => i.ToString()).ToArray());
        control.Bounds = new RectangleF(0, 0, 300, 148);
        var activated = new List<string>();
        control.ButtonActivated += e => activated.Add(e.ButtonLabel);
        control.KeyDown(KeyCode.PageDown, false, false);
        Assert.Equal(1, control.SelectedIndex);
        control.KeyDown(KeyCode.PageDown, false, false);
        Assert.Equal(3, control.SelectedIndex);
        control.PointerDown(new PointF(50, 125), CrystalMouseButton.Left);
        Assert.Equal(new[] { "OK" }, activated);
        Assert.True(control.ScrollOffset > 0);
    }
}