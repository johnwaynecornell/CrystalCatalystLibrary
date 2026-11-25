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
}