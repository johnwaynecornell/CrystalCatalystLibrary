using SlideScramble;
using Xunit;

namespace SlideScramble.Tests;

public class OperationPlaybackTests
{
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
        Assert.False(forward.CreateAnimationPlan(OperationBoardSnapshot.FromGrid(grid)).SupportsLogicalAnimation);
        controller.CommitPlaybackOperationStep(forward);
        Assert.Equal(1, controller.CompletedOperationCount);
        Assert.Null(controller.CurrentOperationIndex);
        Assert.True(controller.StepBack());
        Assert.Equal(1, controller.CurrentOperationIndex);
        var backward = Assert.IsType<RecordedOperationPlaybackStep>(controller.GetPlaybackOperationStep(false));
        Assert.False(backward.IsCompleteOperation);
        Assert.Equal(3, backward.Moves.Count);
        Assert.False(backward.CreateAnimationPlan(OperationBoardSnapshot.FromGrid(grid)).SupportsLogicalAnimation);
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
        controller.CommitPlaybackOperationStep(controller.GetPlaybackOperationStep(true)!);
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

    [Fact]
    public void PlaybackAnimationCommitsOnceAndCancellationLeavesStateUnchanged()
    {
        var (controller, grid, _) = CreatePlayback();
        var step = controller.GetPlaybackOperationStep(true)!;
        var timeline = new OperationAnimationTimeline(step.CreateAnimationPlan(OperationBoardSnapshot.FromGrid(grid)),
            OperationAnimationStyle.Logical);
        int[,] original = OperationBoardSnapshot.FromGrid(grid).Cells;
        timeline.GetFrame(timeline.DurationSeconds / 2);
        Assert.False(timeline.TryCommit(() => controller.CommitPlaybackOperationStep(step)));
        Assert.Equal(0, controller.Position);
        Assert.Equal(original, OperationBoardSnapshot.FromGrid(grid).Cells);
        timeline.Cancel();
        Assert.False(timeline.TryCommit(() => controller.CommitPlaybackOperationStep(step)));
        Assert.Equal(0, controller.Position);

        var completed = new OperationAnimationTimeline(step.CreateAnimationPlan(OperationBoardSnapshot.FromGrid(grid)),
            OperationAnimationStyle.Logical);
        completed.GetFrame(completed.DurationSeconds);
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
        controller.CommitPlaybackOperationStep(forward);
        Assert.Equal(board.Cells, OperationBoardSnapshot.FromGrid(grid).Cells);
        var backward = controller.GetPlaybackOperationStep(false)!;
        Assert.True(backward.CreateAnimationPlan(OperationBoardSnapshot.FromGrid(grid)).SupportsLogicalAnimation);
        controller.CommitPlaybackOperationStep(backward);
        Assert.Equal(reversed.InitialCells, OperationBoardSnapshot.FromGrid(grid).Cells);
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