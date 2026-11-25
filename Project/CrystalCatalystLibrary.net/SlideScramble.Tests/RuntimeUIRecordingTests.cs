using SlideScramble;
using Xunit;

namespace SlideScramble.Tests;

public class RuntimeUIRecordingTests
{
    [Fact]
    public void LoadedRulesRemainActiveWhenStartingANewRecording()
    {
        var rules = PuzzleRuleSet.CreateShuffleLogic();
        var original = new SessionRecording(new[,] { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } },
            Array.Empty<RecordedOperation>(), rules);
        var controller = new SessionRecordingController(new Grid(3, 3));
        controller.LoadPlayback(original);
        controller.StopPlayback();
        controller.StartRecording();
        Assert.Equal(rules.SolveAllowed, controller.RuleSet.SolveAllowed);
        Assert.Equal(rules.UIEnabled, controller.StopRecording().RuleSet.UIEnabled);
    }

    [Fact]
    public void ConfiguringRulesAfterPlaybackStartsANewContextWithoutChangingBoard()
    {
        var grid = new Grid(3, 3);
        var controller = new SessionRecordingController(grid);
        var original = new SessionRecording(new[,] { { 1, 0, 2 }, { 3, 4, 5 }, { 6, 7, 8 } },
            Array.Empty<RecordedOperation>(), PuzzleRuleSet.CreateShuffleLogic());
        controller.LoadPlayback(original);
        controller.StopPlayback();
        int[,] before = OperationBoardSnapshot.FromGrid(grid).Cells;
        controller.ConfigureRules(PuzzleRuleSet.CreateClassic());
        Assert.Equal(before, OperationBoardSnapshot.FromGrid(grid).Cells);
        Assert.Equal("Classic", controller.RuleSet.Name);
        Assert.Null(controller.Playback);
    }

    [Fact]
    public void LiveInputCanExposeAnOperationThatSolveRulesStillReject()
    {
        var registry = OperationRegistry.CreateDefault();
        var rules = new PuzzleRuleSet("No solve operations", Array.Empty<string>(), Array.Empty<string>(),
            Array.Empty<string>(), registry);
        var grid = new Grid(4, 4);
        var controller = new SessionRecordingController(grid, rules);
        controller.StartRecording();
        Assert.True(controller.SetRuntimeUIEnabled(PuzzleOperationIds.Shuffle, true));
        var result = new OperationRuleEvaluator(registry).EvaluateUiRequest(OperationBoardSnapshot.FromGrid(grid),
            new ShuffleOperationIntent(0, 0, PortableLensCell.Circle, PortableLensCell.Star),
            controller.RuleSet, controller.RuntimeUIEnabled);
        Assert.False(result.IsAccepted);
        Assert.Empty(controller.StopRecording().Operations);
    }

    [Fact]
    public void DisablingInputInvalidatesAnOutstandingPlanWithoutMutation()
    {
        var rules = PuzzleRuleSet.CreateShuffleLogic();
        var grid = new Grid(4, 4);
        var controller = new SessionRecordingController(grid, rules);
        controller.StartRecording();
        var before = OperationBoardSnapshot.FromGrid(grid).Cells;
        var intent = new ShuffleOperationIntent(0, 0, PortableLensCell.Circle, PortableLensCell.Star);
        var result = new OperationRuleEvaluator(OperationRegistry.CreateDefault()).EvaluateUiRequest(
            OperationBoardSnapshot.FromGrid(grid), intent, rules, controller.RuntimeUIEnabled);
        Assert.True(result.IsAccepted, result.RejectionReason);
        Assert.True(controller.SetRuntimeUIEnabled(PuzzleOperationIds.Shuffle, false));
        Assert.Throws<InvalidOperationException>(() => controller.ApplyOperation(result.Plan!, intent.ToRecordingParameters()));
        Assert.Equal(before, OperationBoardSnapshot.FromGrid(grid).Cells);
        Assert.Empty(controller.StopRecording().Operations);
    }

    [Fact]
    public void OperationEnabledAfterRecordingStartsIsSavedAndReplayedWithoutInitialUiPermission()
    {
        OperationRegistry registry = OperationRegistry.CreateDefault();
        var rules = new PuzzleRuleSet("Runtime input test", new[] { PuzzleOperationIds.Slide },
            new[] { PuzzleOperationIds.Shuffle }, new[] { PuzzleOperationIds.Slide }, registry);
        var grid = new Grid(4, 4);
        var controller = new SessionRecordingController(grid, rules);
        controller.StartRecording();

        Assert.False(controller.RuntimeUIEnabled.Contains(PuzzleOperationIds.Shuffle));
        Assert.True(controller.SetRuntimeUIEnabled(PuzzleOperationIds.Shuffle, true));
        var intent = new ShuffleOperationIntent(3, 3, PortableLensCell.Circle, PortableLensCell.Star);
        var evaluator = new OperationRuleEvaluator(registry);
        OperationEvaluation evaluation = evaluator.EvaluateUiRequest(OperationBoardSnapshot.FromGrid(grid), intent,
            controller.RuleSet, controller.RuntimeUIEnabled);
        Assert.True(evaluation.IsAccepted, evaluation.RejectionReason);
        controller.ApplyOperation(evaluation.Plan!, intent.ToRecordingParameters());

        int[,] recordedResult = OperationBoardSnapshot.FromGrid(grid).Cells;
        SessionRecording recording = controller.StopRecording();
        Assert.Equal(new[] { PuzzleOperationIds.Slide }, recording.RuleSet.UIEnabled);
        Assert.Equal(PuzzleOperationIds.Shuffle, Assert.Single(recording.Operations).OperationId);

        SessionRecording reloaded = SessionRecording.FromJson(recording.ToJson(), registry);
        Assert.Equal(new[] { PuzzleOperationIds.Slide }, reloaded.RuleSet.UIEnabled);
        Assert.Equal(PuzzleOperationIds.Shuffle, Assert.Single(reloaded.Operations).OperationId);

        var replayGrid = new Grid(4, 4);
        var replayController = new SessionRecordingController(replayGrid);
        replayController.LoadPlayback(reloaded);
        Assert.Equal(new[] { PuzzleOperationIds.Slide }, replayController.RuntimeUIEnabled);
        while (replayController.Step()) { }

        Assert.Equal(recordedResult, OperationBoardSnapshot.FromGrid(replayGrid).Cells);
    }
}
