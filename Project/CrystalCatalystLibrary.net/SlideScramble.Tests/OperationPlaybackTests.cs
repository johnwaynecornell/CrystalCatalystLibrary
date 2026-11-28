using System.Text.Json;
using SlideScramble;
using Xunit;

namespace SlideScramble.Tests;

public class OperationPlaybackTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShufflePlaybackUsesTheSavedCycleAcrossExportAndReversal(bool historical)
    {
        var grid = new Grid(3, 3);
        var initial = OperationBoardSnapshot.FromGrid(grid);
        var rules = PuzzleRuleSet.CreateShuffleLogic();
        var intent = new ShuffleOperationIntent(2, 2, PortableLensCell.Circle, PortableLensCell.Star);
        // TriangleStar.A was the v4 expansion for Circle → Star at this wrapped anchor.
        RecordedMove[] moves = historical
            ? [
                new(RecordedMoveAxis.Column, 2, 0, 1),
                new(RecordedMoveAxis.Row, 0, 2, 1),
                new(RecordedMoveAxis.Column, 2, 2, -1),
                new(RecordedMoveAxis.Row, 1, 2, -1)
            ]
            : new ShuffleOperation().Resolve(initial, intent).Expansion.ToArray();
        var operation = new RecordedOperation(PuzzleOperationIds.Shuffle, intent.ToRecordingParameters(),
            moves, OperationPlanPurpose.Solve, OperationMoveAccounting.LogicalOperation, 1);
        string json = new SessionRecording(initial.Cells, new[] { operation }, rules).ToJson();
        if (historical)
        {
            var v4 = System.Text.Json.Nodes.JsonNode.Parse(json)!;
            v4["version"] = 4;
            json = v4.ToJsonString();
        }

        var loaded = SessionRecording.FromJson(json);
        string exported = loaded.ToJson();
        using var document = JsonDocument.Parse(exported);
        Assert.Equal(5, document.RootElement.GetProperty("version").GetInt32());
        var reloaded = SessionRecording.FromJson(exported);
        Assert.Equal(exported, reloaded.ToJson());
        Assert.Equal(moves, reloaded.Moves);
        var saved = Assert.Single(reloaded.Operations);
        Assert.Equal(operation.Parameters.GetRawText(), saved.Parameters.GetRawText());
        Assert.Equal(OperationMoveAccounting.LogicalOperation, saved.Accounting);
        Assert.Equal(1, saved.MoveCost);

        var expected = initial.Cells;
        expected[2, 2] = historical ? 2 : 0;
        expected[2, 0] = 8;
        expected[0, 2] = historical ? 6 : 2;
        expected[0, 0] = historical ? 0 : 6;
        int fixedX = historical ? 0 : 2;
        int fixedTile = initial.TileAt(fixedX, 0);

        var controller = new SessionRecordingController(grid);
        foreach (var recording in new[] { loaded, reloaded })
        {
            controller.LoadPlayback(recording);
            var forward = controller.GetPlaybackOperationStep(true)!;
            var visual = forward.CreateAnimationPlan(initial);
            Assert.True(visual.SupportsLogicalAnimation);
            Assert.Equal(expected, visual.FinalCells);
            Assert.Equal(expected, visual.PrimitiveEndCells(3));
            Assert.DoesNotContain(visual.LogicalMotions, motion => motion.TileId == fixedTile);
            Assert.Single(visual.LogicalMotions, motion => visual.GetLogicalPoses(motion.TileId, 0.5f).Count == 2);
            controller.CommitPlaybackOperationStep(forward);
            Assert.Equal(4, controller.Position);
            Assert.Equal(1, controller.CompletedOperationCount);
            Assert.Equal(expected, OperationBoardSnapshot.FromGrid(grid).Cells);

            var backward = controller.GetPlaybackOperationStep(false)!;
            var reverse = backward.CreateAnimationPlan(OperationBoardSnapshot.FromGrid(grid));
            Assert.True(reverse.SupportsLogicalAnimation);
            Assert.DoesNotContain(reverse.LogicalMotions, motion => motion.TileId == fixedTile);
            Assert.Equal(initial.Cells, reverse.FinalCells);
            controller.CommitPlaybackOperationStep(backward);
            Assert.Equal(initial.Cells, OperationBoardSnapshot.FromGrid(grid).Cells);

            controller.LoadPlayback(recording.Reverse());
            while (controller.Step())
            {
            }

            Assert.Equal(initial.Cells, OperationBoardSnapshot.FromGrid(grid).Cells);
        }
    }

    [Fact]
    public void WholeOperationsAdvanceAndReverseAtPrimitiveBoundaries()
    {
        var (controller, grid, recording) = CreatePlayback();
        int[,] initial = OperationBoardSnapshot.FromGrid(grid).Cells;
        Assert.Equal(2, controller.OperationCount);
        Assert.Equal(0, controller.CompletedOperationCount);
        Assert.Null(controller.CurrentOperationIndex);
        controller.SetAutoplay(true);
        var forward = Assert.IsType<RecordedOperationPlaybackStep>(controller.GetPlaybackOperationStep(true));
        Assert.True(forward.IsCompleteOperation);
        Assert.Equal(4, forward.TargetPosition);
        var visual = forward.CreateAnimationPlan(OperationBoardSnapshot.FromGrid(grid));
        Assert.True(visual.SupportsLogicalAnimation);
        Assert.Equal(3, visual.LogicalMotions.Count);
        Assert.Equal((3, 3), visual.LensOverlayAnchor);
        controller.CommitPlaybackOperationStep(forward);
        Assert.Equal(visual.FinalCells, OperationBoardSnapshot.FromGrid(grid).Cells);
        Assert.Equal(4, controller.Position);
        Assert.Equal(1, controller.CompletedOperationCount);
        Assert.Null(controller.CurrentOperationIndex);
        Assert.True(controller.IsAutoplaying);
        Assert.Throws<InvalidOperationException>(() => controller.CommitPlaybackOperationStep(forward));

        var backward = Assert.IsType<RecordedOperationPlaybackStep>(controller.GetPlaybackOperationStep(false));
        Assert.True(backward.IsCompleteOperation);
        var reverseVisual = backward.CreateAnimationPlan(OperationBoardSnapshot.FromGrid(grid));
        Assert.True(reverseVisual.SupportsLogicalAnimation);
        Assert.Equal(visual.LensOverlayAnchor, reverseVisual.LensOverlayAnchor);
        Assert.Equal(initial, reverseVisual.FinalCells);
        controller.CommitPlaybackOperationStep(backward);
        Assert.Equal(initial, OperationBoardSnapshot.FromGrid(grid).Cells);
        Assert.Equal(0, controller.Position);
        Assert.Equal(0, controller.CompletedOperationCount);
        Assert.Equal(recording.ToJson(), controller.Playback!.ToJson());
        Assert.Throws<InvalidOperationException>(() => controller.CommitPlaybackOperationStep(forward));
    }

    [Fact]
    public void PartialOperationStepsOnlyToNearestBoundary()
    {
        var (controller, grid, _) = CreatePlayback();
        controller.Step();
        controller.Step();
        Assert.Equal(0, controller.CompletedOperationCount);
        Assert.Equal(1, controller.CurrentOperationIndex);
        var forward = Assert.IsType<RecordedOperationPlaybackStep>(controller.GetPlaybackOperationStep(true));
        Assert.False(forward.IsCompleteOperation);
        Assert.Equal(2, forward.Moves.Count);
        Assert.Equal(4, forward.TargetPosition);
        var visual = forward.CreateAnimationPlan(OperationBoardSnapshot.FromGrid(grid));
        Assert.False(visual.SupportsLogicalAnimation);
        Assert.Equal((3, 3), visual.LensOverlayAnchor);
        controller.CommitPlaybackOperationStep(forward);
        Assert.Equal(1, controller.CompletedOperationCount);
        Assert.Null(controller.CurrentOperationIndex);
        Assert.True(controller.StepBack());
        Assert.Equal(1, controller.CurrentOperationIndex);
        var backward = Assert.IsType<RecordedOperationPlaybackStep>(controller.GetPlaybackOperationStep(false));
        Assert.False(backward.IsCompleteOperation);
        Assert.Equal(3, backward.Moves.Count);
        var reverseVisual = backward.CreateAnimationPlan(OperationBoardSnapshot.FromGrid(grid));
        Assert.False(reverseVisual.SupportsLogicalAnimation);
        Assert.Equal(visual.LensOverlayAnchor, reverseVisual.LensOverlayAnchor);
        controller.CommitPlaybackOperationStep(backward);
        Assert.Equal(0, controller.Position);
    }

    [Fact]
    public void OperationProgressTracksRecordingLoadingAndContinuation()
    {
        var grid = new Grid(3, 3);
        var controller = new SessionRecordingController(grid);
        Assert.Equal(0, controller.OperationCount);
        controller.StartRecording();
        controller.MoveRow(0, 1);
        controller.MoveColumn(1, -1);
        Assert.Equal(2, controller.OperationCount);
        Assert.Equal(2, controller.CompletedOperationCount);
        Assert.Null(controller.CurrentOperationIndex);

        SessionRecording recording = controller.StopRecording();
        controller.LoadPlayback(recording);
        Assert.Equal(2, controller.OperationCount);
        Assert.Equal(0, controller.CompletedOperationCount);
        Assert.True(controller.Step());
        Assert.Equal(1, controller.CompletedOperationCount);
        Assert.Equal(1, controller.ContinueRecordingFromHere());
        Assert.Equal(1, controller.OperationCount);
        Assert.Equal(1, controller.CompletedOperationCount);
        controller.MoveRow(2, 1);
        Assert.Equal(2, controller.OperationCount);
        Assert.Equal(2, controller.CompletedOperationCount);
    }

    [Fact]
    public void ClassicMovesRemainIndividualOperationsAndEmptyPlaybackStops()
    {
        var grid = new Grid(3, 3);
        var recording = new SessionRecording(OperationBoardSnapshot.FromGrid(grid).Cells, new[]
        {
            new RecordedMove(RecordedMoveAxis.Row, 0, 0, 1),
            new RecordedMove(RecordedMoveAxis.Column, 1, 0, -1)
        });
        var controller = new SessionRecordingController(grid);
        controller.LoadPlayback(recording);
        var slide = controller.GetPlaybackOperationStep(true)!;
        Assert.Null(slide.CreateAnimationPlan(OperationBoardSnapshot.FromGrid(grid)).LensOverlayAnchor);
        controller.CommitPlaybackOperationStep(slide);
        Assert.Equal(1, controller.Position);
        controller.SetAutoplay(true);
        controller.CommitPlaybackOperationStep(controller.GetPlaybackOperationStep(true)!);
        Assert.Equal(2, controller.Position);
        Assert.False(controller.IsAutoplaying);
        Assert.Null(controller.GetPlaybackOperationStep(true));
        controller.ResetPlayback();
        Assert.Equal(2, controller.OperationCount);
        Assert.Equal(0, controller.CompletedOperationCount);
        Assert.Null(controller.GetPlaybackOperationStep(false));
    }

    [Fact]
    public void StaleBoardIsRejectedWithoutMovingPlaybackPosition()
    {
        var (controller, grid, _) = CreatePlayback();
        var step = controller.GetPlaybackOperationStep(true)!;
        grid.MoveRow(0, 1);
        var changed = OperationBoardSnapshot.FromGrid(grid);
        Assert.Throws<InvalidOperationException>(() => step.CreateAnimationPlan(changed));
        Assert.Throws<InvalidOperationException>(() => controller.CommitPlaybackOperationStep(step));
        Assert.Equal(0, controller.Position);
        Assert.Equal(changed.Cells, OperationBoardSnapshot.FromGrid(grid).Cells);
    }

    [Theory]
    [InlineData(OperationAnimationStyle.Logical)]
    [InlineData(OperationAnimationStyle.Primitive)]
    public void PlaybackAnimationShowsLensUntilCommitOrCancellation(OperationAnimationStyle style)
    {
        var (controller, grid, _) = CreatePlayback();
        var step = controller.GetPlaybackOperationStep(true)!;
        var timeline = new OperationAnimationTimeline(step.CreateAnimationPlan(OperationBoardSnapshot.FromGrid(grid)),
            style);
        int[,] original = OperationBoardSnapshot.FromGrid(grid).Cells;
        timeline.GetFrame(0);
        Assert.Equal(0f, timeline.LensOpacity);
        timeline.GetFrame(0.05);
        Assert.Equal(0.5f, timeline.LensOpacity, 3);
        timeline.GetFrame(timeline.DurationSeconds / 2);
        Assert.Equal(1f, timeline.LensOpacity);
        timeline.GetFrame(timeline.DurationSeconds - 0.05);
        Assert.Equal(0.5f, timeline.LensOpacity, 3);

        foreach (double fraction in new[] { 0, 0.25, 0.5, 0.75, 0.99 })
        {
            Assert.False(timeline.GetFrame(timeline.DurationSeconds * fraction).IsComplete);
            Assert.Equal((3, 3), timeline.GetVisibleLensAnchor(showOverlay: true));
            Assert.Null(timeline.GetVisibleLensAnchor(showOverlay: false));
        }
        Assert.False(timeline.TryCommit(() => controller.CommitPlaybackOperationStep(step)));
        Assert.Equal(0, controller.Position);
        Assert.Equal(original, OperationBoardSnapshot.FromGrid(grid).Cells);
        timeline.Cancel();
        Assert.Equal(0f, timeline.LensOpacity);
        Assert.Null(timeline.GetVisibleLensAnchor(showOverlay: true));
        Assert.False(timeline.TryCommit(() => controller.CommitPlaybackOperationStep(step)));
        Assert.Equal(0, controller.Position);

        var completed = new OperationAnimationTimeline(step.CreateAnimationPlan(OperationBoardSnapshot.FromGrid(grid)),
            style);
        completed.GetFrame(completed.DurationSeconds);
        Assert.Equal(0f, completed.LensOpacity);
        Assert.Null(completed.GetVisibleLensAnchor(showOverlay: true));
        Assert.True(completed.TryCommit(() => controller.CommitPlaybackOperationStep(step)));
        Assert.False(completed.TryCommit(() => controller.CommitPlaybackOperationStep(step)));
        Assert.Equal(4, controller.Position);
    }

    [Fact]
    public void NonlocalSavedShuffleFallsBackToPrimitivePresentation()
    {
        var grid = new Grid(4, 4);
        var board = OperationBoardSnapshot.FromGrid(grid);
        var rules = PuzzleRuleSet.CreateShuffleLogic();
        var intent = new ShuffleOperationIntent(0, 0, PortableLensCell.Circle, PortableLensCell.Star);
        var elsewhere = new ShuffleOperationIntent(2, 2, PortableLensCell.Circle, PortableLensCell.Star);
        var expansion = new OperationRuleEvaluator(rules.Registry).EvaluateUiRequest(board, elsewhere, rules)
            .Plan!.Expansion;
        var operation = new RecordedOperation(PuzzleOperationIds.Shuffle, intent.ToRecordingParameters(), expansion,
            OperationPlanPurpose.Solve, OperationMoveAccounting.LogicalOperation, 1);
        var controller = new SessionRecordingController(grid);
        controller.LoadPlayback(new SessionRecording(board.Cells, new[] { operation }, rules));
        var step = controller.GetPlaybackOperationStep(true)!;
        var visual = step.CreateAnimationPlan(board);
        Assert.False(visual.SupportsLogicalAnimation);
        Assert.Equal((0, 0), visual.LensOverlayAnchor);
        Assert.Equal(OperationAnimationStyle.Primitive,
            OperationAnimationPolicy.Select(operation.Accounting, operation.Purpose, visual.SupportsLogicalAnimation,
                OperationAnimationStyle.Logical));
        controller.CommitPlaybackOperationStep(step);
        Assert.Equal(visual.FinalCells, OperationBoardSnapshot.FromGrid(grid).Cells);
    }

    [Theory]
    [InlineData(PuzzleOperationIds.ColumnSwap, PortableLensCell.Circle, PortableLensCell.Star, "UpLeft")]
    [InlineData(PuzzleOperationIds.RowSwap, PortableLensCell.Circle, PortableLensCell.Triangle, "LeftUp")]
    public void SavedSwapsAndReversedRecordingsAnimateWithoutAResolver(string id, PortableLensCell start,
        PortableLensCell target, string variant)
    {
        var grid = new Grid(4, 4);
        var board = OperationBoardSnapshot.FromGrid(grid);
        var rules = PuzzleRuleSet.CreateHybrid();
        var intent = new PortableLensSwapIntent(id, 3, 3, start, target, variant);
        var plan = new OperationRuleEvaluator(rules.Registry).EvaluateUiRequest(board, intent, rules, new[] { id }).Plan!;
        var operation = new RecordedOperation(id, intent.ToRecordingParameters(), plan.Expansion, plan.Purpose,
            plan.Accounting, plan.MoveCost);
        var source = new SessionRecording(board.Cells, new[] { operation }, rules);
        var withoutResolvers = new OperationRegistry();
        Assert.False(withoutResolvers.TryGetOperation(id, out _));
        var reversed = SessionRecording.FromJson(source.Reverse().ToJson(), withoutResolvers);
        var controller = new SessionRecordingController(grid);
        controller.LoadPlayback(reversed);
        var forward = controller.GetPlaybackOperationStep(true)!;
        var visual = forward.CreateAnimationPlan(OperationBoardSnapshot.FromGrid(grid));
        Assert.True(visual.SupportsLogicalAnimation);
        Assert.Equal(4, visual.LogicalMotions.Count);
        Assert.Equal((3, 3), visual.LensOverlayAnchor);
        controller.CommitPlaybackOperationStep(forward);
        Assert.Equal(board.Cells, OperationBoardSnapshot.FromGrid(grid).Cells);
        var backward = controller.GetPlaybackOperationStep(false)!;
        var reverseVisual = backward.CreateAnimationPlan(OperationBoardSnapshot.FromGrid(grid));
        Assert.True(reverseVisual.SupportsLogicalAnimation);
        Assert.Equal(visual.LensOverlayAnchor, reverseVisual.LensOverlayAnchor);
        controller.CommitPlaybackOperationStep(backward);
        Assert.Equal(reversed.InitialCells, OperationBoardSnapshot.FromGrid(grid).Cells);
    }

    [Fact]
    public void SavedShuffleWithoutAnchorKeepsPrimitivePlaybackWithoutLens()
    {
        var (controller, grid, recording) = CreatePlayback();
        var shuffle = recording.Operations[0];
        var legacy = new RecordedOperation(shuffle.OperationId,
            JsonSerializer.SerializeToElement(new { startCell = "Circle", targetCell = "Star" }),
            shuffle.PrimitiveMoves, shuffle.Purpose, shuffle.Accounting, shuffle.MoveCost);
        controller.LoadPlayback(new SessionRecording(recording.InitialCells, new[] { legacy }, recording.RuleSet));
        var step = controller.GetPlaybackOperationStep(true)!;
        var visual = step.CreateAnimationPlan(OperationBoardSnapshot.FromGrid(grid));
        Assert.Null(visual.LensOverlayAnchor);
        Assert.False(visual.SupportsLogicalAnimation);
        controller.CommitPlaybackOperationStep(step);
        Assert.Equal(visual.FinalCells, OperationBoardSnapshot.FromGrid(grid).Cells);
    }

    [Theory]
    [InlineData(PortableLensCell.Circle, 4, 2)]
    [InlineData(PortableLensCell.Star, 0, 2)]
    [InlineData(PortableLensCell.Triangle, 4, 0)]
    [InlineData(PortableLensCell.Square, 0, 0)]
    public void LensSymbolsWrapAcrossBothEdgesOnARectangularBoard(PortableLensCell symbol, int x, int y)
    {
        Assert.Equal((x, y), LensGestureOperationMapper.BoardPosition(symbol, (4, 2), 5, 3));
    }

    private static (SessionRecordingController Controller, Grid Grid, SessionRecording Recording) CreatePlayback()
    {
        var grid = new Grid(4, 4);
        var board = OperationBoardSnapshot.FromGrid(grid);
        var rules = PuzzleRuleSet.CreateShuffleLogic();
        var intent = new ShuffleOperationIntent(3, 3, PortableLensCell.Circle, PortableLensCell.Star);
        var plan = new OperationRuleEvaluator(rules.Registry).EvaluateUiRequest(board, intent, rules).Plan!;
        var operation = new RecordedOperation(plan.OperationId, intent.ToRecordingParameters(), plan.Expansion,
            plan.Purpose, plan.Accounting, plan.MoveCost);
        var slide = RecordedOperation.CreateSlide(new RecordedMove(RecordedMoveAxis.Row, 0, 0, 1));
        var hybrid = PuzzleRuleSet.CreateHybrid();
        var recording = new SessionRecording(board.Cells, new[] { operation, slide }, hybrid);
        var controller = new SessionRecordingController(grid);
        controller.LoadPlayback(SessionRecording.FromJson(recording.ToJson()));
        return (controller, grid, recording);
    }
}