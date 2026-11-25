using Xunit;

namespace SlideScramble.Tests;

public class ShuffleRecordingIntegrationTests
{
    [Fact]
    public void RealShufflesRoundTripReverseAndContinueOnlyAtLogicalBoundaries()
    {
        var registry = OperationRegistry.CreateDefault();
        var rules = PuzzleRuleSet.CreateShuffleLogic(registry);
        var evaluator = new OperationRuleEvaluator(registry);
        var grid = new Grid(4, 3, 32);
        var controller = new SessionRecordingController(grid, rules);
        controller.StartRecording();
        Apply(controller, grid, evaluator, rules, new ShuffleOperationIntent(3, 2, PortableLensCell.Circle, PortableLensCell.Star));
        Apply(controller, grid, evaluator, rules, new ShuffleOperationIntent(1, 0, PortableLensCell.Triangle, PortableLensCell.Square));
        int[,] final = OperationBoardSnapshot.FromGrid(grid).Cells;
        var saved = controller.StopRecording();
        var loaded = SessionRecording.FromJson(saved.ToJson());

        Assert.Equal(2, loaded.Operations.Count);
        Assert.Equal(8, loaded.Moves.Count);
        Assert.All(loaded.Operations, operation =>
        {
            Assert.Equal(PuzzleOperationIds.Shuffle, operation.OperationId);
            Assert.Equal(1, operation.MoveCost);
            Assert.Equal(4, operation.PrimitiveMoves.Count);
        });
        Assert.Equal("ShuffleLogic", loaded.RuleSet.Name);
        Assert.Equal(saved.ToJson(), loaded.Reverse().Reverse().ToJson());

        var replayGrid = new Grid(4, 3, 32);
        var replay = new SessionRecordingController(replayGrid, rules);
        replay.LoadPlayback(loaded);
        Assert.True(replay.Step());
        Assert.Throws<InvalidOperationException>(() => replay.ContinueRecordingFromHere());
        Assert.Equal(1, replay.Position);
        while (replay.Position < 4) Assert.True(replay.Step());
        Assert.True(replay.IsOperationBoundary(replay.Position));
        Assert.Equal(4, replay.ContinueRecordingFromHere());
        Apply(replay, replayGrid, evaluator, rules, new ShuffleOperationIntent(1, 0, PortableLensCell.Triangle, PortableLensCell.Square));
        var continued = replay.StopRecording();
        Assert.Equal(loaded.ToJson(), continued.ToJson());
        Assert.Equal(final, OperationBoardSnapshot.FromGrid(replayGrid).Cells);

        replay.LoadPlayback(loaded.Reverse());
        while (replay.Step()) { }
        Assert.Equal(loaded.InitialCells, OperationBoardSnapshot.FromGrid(replayGrid).Cells);
    }

    private static void Apply(SessionRecordingController controller, Grid grid, OperationRuleEvaluator evaluator,
        PuzzleRuleSet rules, ShuffleOperationIntent intent)
    {
        var evaluation = evaluator.EvaluateUiRequest(OperationBoardSnapshot.FromGrid(grid), intent, rules);
        Assert.True(evaluation.IsAccepted, evaluation.RejectionReason);
        controller.ApplyOperation(evaluation.Plan!, intent.ToRecordingParameters());
    }
}