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
    public void CircleToStarShuffleUsesFullSizeLensEdgeWrapForTheDiagonalTile()
    {
        var (grid, plan, _) = CreateOperation(PuzzleOperationIds.Shuffle);
        var visual = new OperationAnimationPlan(OperationBoardSnapshot.FromGrid(grid), plan);
        OperationTileMotion diagonal = visual.LogicalMotions.Single(motion => motion.SourceX == 0 &&
            motion.SourceY == 2 && motion.DestinationX == 4 && motion.DestinationY == 0);

        IReadOnlyList<OperationTilePose> start = visual.GetLogicalPoses(diagonal.TileId, 0);
        IReadOnlyList<OperationTilePose> middle = visual.GetLogicalPoses(diagonal.TileId, 0.5f);
        IReadOnlyList<OperationTilePose> end = visual.GetLogicalPoses(diagonal.TileId, 1);

        Assert.Equal(2, start.Count);
        Assert.Equal(2, middle.Count);
        Assert.Equal(2, end.Count);
        Assert.Equal(new OperationTilePose(5.5f, 2.5f, 1), start[0]);
        Assert.Equal(new OperationTilePose(4.5f, 4.5f, 1), start[1]);
        Assert.Equal(new OperationTilePose(6.5f, 2.5f, 1), end[0]);
        Assert.Equal(new OperationTilePose(4.5f, 3.5f, 1), end[1]);
        Assert.Equal(new[] { (4, 2), (0, 2), (4, 0), (0, 0) }, visual.LensCells);
        Assert.NotEqual(diagonal.TileId, visual.CenterBypassTileId);
        Assert.All(middle, pose => Assert.Equal(1, pose.Scale, 4));
    }

    [Theory]
    [InlineData(PortableLensCell.Circle, PortableLensCell.Star)]
    [InlineData(PortableLensCell.Star, PortableLensCell.Circle)]
    [InlineData(PortableLensCell.Star, PortableLensCell.Square)]
    [InlineData(PortableLensCell.Square, PortableLensCell.Star)]
    [InlineData(PortableLensCell.Circle, PortableLensCell.Triangle)]
    [InlineData(PortableLensCell.Triangle, PortableLensCell.Circle)]
    [InlineData(PortableLensCell.Triangle, PortableLensCell.Square)]
    [InlineData(PortableLensCell.Square, PortableLensCell.Triangle)]
    public void EveryShuffleGestureHasOneFullSizeLensEdgeWrapAndSeparateBypass(
        PortableLensCell start, PortableLensCell target)
    {
        var (grid, plan, _) = CreateOperation(PuzzleOperationIds.Shuffle, startCell: start, targetCell: target);
        var visual = new OperationAnimationPlan(OperationBoardSnapshot.FromGrid(grid), plan);
        OperationTileMotion diagonal = visual.LogicalMotions.Single(motion =>
            motion.SourceX != motion.DestinationX && motion.SourceY != motion.DestinationY);

        Assert.Equal(2, visual.GetLogicalPoses(diagonal.TileId, 0.5f).Count);
        Assert.NotEqual(diagonal.TileId, visual.CenterBypassTileId);
        Assert.All(visual.GetLogicalPoses(diagonal.TileId, 0.5f), pose => Assert.Equal(1, pose.Scale, 4));
        Assert.Equal(4, visual.LensCells.Count);
    }

    [Fact]
    public void ShuffleLensEdgeRoutesRemainCorrectAtEveryWrappedBoardPlacement()
    {
        (PortableLensCell Start, PortableLensCell Target)[] gestures =
        [
            (PortableLensCell.Circle, PortableLensCell.Star),
            (PortableLensCell.Star, PortableLensCell.Circle),
            (PortableLensCell.Star, PortableLensCell.Square),
            (PortableLensCell.Square, PortableLensCell.Star),
            (PortableLensCell.Circle, PortableLensCell.Triangle),
            (PortableLensCell.Triangle, PortableLensCell.Circle),
            (PortableLensCell.Triangle, PortableLensCell.Square),
            (PortableLensCell.Square, PortableLensCell.Triangle)
        ];

        for (int anchorY = 0; anchorY < 3; anchorY++)
        for (int anchorX = 0; anchorX < 5; anchorX++)
        foreach (var gesture in gestures)
        {
            var (grid, operation, _) = CreateOperation(PuzzleOperationIds.Shuffle, startCell: gesture.Start,
                targetCell: gesture.Target, anchorX: anchorX, anchorY: anchorY);
            var visual = new OperationAnimationPlan(OperationBoardSnapshot.FromGrid(grid), operation);
            OperationTileMotion diagonal = visual.LogicalMotions.Single(motion =>
                motion.SourceX != motion.DestinationX && motion.SourceY != motion.DestinationY);
            IReadOnlyList<OperationTilePose> beginning = visual.GetLogicalPoses(diagonal.TileId, 0);
            IReadOnlyList<OperationTilePose> ending = visual.GetLogicalPoses(diagonal.TileId, 1);

            Assert.Equal(4, visual.LensCells.Count);
            Assert.Equal(2, beginning.Count);
            Assert.Equal(diagonal.SourceX, WrapCell(beginning[0].CenterX, visual.Width));
            Assert.Equal(diagonal.SourceY, WrapCell(beginning[0].CenterY, visual.Height));
            Assert.Equal(diagonal.DestinationX, WrapCell(ending[1].CenterX, visual.Width));
            Assert.Equal(diagonal.DestinationY, WrapCell(ending[1].CenterY, visual.Height));
            Assert.All(beginning, pose => Assert.Equal(1, pose.Scale));
            Assert.All(ending, pose => Assert.Equal(1, pose.Scale));
        }
    }

    [Fact]
    public void ShuffleCenterBypassAndSwapCenterConsumptionShrinkWholeTileFrames()
    {
        var (shuffleGrid, shufflePlan, _) = CreateOperation(PuzzleOperationIds.Shuffle);
        var shuffle = new OperationAnimationPlan(OperationBoardSnapshot.FromGrid(shuffleGrid), shufflePlan);
        int bypassId = shuffle.CenterBypassTileId!.Value;
        Assert.True(shuffle.GetLogicalPose(bypassId, 0.5f).Scale < 1);
        OperationTileMotion diagonal = shuffle.LogicalMotions.Single(motion =>
            motion.SourceX != motion.DestinationX && motion.SourceY != motion.DestinationY);
        Assert.NotEqual(diagonal.TileId, bypassId);
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

    [Fact]
    public void StandaloneSlideUsesLogicalCadenceWhileMultiPrimitiveOperationsKeepPerStepTiming()
    {
        var grid = new Grid(4, 4);
        var board = OperationBoardSnapshot.FromGrid(grid);
        var registry = OperationRegistry.CreateDefault();
        OperationEvaluation evaluation = new OperationRuleEvaluator(registry).EvaluateUiRequest(board,
            new SlideOperationIntent(RecordedMoveAxis.Row, 0, 2), PuzzleRuleSet.CreateClassic());
        Assert.True(evaluation.IsAccepted, evaluation.RejectionReason);

        var slidePlan = new OperationAnimationPlan(board, evaluation.Plan!);
        var slideTimeline = new OperationAnimationTimeline(slidePlan, OperationAnimationStyle.Primitive);
        Assert.Equal(OperationAnimationTimeline.LogicalDurationSeconds, slideTimeline.DurationSeconds);

        OperationAnimationFrame halfway = slideTimeline.GetFrame(slideTimeline.DurationSeconds / 2);
        Assert.False(halfway.IsComplete);
        Assert.Equal(0, halfway.PrimitiveIndex);
        Assert.Equal(0.5f, halfway.Progress, 4);

        var (_, shufflePlan, _) = CreateOperation(PuzzleOperationIds.Shuffle);
        var shuffleVisual = new OperationAnimationPlan(OperationBoardSnapshot.FromGrid(new Grid(5, 3)), shufflePlan);
        var shuffleTimeline = new OperationAnimationTimeline(shuffleVisual, OperationAnimationStyle.Primitive);
        Assert.Equal(4 * OperationAnimationTimeline.PrimitiveStepDurationSeconds, shuffleTimeline.DurationSeconds);
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
    public void GeneratedLensScramblesPreferLogicalAnimationAndRespectExplicitStyle()
    {
        var registry = OperationRegistry.CreateDefault();
        var rules = new PuzzleRuleSet("Hybrid", new[] { PuzzleOperationIds.Slide, PuzzleOperationIds.Shuffle },
            new[] { PuzzleOperationIds.Slide, PuzzleOperationIds.Shuffle },
            new[] { PuzzleOperationIds.Slide, PuzzleOperationIds.Shuffle }, registry);
        var board = OperationBoardSnapshot.FromGrid(new Grid(5, 3));
        var evaluator = new OperationRuleEvaluator(registry);
        OperationEvaluation shuffleEvaluation = evaluator.EvaluateScrambleRequest(board,
            new ShuffleOperationIntent(4, 2, PortableLensCell.Circle, PortableLensCell.Star), rules);
        OperationEvaluation slideEvaluation = evaluator.EvaluateScrambleRequest(board,
            new SlideOperationIntent(RecordedMoveAxis.Row, 0, 1), rules);
        Assert.True(shuffleEvaluation.IsAccepted, shuffleEvaluation.RejectionReason);
        Assert.True(slideEvaluation.IsAccepted, slideEvaluation.RejectionReason);

        var shufflePlan = new OperationAnimationPlan(board, shuffleEvaluation.Plan!);
        var slidePlan = new OperationAnimationPlan(board, slideEvaluation.Plan!);

        Assert.Equal(OperationAnimationStyle.Logical, OperationAnimationPolicy.Select(shuffleEvaluation.Plan!,
            shufflePlan.SupportsLogicalAnimation, preferLogicalForScramble: true));
        Assert.Equal(OperationAnimationStyle.Primitive, OperationAnimationPolicy.Select(slideEvaluation.Plan!,
            slidePlan.SupportsLogicalAnimation, preferLogicalForScramble: true));
        Assert.Equal(OperationAnimationStyle.Primitive, OperationAnimationPolicy.Select(shuffleEvaluation.Plan!,
            shufflePlan.SupportsLogicalAnimation, OperationAnimationStyle.Primitive, preferLogicalForScramble: true));
        Assert.Equal(shuffleEvaluation.Plan!.Expansion, shufflePlan.PrimitiveMoves);
        Assert.Equal(OperationAnimationStyle.Primitive, OperationAnimationPolicy.Select(slideEvaluation.Plan!,
            slidePlan.SupportsLogicalAnimation, OperationAnimationStyle.Logical, preferLogicalForScramble: true));
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
        string operationId, OperationMoveAccounting? requestedAccounting = null,
        PortableLensCell startCell = PortableLensCell.Circle, PortableLensCell targetCell = PortableLensCell.Star,
        int anchorX = 4, int anchorY = 2)
    {
        var registry = OperationRegistry.CreateDefault();
        var grid = new Grid(5, 3);
        IOperationIntent intent = operationId switch
        {
            PuzzleOperationIds.Shuffle => new ShuffleOperationIntent(anchorX, anchorY, startCell, targetCell),
            PuzzleOperationIds.ColumnSwap => new PortableLensSwapIntent(operationId, anchorX, anchorY,
                PortableLensCell.Circle, PortableLensCell.Star, "UpLeft"),
            PuzzleOperationIds.RowSwap => new PortableLensSwapIntent(operationId, anchorX, anchorY,
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

    private static int WrapCell(float center, int length) =>
        ((int)MathF.Floor(center - 0.5f) % length + length) % length;
}