using CrystalCatalystLibrary.net;
using SlideScramble;
using Xunit;

namespace SlideScramble.Tests;

public class ShortcutKeysTests
{
    [Theory]
    [InlineData(KeyCode.Control_Left, KeyCode.Shift_Left)]
    [InlineData(KeyCode.Control_Right, KeyCode.Shift_Right)]
    public void ReverseRequiresBothModifiersAndDoesNotFallThrough(KeyCode control, KeyCode shift)
    {
        var keys = new ShortcutKeys();
        Assert.True(keys.AllowOrdinary);
        Assert.False(keys.IsReverse('r'));
        keys.Press((int)control);
        Assert.False(keys.AllowOrdinary);
        Assert.False(keys.IsReverse('r'));
        keys.Press((int)shift);
        Assert.True(keys.IsReverse('r'));
        Assert.True(keys.IsReverse('R'));
        Assert.True(keys.IsReverse(18));
        Assert.False(keys.IsReverse('g'));
        Assert.False(keys.AllowOrdinary);
        keys.Release((int)shift);
        Assert.False(keys.IsReverse('r'));
        keys.Release((int)control);
        Assert.True(keys.AllowOrdinary);
    }

    [Fact]
    public void HeldKeysAndFocusLossAreTrackedIndependently()
    {
        var keys = new ShortcutKeys();
        keys.Press((int)KeyCode.Control_Left);
        keys.Press((int)KeyCode.Control_Right);
        keys.Press((int)KeyCode.Shift_Left);
        keys.Release((int)KeyCode.Control_Left);
        Assert.True(keys.IsReverse('R'));
        keys.Press((int)KeyCode.Alt_Left);
        Assert.False(keys.IsReverse('R'));
        Assert.False(keys.AllowOrdinary);
        keys.Clear();
        Assert.True(keys.AllowOrdinary);
        Assert.False(keys.Shift);
        Assert.False(keys.Control);
    }
}