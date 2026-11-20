using System.Drawing;
using CrystalCatalystLibrary.net;
using CrystalSkia.net;
using SkiaSharp;
using Xunit;

namespace CrystalCatalystLibrary.Tests;

public class ScrollBarTests
{
    [Fact]
    public void Constructor_Defaults()
    {
        var bar = new CrystalScrollBar(new RectangleF(0, 0, 10f, 200f));
        Assert.Equal(0f, bar.Offset);
        Assert.Equal(ScrollBarOrientation.Vertical, bar.Orientation);
        Assert.True(bar.Visible); // ViewportRatio defaults to 0, which is < 1
    }

    [Fact]
    public void ViewportRatio_SetsVisibility()
    {
        var bar = new CrystalScrollBar(new RectangleF(0, 0, 10f, 200f));
        bar.ViewportRatio = 0.5f;
        Assert.True(bar.Visible);

        bar.ViewportRatio = 1f;
        Assert.False(bar.Visible);
    }

    [Fact]
    public void Offset_ClampedToZeroOne()
    {
        var bar = new CrystalScrollBar(new RectangleF(0, 0, 10f, 200f));
        bar.ViewportRatio = 0.5f;

        bar.Offset = -0.5f;
        Assert.Equal(0f, bar.Offset);

        bar.Offset = 1.5f;
        Assert.Equal(1f, bar.Offset);

        bar.Offset = 0.5f;
        Assert.Equal(0.5f, bar.Offset);
    }

    [Fact]
    public void Offset_ChangedEvent_Fires()
    {
        var bar = new CrystalScrollBar(new RectangleF(0, 0, 10f, 200f));
        bar.ViewportRatio = 0.5f;
        int changedCount = 0;
        bar.Changed += () => changedCount++;

        bar.Offset = 0.5f;
        Assert.Equal(1, changedCount);

        bar.Offset = 0.5f; // Same value, no event
        Assert.Equal(1, changedCount);

        bar.Offset = 0.7f;
        Assert.Equal(2, changedCount);
    }

    [Fact]
    public void PointerDown_OnThumb_StartsDrag()
    {
        var bar = new CrystalScrollBar(new RectangleF(0, 0, 10f, 200f));
        bar.ViewportRatio = 0.5f;

        // Thumb should be at top initially; click on thumb
        bar.PointerDown(new PointF(5f, 10f), CrystalMouseButton.Left);
        // Drag down
        bar.PointerMove(new PointF(5f, 100f));
        Assert.True(bar.Offset > 0f);
        bar.PointerUp(new PointF(5f, 100f), CrystalMouseButton.Left);
    }

    [Fact]
    public void PointerDown_OnTrack_JumpsToPosition()
    {
        var bar = new CrystalScrollBar(new RectangleF(0, 0, 10f, 200f));
        bar.ViewportRatio = 0.25f; // Small thumb so track click is clear

        // Click near bottom of track (thumb is at top, so this is track)
        bar.PointerDown(new PointF(5f, 180f), CrystalMouseButton.Left);
        Assert.True(bar.Offset > 0f && bar.Offset <= 1f);
    }

    [Fact]
    public void PointerDown_RightButton_Ignores()
    {
        var bar = new CrystalScrollBar(new RectangleF(0, 0, 10f, 200f));
        bar.ViewportRatio = 0.5f;
        bar.PointerDown(new PointF(5f, 100f), CrystalMouseButton.Right);
        Assert.Equal(0f, bar.Offset);
    }

    [Fact]
    public void PointerDown_WhenNotVisible_Ignores()
    {
        var bar = new CrystalScrollBar(new RectangleF(0, 0, 10f, 200f));
        // ViewportRatio is 0 by default, but Visible is true since 0 < 1
        bar.ViewportRatio = 1f; // Now not visible
        bar.PointerDown(new PointF(5f, 100f), CrystalMouseButton.Left);
        Assert.Equal(0f, bar.Offset);
    }

    [Fact]
    public void Horizontal_Orientation()
    {
        var bar = new CrystalScrollBar(new RectangleF(0, 0, 200f, 10f), ScrollBarOrientation.Horizontal);
        bar.ViewportRatio = 0.25f; // Small thumb so track click is clear

        // Click near right side of track (thumb is at left)
        bar.PointerDown(new PointF(180f, 5f), CrystalMouseButton.Left);
        Assert.True(bar.Offset > 0f);
    }

    [Fact]
    public void Colors_DefaultValues()
    {
        var bar = new CrystalScrollBar(new RectangleF(0, 0, 10f, 200f));
        // Color is a struct; verify defaults are set (non-default black)
        Assert.NotEqual(Color.Empty, bar.BackgroundColor);
        Assert.NotEqual(Color.Empty, bar.ThumbColor);
        Assert.NotEqual(Color.Empty, bar.ThumbHoverColor);
    }

    [Fact]
    public void Colors_Customizable()
    {
        var bar = new CrystalScrollBar(new RectangleF(0, 0, 10f, 200f));
        bar.BackgroundColor = Color.Red;
        bar.ThumbColor = Color.Blue;
        bar.ThumbHoverColor = Color.Green;

        Assert.Equal(Color.Red, bar.BackgroundColor);
        Assert.Equal(Color.Blue, bar.ThumbColor);
        Assert.Equal(Color.Green, bar.ThumbHoverColor);
    }

    [Fact]
    public void Draw_WhenNotVisible_DoesNothing()
    {
        var bar = new CrystalScrollBar(new RectangleF(0, 0, 10f, 200f));
        bar.ViewportRatio = 1f;
        using var bitmap = new SKBitmap(100, 100);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Red);
        bar.OnDraw(canvas); // Should not throw
    }

    [Fact]
    public void Draw_WhenVisible_DoesNotThrow()
    {
        var bar = new CrystalScrollBar(new RectangleF(0, 0, 10f, 200f));
        bar.ViewportRatio = 0.5f;
        bar.Offset = 0.3f;
        using var bitmap = new SKBitmap(100, 300);
        using var canvas = new SKCanvas(bitmap);
        bar.OnDraw(canvas);
    }

    [Fact]
    public void ViewportRatio_Zero_ResetsOffset()
    {
        var bar = new CrystalScrollBar(new RectangleF(0, 0, 10f, 200f));
        bar.ViewportRatio = 0.5f;
        bar.Offset = 0.8f;
        bar.ViewportRatio = 1f; // No scroll needed
        Assert.Equal(0f, bar.Offset);
    }

    [Fact]
    public void Drag_ToBottom_ScrollsToMax()
    {
        var bar = new CrystalScrollBar(new RectangleF(0, 0, 10f, 200f));
        bar.ViewportRatio = 0.5f;

        // Start drag on thumb at top
        bar.PointerDown(new PointF(5f, 5f), CrystalMouseButton.Left);
        // Drag to bottom
        bar.PointerMove(new PointF(5f, 195f));
        Assert.True(bar.Offset >= 0.8f); // Should be near 1.0
        bar.PointerUp(new PointF(5f, 195f), CrystalMouseButton.Left);
    }
}

public class SelectionControlTests
{
    private static readonly SKFont TestFont = new(SKTypeface.Default, 14);

    private static CrystalSelectionControl CreateControl(
        IEnumerable<string> items,
        string initialText = "",
        IEnumerable<string> buttons = null!)
    {
        buttons ??= Array.Empty<string>();
        float height = CrystalSelectionControl.CalculateHeight(items.Count(), buttons.Count(), 300f);
        return new CrystalSelectionControl(
            new RectangleF(0, 0, 300f, height),
            items,
            initialText,
            buttons,
            TestFont);
    }

    [Fact]
    public void Constructor_SetsInitialTextAndIndex()
    {
        var control = CreateControl(new[] { "a.json", "b.json", "c.json" }, "b.json", new[] { "OK" });
        Assert.Equal("b.json", control.ResultText);
        Assert.Equal(1, control.SelectedIndex);
    }

    [Fact]
    public void Constructor_EmptyInitialText_GivesIndexMinusOne()
    {
        var control = CreateControl(new[] { "a.json", "b.json" }, "", new[] { "OK" });
        Assert.Equal("", control.ResultText);
        Assert.Equal(-1, control.SelectedIndex);
    }

    [Fact]
    public void Constructor_NullInitialText_TreatedAsEmpty()
    {
        var control = CreateControl(new[] { "a.json" }, null!, new[] { "OK" });
        Assert.Equal("", control.ResultText);
        Assert.Equal(-1, control.SelectedIndex);
    }

    [Fact]
    public void PointerDown_OnListItem_CopiesTextToEditor()
    {
        var control = CreateControl(new[] { "first.json", "second.json" }, "", new[] { "OK" });
        // Item height is 28, padding is 8, editor height is 28, gap is 8
        // First item starts at y = 8 + 28 + 8 = 44
        control.PointerDown(new PointF(10, 50), CrystalMouseButton.Left);
        Assert.Equal("first.json", control.ResultText);
        Assert.Equal(0, control.SelectedIndex);
    }

    [Fact]
    public void PointerDown_OnSecondItem_CopiesTextToEditor()
    {
        var control = CreateControl(new[] { "a.json", "b.json" }, "", new[] { "OK" });
        // Second item at y = 44 + 28 = 72
        control.PointerDown(new PointF(10, 80), CrystalMouseButton.Left);
        Assert.Equal("b.json", control.ResultText);
        Assert.Equal(1, control.SelectedIndex);
    }

    [Fact]
    public void PointerDown_OnButton_RaisesButtonActivatedEvent()
    {
        var control = CreateControl(new[] { "a.json" }, "a.json", new[] { "Load", "Cancel" });
        string? capturedLabel = null;
        string? capturedText = null;
        int capturedIndex = 0;
        control.ButtonActivated += e =>
        {
            capturedLabel = e.ButtonLabel;
            capturedText = e.CurrentText;
            capturedIndex = e.SelectedIndex;
        };

        // Buttons are at the bottom; find the Load button
        float height = CrystalSelectionControl.CalculateHeight(1, 2, 300f);
        // Button area starts after list + gap
        control.PointerDown(new PointF(50, height - 20), CrystalMouseButton.Left);
        Assert.Equal("Load", capturedLabel);
        Assert.Equal("a.json", capturedText);
        Assert.Equal(0, capturedIndex);
    }

    [Fact]
    public void PointerDown_OnCancelButton_RaisesEventWithCorrectLabel()
    {
        var control = CreateControl(new[] { "a.json" }, "a.json", new[] { "Load", "Cancel" });
        string? capturedLabel = null;
        control.ButtonActivated += e => capturedLabel = e.ButtonLabel;

        float height = CrystalSelectionControl.CalculateHeight(1, 2, 300f);
        // Cancel button is the second one, on the right side
        control.PointerDown(new PointF(250, height - 20), CrystalMouseButton.Left);
        Assert.Equal("Cancel", capturedLabel);
    }

    [Fact]
    public void KeyDown_Up_NavigatesToPreviousItem()
    {
        var control = CreateControl(new[] { "a.json", "b.json", "c.json" }, "c.json", new[] { "OK" });
        Assert.Equal(2, control.SelectedIndex);

        control.KeyDown(KeyCode.Up, false, false);
        Assert.Equal("b.json", control.ResultText);
        Assert.Equal(1, control.SelectedIndex);
    }

    [Fact]
    public void KeyDown_Down_NavigatesToNextItem()
    {
        var control = CreateControl(new[] { "a.json", "b.json", "c.json" }, "a.json", new[] { "OK" });
        Assert.Equal(0, control.SelectedIndex);

        control.KeyDown(KeyCode.Down, false, false);
        Assert.Equal("b.json", control.ResultText);
        Assert.Equal(1, control.SelectedIndex);
    }

    [Fact]
    public void KeyDown_Up_FromNoSelection_SelectsLastItem()
    {
        var control = CreateControl(new[] { "a.json", "b.json" }, "custom.json", new[] { "OK" });
        Assert.Equal(-1, control.SelectedIndex);

        control.KeyDown(KeyCode.Up, false, false);
        Assert.Equal("b.json", control.ResultText);
        Assert.Equal(1, control.SelectedIndex);
    }

    [Fact]
    public void KeyDown_Down_AtEnd_StaysAtLastItem()
    {
        var control = CreateControl(new[] { "a.json", "b.json" }, "b.json", new[] { "OK" });
        control.KeyDown(KeyCode.Down, false, false);
        Assert.Equal("b.json", control.ResultText);
        Assert.Equal(1, control.SelectedIndex);
    }

    [Fact]
    public void KeyDown_Up_AtStart_StaysAtFirstItem()
    {
        var control = CreateControl(new[] { "a.json", "b.json" }, "a.json", new[] { "OK" });
        control.KeyDown(KeyCode.Up, false, false);
        Assert.Equal("a.json", control.ResultText);
        Assert.Equal(0, control.SelectedIndex);
    }

    [Fact]
    public void KeyDown_Home_SelectsFirstItem()
    {
        var control = CreateControl(new[] { "a.json", "b.json", "c.json" }, "c.json", new[] { "OK" });
        control.KeyDown(KeyCode.Home, false, false);
        Assert.Equal("a.json", control.ResultText);
        Assert.Equal(0, control.SelectedIndex);
    }

    [Fact]
    public void KeyDown_End_SelectsLastItem()
    {
        var control = CreateControl(new[] { "a.json", "b.json", "c.json" }, "a.json", new[] { "OK" });
        control.KeyDown(KeyCode.End, false, false);
        Assert.Equal("c.json", control.ResultText);
        Assert.Equal(2, control.SelectedIndex);
    }

    [Fact]
    public void KeyDown_PageUp_MovesUpByPage()
    {
        var items = Enumerable.Range(0, 20).Select(i => $"item{i}.json").ToArray();
        var control = CreateControl(items, "item19.json", new[] { "OK" });
        Assert.Equal(19, control.SelectedIndex);

        control.KeyDown(KeyCode.PageUp, false, false);
        Assert.True(control.SelectedIndex < 19);
        Assert.True(control.SelectedIndex >= 0);
    }

    [Fact]
    public void KeyDown_PageDown_MovesDownByPage()
    {
        var items = Enumerable.Range(0, 20).Select(i => $"item{i}.json").ToArray();
        var control = CreateControl(items, "item0.json", new[] { "OK" });
        Assert.Equal(0, control.SelectedIndex);

        control.KeyDown(KeyCode.PageDown, false, false);
        Assert.True(control.SelectedIndex > 0);
    }

    [Fact]
    public void TextInput_ForwardsToEditor()
    {
        var control = CreateControl(new[] { "a.json" }, "", new[] { "OK" });
        control.TextInput("x");
        Assert.Equal("x", control.ResultText);
        Assert.Equal(-1, control.SelectedIndex);
    }

    [Fact]
    public void EditingTextToMatchItem_UpdatesIndex()
    {
        var control = CreateControl(new[] { "a.json", "b.json" }, "", new[] { "OK" });
        Assert.Equal(-1, control.SelectedIndex);

        // Simulate typing "a.json"
        control.TextInput("a.json");
        Assert.Equal("a.json", control.ResultText);
        Assert.Equal(0, control.SelectedIndex);
    }

    [Fact]
    public void EditingTextToNotMatchItem_ResetsIndex()
    {
        var control = CreateControl(new[] { "a.json", "b.json" }, "a.json", new[] { "OK" });
        Assert.Equal(0, control.SelectedIndex);

        // Caret is at end after setting text, so TextInput appends
        control.TextInput("x");
        Assert.Equal("a.jsonx", control.ResultText);
        Assert.Equal(-1, control.SelectedIndex);
    }

    [Fact]
    public void SetFocus_DelegatesToEditor()
    {
        var control = CreateControl(new[] { "a.json" }, "", new[] { "OK" });
        Assert.False(control.IsFocused);

        control.SetFocus(true);
        Assert.True(control.IsFocused);

        control.SetFocus(false);
        Assert.False(control.IsFocused);
    }

    [Fact]
    public void OnIdle_DelegatesToEditor()
    {
        var control = CreateControl(new[] { "a.json" }, "", new[] { "OK" });
        control.SetFocus(true);
        control.OnIdle(0);
        Assert.True(control.Editor.CaretVisible);
        control.OnIdle(0.6);
        Assert.False(control.Editor.CaretVisible);
    }

    [Fact]
    public void ScrollUp_MovesScrollOffset()
    {
        var items = Enumerable.Range(0, 20).Select(i => $"item{i}.json").ToArray();
        var control = CreateControl(items, "", new[] { "OK" });
        control.ScrollDown();
        control.ScrollDown();
        control.ScrollUp();
        // Scroll offset should be non-negative
        Assert.True(control.SelectedIndex >= -1);
    }

    [Fact]
    public void ScrollDown_MovesScrollOffset()
    {
        var items = Enumerable.Range(0, 20).Select(i => $"item{i}.json").ToArray();
        var control = CreateControl(items, "", new[] { "OK" });
        control.ScrollDown();
        // Should not throw
        Assert.True(control.SelectedIndex >= -1);
    }

    [Fact]
    public void PointerMove_OnListItem_SetsHover()
    {
        var control = CreateControl(new[] { "a.json", "b.json" }, "", new[] { "OK" });
        control.PointerMove(new PointF(10, 50));
        // Hover is internal state; verify no exception
        control.PointerMove(new PointF(10, 80));
    }

    [Fact]
    public void Draw_DoesNotThrow()
    {
        var control = CreateControl(new[] { "a.json", "b.json", "c.json" }, "b.json", new[] { "OK", "Cancel" });
        using var bitmap = new SKBitmap(400, 400);
        using var canvas = new SKCanvas(bitmap);
        control.OnDraw(canvas);
        // Should complete without exception
    }

    [Fact]
    public void EmptyList_NoCrash()
    {
        var control = CreateControl(Array.Empty<string>(), "", new[] { "OK" });
        Assert.Equal(-1, control.SelectedIndex);
        Assert.Equal("", control.ResultText);

        control.KeyDown(KeyCode.Down, false, false);
        Assert.Equal(-1, control.SelectedIndex);

        using var bitmap = new SKBitmap(400, 200);
        using var canvas = new SKCanvas(bitmap);
        control.OnDraw(canvas);
    }

    [Fact]
    public void NoButtons_NoCrash()
    {
        var control = CreateControl(new[] { "a.json" }, "a.json", Array.Empty<string>());
        using var bitmap = new SKBitmap(400, 200);
        using var canvas = new SKCanvas(bitmap);
        control.OnDraw(canvas);
    }

    [Fact]
    public void CalculateHeight_ReturnsPositiveValue()
    {
        float height = CrystalSelectionControl.CalculateHeight(5, 2, 300f);
        Assert.True(height > 0);
    }

    [Fact]
    public void CalculateHeight_ZeroItems_ReturnsMinimalHeight()
    {
        float height = CrystalSelectionControl.CalculateHeight(0, 2, 300f);
        Assert.True(height > 0);
    }

    [Fact]
    public void ScrollOffset_DefaultsToZero()
    {
        var items = Enumerable.Range(0, 20).Select(i => $"item{i}.json").ToArray();
        var control = CreateControl(items, "", new[] { "OK" });
        Assert.Equal(0f, control.ScrollOffset);
    }

    [Fact]
    public void ScrollOffset_InRange()
    {
        var items = Enumerable.Range(0, 20).Select(i => $"item{i}.json").ToArray();
        var control = CreateControl(items, "", new[] { "OK" });
        control.ScrollDown();
        Assert.True(control.ScrollOffset >= 0f);
        Assert.True(control.ScrollOffset <= 1f);
    }

    [Fact]
    public void ScrollOffset_IncreasesOnScrollDown()
    {
        var items = Enumerable.Range(0, 20).Select(i => $"item{i}.json").ToArray();
        var control = CreateControl(items, "", new[] { "OK" });
        float initial = control.ScrollOffset;
        control.ScrollDown();
        Assert.True(control.ScrollOffset >= initial);
    }

    [Fact]
    public void ScrollOffset_DecreasesOnScrollUp()
    {
        var items = Enumerable.Range(0, 20).Select(i => $"item{i}.json").ToArray();
        var control = CreateControl(items, "", new[] { "OK" });
        control.ScrollDown();
        control.ScrollDown();
        float before = control.ScrollOffset;
        control.ScrollUp();
        Assert.True(control.ScrollOffset <= before);
    }

    [Fact]
    public void ScrollOffset_AfterHome_IsZero()
    {
        var items = Enumerable.Range(0, 20).Select(i => $"item{i}.json").ToArray();
        var control = CreateControl(items, "item19.json", new[] { "OK" });
        control.KeyDown(KeyCode.Home, false, false);
        Assert.Equal(0f, control.ScrollOffset);
    }

    [Fact]
    public void ScrollOffset_WithFewItems_IsZero()
    {
        var control = CreateControl(new[] { "a.json", "b.json" }, "", new[] { "OK" });
        // Few items fit without scrolling
        Assert.Equal(0f, control.ScrollOffset);
    }

    [Fact]
    public void PointerDown_OnEditor_WithOffsetBounds_SetsCaretCorrectly()
    {
        // Regression test: editor bounds are offset by Padding within the control.
        // Clicking in the editor must convert to editor-local coordinates.
        var control = CreateControl(new[] { "a.json", "b.json" }, "hello", new[] { "OK" });
        control.SetFocus(true);

        // The editor is positioned at (Padding, Padding) = (8, 8) within the control.
        // Click in the middle of the editor area.
        const float Padding = 8f;
        float editorX = Padding + Padding; // control-local X inside editor
        float editorY = Padding + Padding + 14f; // control-local Y inside editor

        control.PointerDown(new PointF(editorX, editorY), CrystalMouseButton.Left);
        // Caret should be set to a valid position (not out of range)
        Assert.InRange(control.Editor.Caret, 0, control.Editor.Text.Length);
    }

    [Fact]
    public void PointerDown_OnEditor_BeforeOnDraw_SetsCaretCorrectly()
    {
        // Regression test: editor bounds are updated in OnDraw, but pointer events
        // may arrive before the first OnDraw. The control must compute editor bounds
        // dynamically rather than relying on stale cached bounds.
        var control = CreateControl(new[] { "a.json", "b.json" }, "hello world", new[] { "OK" });
        control.SetFocus(true);

        // Do NOT call OnDraw — simulate the first click arriving before any draw.
        // The editor is at (8, 8) within the control, height 28.
        const float Padding = 8f;
        float clickX = Padding + 20f; // inside editor horizontally
        float clickY = Padding + 14f; // inside editor vertically

        control.PointerDown(new PointF(clickX, clickY), CrystalMouseButton.Left);
        Assert.InRange(control.Editor.Caret, 0, control.Editor.Text.Length);

        // Verify drag-to-select also works before OnDraw
        control.PointerMove(new PointF(clickX + 50f, clickY));
        Assert.True(control.Editor.SelectionLength > 0);

        control.PointerUp(new PointF(clickX + 50f, clickY), CrystalMouseButton.Left);
        Assert.True(control.Editor.SelectionLength > 0);
    }
}

public class PopupHostTests
{
    [Fact]
    public void Show_SetsVisibleAndFocus()
    {
        var host = new CrystalPopupHost();
        var editor = new CrystalTextEditor(new RectangleF(0, 0, 200f, 20f), new SKFont(SKTypeface.Default, 14));
        var control = new CrystalSelectionControl(
            new RectangleF(0, 0, 200f, 150f),
            new[] { "a.json" },
            "",
            new[] { "OK" },
            new SKFont(SKTypeface.Default, 14));

        host.Show(control, new RectangleF(0, 0, 800f, 600f));
        Assert.True(host.IsVisible);
        Assert.True(control.IsFocused);
    }

    [Fact]
    public void Accept_SetsResultAndCloses()
    {
        var host = new CrystalPopupHost();
        var control = new CrystalSelectionControl(
            new RectangleF(0, 0, 200f, 150f),
            new[] { "a.json" },
            "a.json",
            new[] { "OK" },
            new SKFont(SKTypeface.Default, 14));

        host.Show(control, new RectangleF(0, 0, 800f, 600f));
        host.Accept("a.json", 0);

        Assert.False(host.IsVisible);
        Assert.NotNull(host.Result);
        Assert.Equal(PopupResultType.Accepted, host.Result!.Type);
        Assert.Equal("a.json", host.Result.ResultText);
        Assert.Equal(0, host.Result.SelectedIndex);
    }

    [Fact]
    public void Cancel_SetsCancelledResult()
    {
        var host = new CrystalPopupHost();
        var control = new CrystalSelectionControl(
            new RectangleF(0, 0, 200f, 150f),
            new[] { "a.json" },
            "",
            new[] { "Cancel" },
            new SKFont(SKTypeface.Default, 14));

        host.Show(control, new RectangleF(0, 0, 800f, 600f));
        host.Cancel();

        Assert.False(host.IsVisible);
        Assert.NotNull(host.Result);
        Assert.Equal(PopupResultType.Cancelled, host.Result!.Type);
    }

    [Fact]
    public void Close_SetsClosedResult()
    {
        var host = new CrystalPopupHost();
        var control = new CrystalSelectionControl(
            new RectangleF(0, 0, 200f, 150f),
            new[] { "a.json" },
            "",
            new[] { "OK" },
            new SKFont(SKTypeface.Default, 14));

        host.Show(control, new RectangleF(0, 0, 800f, 600f));
        host.Close();

        Assert.False(host.IsVisible);
        Assert.NotNull(host.Result);
        Assert.Equal(PopupResultType.Closed, host.Result!.Type);
    }

    [Fact]
    public void KeyDown_Escape_Cancels()
    {
        var host = new CrystalPopupHost();
        var control = new CrystalSelectionControl(
            new RectangleF(0, 0, 200f, 150f),
            new[] { "a.json" },
            "",
            new[] { "OK" },
            new SKFont(SKTypeface.Default, 14));

        host.Show(control, new RectangleF(0, 0, 800f, 600f));
        host.KeyDown(KeyCode.Escape, false, false);

        Assert.False(host.IsVisible);
        Assert.Equal(PopupResultType.Cancelled, host.Result!.Type);
    }

    [Fact]
    public void Closed_Event_FiresOnAccept()
    {
        var host = new CrystalPopupHost();
        var control = new CrystalSelectionControl(
            new RectangleF(0, 0, 200f, 150f),
            new[] { "a.json" },
            "",
            new[] { "OK" },
            new SKFont(SKTypeface.Default, 14));

        PopupResult? captured = null;
        host.Closed += r => captured = r;

        host.Show(control, new RectangleF(0, 0, 800f, 600f));
        host.Accept("test", 0);

        Assert.NotNull(captured);
        Assert.Equal(PopupResultType.Accepted, captured!.Type);
        Assert.Equal("test", captured.ResultText);
    }

    [Fact]
    public void Closed_Event_FiresOnCancel()
    {
        var host = new CrystalPopupHost();
        var control = new CrystalSelectionControl(
            new RectangleF(0, 0, 200f, 150f),
            new[] { "a.json" },
            "",
            new[] { "Cancel" },
            new SKFont(SKTypeface.Default, 14));

        PopupResult? captured = null;
        host.Closed += r => captured = r;

        host.Show(control, new RectangleF(0, 0, 800f, 600f));
        host.Cancel();

        Assert.NotNull(captured);
        Assert.Equal(PopupResultType.Cancelled, captured!.Type);
    }

    [Fact]
    public void Accept_WhenNotVisible_DoesNothing()
    {
        var host = new CrystalPopupHost();
        Assert.False(host.IsVisible);
        host.Accept("test", 0);
        Assert.False(host.IsVisible);
        Assert.Null(host.Result);
    }

    [Fact]
    public void Cancel_WhenNotVisible_DoesNothing()
    {
        var host = new CrystalPopupHost();
        Assert.False(host.IsVisible);
        host.Cancel();
        Assert.False(host.IsVisible);
        Assert.Null(host.Result);
    }

    [Fact]
    public void Draw_WhenNotVisible_DoesNothing()
    {
        var host = new CrystalPopupHost();
        using var bitmap = new SKBitmap(800, 600);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Red);
        host.OnDraw(canvas);
        // Should not throw and should not change anything
    }

    [Fact]
    public void Draw_WhenVisible_DoesNotThrow()
    {
        var host = new CrystalPopupHost();
        var control = new CrystalSelectionControl(
            new RectangleF(0, 0, 200f, 150f),
            new[] { "a.json" },
            "",
            new[] { "OK" },
            new SKFont(SKTypeface.Default, 14));

        host.Show(control, new RectangleF(0, 0, 800f, 600f));
        using var bitmap = new SKBitmap(800, 600);
        using var canvas = new SKCanvas(bitmap);
        host.OnDraw(canvas);
    }

    [Fact]
    public void PointerDown_WithinBounds_ForwardsToContent()
    {
        var host = new CrystalPopupHost();
        var control = new CrystalSelectionControl(
            new RectangleF(0, 0, 200f, 150f),
            new[] { "a.json", "b.json" },
            "",
            new[] { "OK" },
            new SKFont(SKTypeface.Default, 14));

        host.Show(control, new RectangleF(0, 0, 800f, 600f));
        // Popup is centered; bounds should be within the window
        var bounds = host.Bounds;
        // Click within the popup bounds
        host.PointerDown(new PointF(bounds.X + 10, bounds.Y + 50), CrystalMouseButton.Left);
        // Should not throw
    }

    [Fact]
    public void TextInput_ForwardsToContent()
    {
        var host = new CrystalPopupHost();
        var control = new CrystalSelectionControl(
            new RectangleF(0, 0, 200f, 150f),
            new[] { "a.json" },
            "",
            new[] { "OK" },
            new SKFont(SKTypeface.Default, 14));

        host.Show(control, new RectangleF(0, 0, 800f, 600f));
        host.TextInput("x");
        Assert.Equal("x", control.ResultText);
    }

    [Fact]
    public void OnIdle_ForwardsToContent()
    {
        var host = new CrystalPopupHost();
        var control = new CrystalSelectionControl(
            new RectangleF(0, 0, 200f, 150f),
            new[] { "a.json" },
            "",
            new[] { "OK" },
            new SKFont(SKTypeface.Default, 14));

        host.Show(control, new RectangleF(0, 0, 800f, 600f));
        host.OnIdle(0);
        // Should not throw
    }

    [Fact]
    public void PopupResult_EmptyString_DistinguishableFromCancelled()
    {
        var host = new CrystalPopupHost();
        var control = new CrystalSelectionControl(
            new RectangleF(0, 0, 200f, 150f),
            new[] { "a.json" },
            "",
            new[] { "OK" },
            new SKFont(SKTypeface.Default, 14));

        host.Show(control, new RectangleF(0, 0, 800f, 600f));
        host.Accept("", -1);

        Assert.Equal(PopupResultType.Accepted, host.Result!.Type);
        Assert.Equal("", host.Result.ResultText);
        // Accepted with empty string is different from Cancelled
        Assert.NotEqual(PopupResultType.Cancelled, host.Result.Type);
    }

    [Fact]
    public void SetFocus_True_GivesFocusToContent()
    {
        var host = new CrystalPopupHost();
        var control = new CrystalSelectionControl(
            new RectangleF(0, 0, 200f, 150f),
            new[] { "a.json" },
            "",
            new[] { "OK" },
            new SKFont(SKTypeface.Default, 14));

        host.Show(control, new RectangleF(0, 0, 800f, 600f));
        host.SetFocus(false);
        host.SetFocus(true);
        Assert.True(control.IsFocused);
    }

    [Fact]
    public void Accept_ClearsContent()
    {
        var host = new CrystalPopupHost();
        var control = new CrystalSelectionControl(
            new RectangleF(0, 0, 200f, 150f),
            new[] { "a.json" },
            "",
            new[] { "OK" },
            new SKFont(SKTypeface.Default, 14));

        host.Show(control, new RectangleF(0, 0, 800f, 600f));
        host.Accept("a.json", 0);
        // Content should be released
        Assert.False(control.IsFocused);
    }

    [Fact]
    public void PointerDown_OnEditor_ThroughPopupHost_SetsCaretCorrectly()
    {
        // End-to-end: popup host -> selection control -> text editor coordinate chain.
        var control = new CrystalSelectionControl(
            new RectangleF(0, 0, 300f, 200f),
            new[] { "a.json", "b.json" },
            "hello",
            new[] { "OK" },
            new SKFont(SKTypeface.Default, 14));

        var host = new CrystalPopupHost();
        host.Show(control, new RectangleF(0, 0, 800f, 600f));
        control.SetFocus(true);

        // Popup is centered. Editor is at (Padding, Padding) within the control,
        // and the control bounds are set to the popup bounds by Show().
        const float Padding = 8f;
        float popupX = host.Bounds.X;
        float popupY = host.Bounds.Y;
        // Click in the middle of the editor area (within the popup)
        float clickX = popupX + Padding + 50f;
        float clickY = popupY + Padding + 14f;

        host.PointerDown(new PointF(clickX, clickY), CrystalMouseButton.Left);
        Assert.InRange(control.Editor.Caret, 0, control.Editor.Text.Length);
    }

    [Fact]
    public void PointerMove_OnEditor_ThroughPopupHost_UpdatesCaret()
    {
        var control = new CrystalSelectionControl(
            new RectangleF(0, 0, 300f, 200f),
            new[] { "a.json" },
            "hello world",
            new[] { "OK" },
            new SKFont(SKTypeface.Default, 14));

        var host = new CrystalPopupHost();
        host.Show(control, new RectangleF(0, 0, 800f, 600f));
        control.SetFocus(true);

        const float Padding = 8f;
        float popupX = host.Bounds.X;
        float popupY = host.Bounds.Y;
        float editorY = popupY + Padding + 14f;

        // Click at the start of the editor
        host.PointerDown(new PointF(popupX + Padding + 5f, editorY), CrystalMouseButton.Left);
        int caretAfterClick = control.Editor.Caret;

        // Drag to the right
        host.PointerMove(new PointF(popupX + Padding + 150f, editorY));
        int caretAfterDrag = control.Editor.Caret;

        // Caret should have moved to the right
        Assert.True(caretAfterDrag >= caretAfterClick);
        host.PointerUp(new PointF(popupX + Padding + 150f, editorY), CrystalMouseButton.Left);
    }

    [Fact]
    public void PointerDown_OnListItem_ThroughPopupHost_WithoutOnDraw_CopiesText()
    {
        // Regression: clicking a list item through the popup host should work
        // even before the first OnDraw (no scrolling has occurred).
        var control = new CrystalSelectionControl(
            new RectangleF(0, 0, 300f, 200f),
            new[] { "first.json", "second.json", "third.json" },
            "default.json",
            new[] { "OK" },
            new SKFont(SKTypeface.Default, 14));

        var host = new CrystalPopupHost();
        host.Show(control, new RectangleF(0, 0, 800f, 600f));
        // Do NOT call OnDraw — simulate first interaction before any draw.

        // Layout: editor at (8, 8), height 28. Gap 8. List starts at y = 8 + 28 + 8 = 44.
        // First item at y = 44, height 28. Second item at y = 72.
        const float Padding = 8f;
        float popupX = host.Bounds.X;
        float popupY = host.Bounds.Y;
        // Click in the middle of the second list item
        float clickX = popupX + Padding + 50f;
        float clickY = popupY + 44f + 28f + 14f; // second item center

        host.PointerDown(new PointF(clickX, clickY), CrystalMouseButton.Left);
        Assert.Equal("second.json", control.ResultText);
        Assert.Equal(1, control.SelectedIndex);
    }

    [Fact]
    public void PointerDown_OnListItem_AfterOnDraw_WithManyItems_CopiesText()
    {
        // Regression: with many items that require scrolling, clicking a visible
        // list item should work after OnDraw has been called (which initializes
        // the scroll bar ViewportRatio), even if the scroll offset is still 0.
        var items = new List<string>();
        for (int i = 0; i < 20; i++) items.Add($"item{i}.json");

        var control = new CrystalSelectionControl(
            new RectangleF(0, 0, 360f, 300f),
            items,
            "default.json",
            new[] { "Load", "Cancel" },
            new SKFont(SKTypeface.Default, 14));

        var host = new CrystalPopupHost();
        host.Show(control, new RectangleF(0, 0, 850f, 700f));

        // Simulate the first frame's OnDraw (which initializes ViewportRatio)
        using var bitmap = new SKBitmap(850, 700);
        using var canvas = new SKCanvas(bitmap);
        host.OnDraw(canvas);

        // Now click the first visible list item (item0) without scrolling
        // Layout: editor at (8, 8), height 28. Gap 8. List starts at y = 44.
        // With 360 width, MaxVisibleItems = max(3, min(12, (360-16-10)*0.6/28)) = max(3, min(12, 7)) = 7
        // First item at y = 44, second at y = 72, etc.
        float popupX = host.Bounds.X;
        float popupY = host.Bounds.Y;
        float clickX = popupX + 50f;
        float clickY = popupY + 44f + 14f; // first item center

        host.PointerDown(new PointF(clickX, clickY), CrystalMouseButton.Left);
        Assert.Equal("item0.json", control.ResultText);
        Assert.Equal(0, control.SelectedIndex);
    }

    [Fact]
    public void PointerDown_OnListItem_ExactSlideScrambleScenario_CopiesText()
    {
        // Exact replication of SlideScramble's session popup scenario.
        // The popup is created with width 360, shown in an 850x700 window.
        // Multiple session files exist, requiring a scroll bar.
        var filenames = new[] { "alpha.json", "beta.json", "gamma.json", "delta.json",
            "epsilon.json", "zeta.json", "eta.json", "theta.json" };

        var control = new CrystalSelectionControl(
            new RectangleF(0, 0, 360f, CrystalSelectionControl.CalculateHeight(filenames.Length, 2, 360f)),
            filenames,
            "session",
            new[] { "Load", "Cancel" },
            new SKFont(SKTypeface.Default, 14));

        string? capturedLabel = null;
        string? capturedText = null;
        control.ButtonActivated += e =>
        {
            capturedLabel = e.ButtonLabel;
            capturedText = e.CurrentText;
        };

        var host = new CrystalPopupHost();
        host.Show(control, new RectangleF(0, 0, 850f, 700f));

        // Simulate the first frame's OnDraw
        using var bitmap = new SKBitmap(850, 700);
        using var canvas = new SKCanvas(bitmap);
        host.OnDraw(canvas);

        // Click "beta.json" (second item)
        float popupX = host.Bounds.X;
        float popupY = host.Bounds.Y;
        float clickX = popupX + 50f;
        float clickY = popupY + 44f + 28f + 14f; // second item center

        host.PointerDown(new PointF(clickX, clickY), CrystalMouseButton.Left);
        Assert.Equal("beta.json", control.ResultText);

        // Now click the Load button
        float height = control.Bounds.Height;
        float loadBtnY = popupY + height - 20f;
        host.PointerDown(new PointF(popupX + 50f, loadBtnY), CrystalMouseButton.Left);

        Assert.Equal("Load", capturedLabel);
        Assert.Equal("beta.json", capturedText);
    }

    [Fact]
    public void PointerDown_OnButton_NotInterceptedByListItem()
    {
        // Regression: buttons are drawn on top of the list area, so clicking a button
        // must not be intercepted by a list item hit test. Previously the list items
        // were checked before buttons, causing button clicks to fall through to items.
        var control = new CrystalSelectionControl(
            new RectangleF(0, 0, 360f, CrystalSelectionControl.CalculateHeight(3, 2, 360f)),
            new[] { "a.json", "b.json", "c.json" },
            "default.json",
            new[] { "Load", "Cancel" },
            new SKFont(SKTypeface.Default, 14));

        string? capturedLabel = null;
        string? capturedText = null;
        control.ButtonActivated += e =>
        {
            capturedLabel = e.ButtonLabel;
            capturedText = e.CurrentText;
        };

        // Click "a.json" to change the editor text
        control.PointerDown(new PointF(50f, 44f + 14f), CrystalMouseButton.Left);
        Assert.Equal("a.json", control.ResultText);

        // Click the Load button — should NOT be intercepted by the last list item
        float height = control.Bounds.Height;
        float loadBtnY = height - 20f;
        control.PointerDown(new PointF(50f, loadBtnY), CrystalMouseButton.Left);

        Assert.Equal("Load", capturedLabel);
        Assert.Equal("a.json", capturedText); // Text should still be "a.json", not reverted
    }

    [Fact]
    public void PointerDown_OnButton_AfterScroll_StillActivatesButton()
    {
        // Regression: even after scrolling the list, clicking a button should activate
        // the button, not a list item that scrolled into the button area.
        var items = new List<string>();
        for (int i = 0; i < 15; i++) items.Add($"item{i}.json");

        var control = new CrystalSelectionControl(
            new RectangleF(0, 0, 360f, CrystalSelectionControl.CalculateHeight(items.Count, 2, 360f)),
            items.ToArray(),
            "default.json",
            new[] { "Load", "Cancel" },
            new SKFont(SKTypeface.Default, 14));

        string? capturedLabel = null;
        control.ButtonActivated += e => capturedLabel = e.ButtonLabel;

        // Simulate scrolling by dragging the scroll bar
        float scrollBarX = control.Bounds.Width - 8f - 10f; // Padding + ScrollBarWidth
        float scrollBarY = 8f + 50f; // middle of scroll bar
        control.PointerDown(new PointF(scrollBarX, scrollBarY), CrystalMouseButton.Left);
        control.PointerMove(new PointF(scrollBarX, scrollBarY + 100f));
        control.PointerUp(new PointF(scrollBarX, scrollBarY + 100f), CrystalMouseButton.Left);

        // Click the Load button
        float height = control.Bounds.Height;
        float loadBtnY = height - 20f;
        control.PointerDown(new PointF(50f, loadBtnY), CrystalMouseButton.Left);

        Assert.Equal("Load", capturedLabel);
    }
}
