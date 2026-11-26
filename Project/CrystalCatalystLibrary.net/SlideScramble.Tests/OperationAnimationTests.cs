using System.Text.Json;
using Xunit;

namespace SlideScramble.Tests;

public class OperationAnimationTests
{
    [Fact]
    public void LogicalShuffleTracksExactlyThreeChangedTilesAndPreservesTheOtherLensCell()
    {
        var (grid, plan, _) = CreateOperation(PuzzleOperationIds.Shuffle);
        var visual = new OperationAnimationPlan(OperationBoardSnapshot.FromGrid(grid), plan);

        Assert.True(visual.SupportsLogicalAnimation);
        Assert.Equal(3, visual.LogicalMotions.Count);
        Assert.Equal(3, CountChangedCells(visual.StartingCells, visual.FinalCells));
        Assert.Equal(visual.StartingCells[0, 0], visual.FinalCells[0, 0]);
        Assert.Equal(4, visual.PrimitiveCount);
        Assert.NotNull(visual.CenterBypassTileId);
    }

    [Theory]
    [InlineData(PuzzleOperationIds.ColumnSwap)]
    [InlineData(PuzzleOperationIds.RowSwap)]
    public void LogicalSwapTracksBothVerifiedExchangedPairs(string operationId)
    {
        var (grid, plan, _) = CreateOperation(operationId);
        var visual = new OperationAnimationPlan(OperationBoardSnapshot.FromGrid(grid), plan);

        Assert.True(visual.SupportsLogicalAnimation);
        Assert.Equal(4, visual.LogicalMotions.Count);
        Assert.Equal(4, CountChangedCells(visual.StartingCells, visual.FinalCells));
        Assert.Null(visual.CenterBypassTileId);
        Assert.All(visual.LogicalMotions, motion => Assert.NotEqual(motion.TileId,
            visual.TileAtFinal(motion.SourceX, motion.SourceY)));
        Assert.All(visual.LogicalMotions, motion =>
            Assert.Contains(visual.LogicalMotions, partner => partner.TileId == motion.PartnerTileId &&
                                                              partner.PartnerTileId == motion.TileId));
    }

    [Fact]
    public void LogicalPosesStartAndFinishAtTheExactWrappedTileDestinations()
    {
        var (grid, plan, _) = CreateOperation(PuzzleOperationIds.Shuffle);
        var visual = new OperationAnimationPlan(OperationBoardSnapshot.FromGrid(grid), plan);

        foreach (OperationTileMotion motion in visual.LogicalMotions)
        {
            OperationTilePose start = visual.GetLogicalPose(motion.TileId, 0);
            OperationTilePose end = visual.GetLogicalPose(motion.TileId, 1);
            Assert.Equal(motion.SourceX + 0.5f, start.CenterX, 4);
            Assert.Equal(motion.SourceY + 0.5f, start.CenterY, 4);
            Assert.Equal(1, start.Scale, 4);
            Assert.Equal(motion.SourceX + motion.DeltaX + 0.5f, end.CenterX, 4);
            Assert.Equal(motion.SourceY + motion.DeltaY + 0.5f, end.CenterY, 4);
            Assert.Equal(1, end.Scale, 4);
        }

        Assert.Equal(-1, OperationAnimationGeometry.ShortestDelta(0, 4, 5));
        Assert.Equal(1, OperationAnimationGeometry.ShortestDelta(4, 0, 5));
    }

    [Fact]
    public void ShuffleCenterBypassAndSwapCenterConsumptionShrinkWholeTileFrames()
    {
        var (shuffleGrid, shufflePlan, _) = CreateOperation(PuzzleOperationIds.Shuffle);
        var shuffle = new OperationAnimationPlan(OperationBoardSnapshot.FromGrid(shuffleGrid), shufflePlan);
        int bypassId = shuffle.CenterBypassTileId!.Value;
        Assert.True(shuffle.GetLogicalPose(bypassId, 0.5f).Scale < 1);
        Assert.All(shuffle.LogicalMotions.Where(motion => motion.TileId != bypassId), motion =>
            Assert.Equal(1, shuffle.GetLogicalPose(motion.TileId, 0.5f).Scale, 4));

        var (swapGrid, swapPlan, _) = CreateOperation(PuzzleOperationIds.RowSwap);
        var swap = new OperationAnimationPlan(OperationBoardSnapshot.FromGrid(swapGrid), swapPlan);
        Assert.All(swap.LogicalMotions, motion =>
            Assert.Equal(0.22f, swap.GetLogicalPose(motion.TileId, 0.5f).Scale, 4));
    }

    [Fact]
    public void WrappedLogicalTileHasClippedCopiesAtBothBoardEdges()
    {
        var copies = OperationAnimationGeometry.GetVisibleWrappedCopies(0.25f, 1.5f,
            0.8f, 0.8f, 5, 3);

        Assert.Contains((0.25f, 1.5f), copies);
        Assert.Contains((5.25f, 1.5f), copies);
        Assert.Equal(2, copies.Count);
    }

    [Fact]
    public void PrimitiveTimelineShowsOrderedIntermediateBoardsWithoutMutatingTheLiveGrid()
    {
        var (grid, plan, _) = CreateOperation(PuzzleOperationIds.Shuffle);
        int[,] original = OperationBoardSnapshot.FromGrid(grid).Cells;
        var visual = new OperationAnimationPlan(new OperationBoardSnapshot(original), plan);
        var timeline = new OperationAnimationTimeline(visual, OperationAnimationStyle.Primitive);

        OperationAnimationFrame start = timeline.GetFrame(0);
        OperationAnimationFrame nextStep = timeline.GetFrame(OperationAnimationTimeline.PrimitiveStepDurationSeconds);
        OperationAnimationFrame finish = timeline.GetFrame(timeline.DurationSeconds);

        Assert.Equal(0, start.PrimitiveIndex);
        Assert.Equal(0, start.Progress);
        Assert.Equal(1, nextStep.PrimitiveIndex);
        Assert.Equal(0, nextStep.Progress);
        Assert.True(finish.IsComplete);
        Assert.Equal(visual.FinalCells, visual.PrimitiveEndCells(visual.PrimitiveCount - 1));
        Assert.Equal(original, OperationBoardSnapshot.FromGrid(grid).Cells);
    }

    [Theory]
    [InlineData(RecordedMoveAxis.Row, 2)]
    [InlineData(RecordedMoveAxis.Row, -2)]
    [InlineData(RecordedMoveAxis.Column, 3)]
    [InlineData(RecordedMoveAxis.Column, -3)]
    public void PrimitiveMotionsFollowTheRecordedDirectionForEveryTile(RecordedMoveAxis axis, int delta)
    {
        var grid = new Grid(4, 4);
        var board = OperationBoardSnapshot.FromGrid(grid);
        var registry = OperationRegistry.CreateDefault();
        var evaluation = new OperationRuleEvaluator(registry).EvaluateUiRequest(board,
            new SlideOperationIntent(axis, 0, delta), PuzzleRuleSet.CreateClassic());
        Assert.True(evaluation.IsAccepted, evaluation.RejectionReason);
        var visual = new OperationAnimationPlan(board, evaluation.Plan!);

        Assert.Equal(4, visual.GetPrimitiveMotions(0).Count);
        Assert.All(visual.GetPrimitiveMotions(0), motion =>
        {
            Assert.Equal(axis == RecordedMoveAxis.Row ? -delta : 0, motion.DeltaX);
            Assert.Equal(axis == RecordedMoveAxis.Column ? -delta : 0, motion.DeltaY);
        });
    }

    [Fact]
    public void CancellationNeverCommitsAndCompletedAnimationCommitsAtMostOnce()
    {
        var (grid, plan, parameters) = CreateOperation(PuzzleOperationIds.Shuffle);
        var visual = new OperationAnimationPlan(OperationBoardSnapshot.FromGrid(grid), plan);
        var cancelled = new OperationAnimationTimeline(visual, OperationAnimationStyle.Logical);
        int cancelledCommits = 0;
        Assert.True(cancelled.Cancel());
        Assert.False(cancelled.TryCommit(() => cancelledCommits++));
        Assert.Equal(0, cancelledCommits);

        var completed = new OperationAnimationTimeline(visual, OperationAnimationStyle.Logical);
        Assert.False(completed.TryCommit(() => cancelledCommits++));
        completed.GetFrame(completed.DurationSeconds);
        Assert.True(completed.TryCommit(() => cancelledCommits++));
        Assert.False(completed.TryCommit(() => cancelledCommits++));
        Assert.Equal(1, cancelledCommits);
    }

    [Theory]
    [InlineData(OperationMoveAccounting.LogicalOperation, null, OperationAnimationStyle.Logical)]
    [InlineData(OperationMoveAccounting.PrimitiveMoves, null, OperationAnimationStyle.Primitive)]
    [InlineData(OperationMoveAccounting.LogicalOperation, OperationAnimationStyle.Instant,
        OperationAnimationStyle.Instant)]
    [InlineData(OperationMoveAccounting.PrimitiveMoves, OperationAnimationStyle.Logical,
        OperationAnimationStyle.Logical)]
    public void AnimationPolicyKeepsPresentationSeparateFromMoveAccounting(OperationMoveAccounting accounting,
        OperationAnimationStyle? preference, OperationAnimationStyle expected)
    {
        var (_, plan, _) = CreateOperation(PuzzleOperationIds.Shuffle, accounting);

        Assert.Equal(expected, OperationAnimationPolicy.Select(plan, true, preference));
    }

    [Fact]
    public void RecordingReceivesOneCompleteOperationOnlyAfterAnimationCompletes()
    {
        var (grid, plan, parameters) = CreateOperation(PuzzleOperationIds.Shuffle);
        var controller = new SessionRecordingController(grid,
            new PuzzleRuleSet("Shuffle", new[] { PuzzleOperationIds.Slide },
                new[] { PuzzleOperationIds.Shuffle }, new[] { PuzzleOperationIds.Shuffle },
                OperationRegistry.CreateDefault()));
        controller.StartRecording();
        int[,] original = OperationBoardSnapshot.FromGrid(grid).Cells;
        var visual = new OperationAnimationPlan(new OperationBoardSnapshot(original), plan);
        var timeline = new OperationAnimationTimeline(visual, OperationAnimationStyle.Logical);

        Assert.Equal(original, OperationBoardSnapshot.FromGrid(grid).Cells);
        Assert.Equal(0, controller.MoveCount);
        timeline.GetFrame(timeline.DurationSeconds);
        Assert.True(timeline.TryCommit(() => controller.ApplyOperation(plan, parameters)));
        Assert.False(timeline.TryCommit(() => controller.ApplyOperation(plan, parameters)));

        SessionRecording recording = controller.StopRecording();
        Assert.Equal(visual.FinalCells, OperationBoardSnapshot.FromGrid(grid).Cells);
        Assert.Equal(1, recording.Operations.Count);
        Assert.Equal(4, recording.Moves.Count);
        Assert.Equal(1, recording.Operations[0].MoveCost);
    }

    private static (Grid Grid, OperationPlan Plan, JsonElement Parameters) CreateOperation(
        string operationId, OperationMoveAccounting? requestedAccounting = null)
    {
        var registry = OperationRegistry.CreateDefault();
        var grid = new Grid(5, 3);
        IOperationIntent intent = operationId switch
        {
            PuzzleOperationIds.Shuffle => new ShuffleOperationIntent(4, 2,
                PortableLensCell.Circle, PortableLensCell.Star),
            PuzzleOperationIds.ColumnSwap => new PortableLensSwapIntent(operationId, 4, 2,
                PortableLensCell.Circle, PortableLensCell.Star, "UpLeft"),
            PuzzleOperationIds.RowSwap => new PortableLensSwapIntent(operationId, 4, 2,
                PortableLensCell.Circle, PortableLensCell.Triangle, "LeftUp"),
            _ => throw new ArgumentOutOfRangeException(nameof(operationId))
        };
        string[] solveAllowed = requestedAccounting == OperationMoveAccounting.PrimitiveMoves
            ? new[] { PuzzleOperationIds.Slide }
            : new[] { operationId };
        var rules = new PuzzleRuleSet("Animation Test", new[] { PuzzleOperationIds.Slide }, solveAllowed,
            new[] { operationId }, registry);
        OperationEvaluation evaluation = new OperationRuleEvaluator(registry).EvaluateUiRequest(
            OperationBoardSnapshot.FromGrid(grid), intent, rules, new[] { operationId });
        Assert.True(evaluation.IsAccepted, evaluation.RejectionReason);
        Assert.Equal(requestedAccounting ?? OperationMoveAccounting.LogicalOperation, evaluation.Plan!.Accounting);
        JsonElement parameters = intent switch
        {
            ShuffleOperationIntent shuffle => shuffle.ToRecordingParameters(),
            PortableLensSwapIntent swap => swap.ToRecordingParameters(),
            _ => throw new InvalidOperationException("Unexpected test intent")
        };
        return (grid, evaluation.Plan, parameters);
    }

    private static int CountChangedCells(int[,] before, int[,] after)
    {
        int count = 0;
        for (int y = 0; y < before.GetLength(0); y++)
        for (int x = 0; x < before.GetLength(1); x++)
            if (before[y, x] != after[y, x])
                count++;
        return count;
    }
}