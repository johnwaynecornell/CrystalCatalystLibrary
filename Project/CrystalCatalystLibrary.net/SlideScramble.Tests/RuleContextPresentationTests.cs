using Xunit;

namespace SlideScramble.Tests;

public class RuleContextPresentationTests
{
    [Fact]
    public void MathematicalRuleChangeIgnoresPresetNameAndInitialInputPreferences()
    {
        var original = PuzzleRuleSet.CreateHybrid();
        var renamed = new PuzzleRuleSet("Custom", original.ScrambleAllowed, original.SolveAllowed,
            new[] { PuzzleOperationIds.Slide }, OperationRegistry.CreateDefault());
        var changedSolveRules = new PuzzleRuleSet("Custom", original.ScrambleAllowed,
            new[] { PuzzleOperationIds.Slide }, renamed.UIEnabled, OperationRegistry.CreateDefault());

        Assert.True(original.HasSameMathematicalPermissions(renamed));
        Assert.False(original.HasSameMathematicalPermissions(changedSolveRules));
        Assert.False(original.HasSameMathematicalPermissions(PuzzleRuleSet.CreateClassic()));
    }

    [Fact]
    public void ScoreTextShowsAccumulatedMoveCostForEveryPreset()
    {
        Assert.Equal("Move cost: 6  Goal cost: 10", Window.FormatGameScoreText(6, 10));
        Assert.Equal("Move cost: 6", Window.FormatGameScoreText(6, null));
    }

    [Fact]
    public void RecordingAndPlaybackStatusesIdentifyPrimitiveCountsAndPositions()
    {
        Assert.Equal("Recorded primitives: 8",
            Window.FormatRecordingStatus(RecordingControllerMode.Recording, 8, 8));
        Assert.Equal("Primitive playback: 3/8",
            Window.FormatRecordingStatus(RecordingControllerMode.Playback, 3, 8));
        Assert.Equal("Primitive position: 3/8", Window.FormatPrimitivePosition(3, 8));
        Assert.Equal("Recording from primitive position 3. Stop Rec, then Save.",
            Window.FormatContinueRecordingStatus(3));
    }

    [Fact]
    public void OperationPositionShowsCompletedAndInProgressOperations()
    {
        Assert.Equal("Operations: 0/3 complete",
            Window.FormatOperationPosition(RecordingControllerMode.Playback, 0, 3, null));
        Assert.Equal("Operations: 1/3 complete · #2 active",
            Window.FormatOperationPosition(RecordingControllerMode.Playback, 1, 3, 2));
        Assert.Equal("Operations: 3/3 complete",
            Window.FormatOperationPosition(RecordingControllerMode.Idle, 3, 3, null));
        Assert.Equal("Operations recorded: 2",
            Window.FormatOperationPosition(RecordingControllerMode.Recording, 2, 2, null));
    }

    [Fact]
    public void FpsDisplayIsOmittedWhenCenterHudWouldOverlapIt()
    {
        Assert.True(Window.ShouldDrawFps(900, 42, 500));
        Assert.False(Window.ShouldDrawFps(900, 42, 540));
        Assert.False(Window.ShouldDrawFps(649, 42, 300));
    }

    [Fact]
    public void FpsDisplayAccountsForLongRecordingTextAndWindowWidth()
    {
        const float fpsWidth = 42;
        const float centerWidthWithShortStatus = 500;
        const float centerWidthWithLongRecordingStatus = 540;

        Assert.True(Window.ShouldDrawFps(900, fpsWidth, centerWidthWithShortStatus));
        Assert.False(Window.ShouldDrawFps(900, fpsWidth, centerWidthWithLongRecordingStatus));
        Assert.True(Window.ShouldDrawFps(1000, fpsWidth, centerWidthWithLongRecordingStatus));
    }
}