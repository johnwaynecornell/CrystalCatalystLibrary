using Xunit;

namespace SlideScramble.Tests;

public class SlideAxisPermissionTests
{
    private const string Slide = PuzzleOperationIds.Slide;
    private const string RowSlide = PuzzleOperationIds.RowSlide;
    private const string ColumnSlide = PuzzleOperationIds.ColumnSlide;
    private static int[,] Identity => new[,] { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } };

    [Theory]
    [InlineData(false, false, null)]
    [InlineData(true, false, RowSlide)]
    [InlineData(false, true, ColumnSlide)]
    [InlineData(true, true, Slide)]
    public void CanonicalPermissionsRoundTrip(bool row, bool column, string? canonical)
    {
        var permissions = new List<string>();
        if (row) permissions.Add(RowSlide);
        if (column) permissions.Add(ColumnSlide);
        string[] expected = canonical == null ? [] : [canonical];
        var rules = new PuzzleRuleSet("test", permissions, permissions, permissions, OperationRegistry.CreateDefault());

        var recording = new SessionRecording(Identity, Array.Empty<RecordedOperation>(), rules);
        var restored = SessionRecording.FromJson(recording.ToJson()).RuleSet;
        Assert.Equal(expected, restored.ScrambleAllowed);
        Assert.Equal(expected, restored.SolveAllowed);
        Assert.Equal(expected, restored.UIEnabled);
        Assert.Equal(row, restored.AllowsSolveSlide(RecordedMoveAxis.Row));
        Assert.Equal(column, restored.AllowsSolveSlide(RecordedMoveAxis.Column));
        Assert.Equal(restored.HasSameMathematicalPermissions(new PuzzleRuleSet()), row && column);
    }

    [Fact]
    public void LiveInputTogglesAxesIndependentlyAndBlockedMovesLeaveRecordingUntouched()
    {
        var registry = OperationRegistry.CreateDefault();
        var rules = new PuzzleRuleSet("Rows", [ColumnSlide], [RowSlide], [Slide], registry);
        var grid = new Grid(3, 3, 32);
        var controller = new SessionRecordingController(grid, rules);
        var evaluator = new OperationRuleEvaluator(registry);
        var board = OperationBoardSnapshot.FromGrid(grid);
        var row = new SlideOperationIntent(RecordedMoveAxis.Row, 0, 1);
        var column = new SlideOperationIntent(RecordedMoveAxis.Column, 0, 1);

        Assert.False(evaluator.EvaluateScrambleRequest(board, row, rules).IsAccepted);
        Assert.True(evaluator.EvaluateScrambleRequest(board, column, rules).IsAccepted);
        Assert.True(evaluator.EvaluateUiRequest(board, row, rules).IsAccepted);
        Assert.Equal("Column slides aren’t allowed by Rows.", controller.GetSlideInputRejection(column.Axis));
        Assert.Throws<InvalidOperationException>(() => controller.MoveColumn(0, 1));
        controller.StartRecording();
        Assert.True(controller.SetRuntimeUIEnabled(RowSlide, false));
        Assert.Equal(new[] { ColumnSlide }, controller.RuntimeUIEnabled);
        Assert.Equal("Row slides are disabled in Live Input.", controller.GetSlideInputRejection(row.Axis));
        Assert.Throws<InvalidOperationException>(() => controller.MoveRow(0, 1));
        Assert.True(grid.IsSolved());
        Assert.Equal(0, controller.MoveCount);

        Assert.True(controller.SetRuntimeUIEnabled(RowSlide, true));
        Assert.Equal(new[] { Slide }, controller.RuntimeUIEnabled);
        Assert.True(controller.MoveRow(0, 1));
        var restored = SessionRecording.FromJson(controller.StopRecording().ToJson());
        Assert.Equal(new[] { Slide }, restored.RuleSet.UIEnabled);
        Assert.Equal(Slide, Assert.Single(restored.Operations).OperationId);
        controller.LoadPlayback(restored);
        Assert.True(controller.Step());
        Assert.False(grid.IsSolved());
        Assert.True(controller.StepBack());
        Assert.True(grid.IsSolved());
    }

    [Fact]
    public void DirectSwapCanUseBothAxesButPrimitiveFallbackCannot()
    {
        const string swap = PuzzleOperationIds.ColumnSwap;
        var direct = PuzzleRuleSet.CreateTwoWideSlide();
        var fallback = new PuzzleRuleSet("fallback", [swap], [RowSlide], [swap], direct.Registry);
        var evaluator = new OperationRuleEvaluator(direct.Registry);
        var board = new OperationBoardSnapshot(Identity);
        Assert.True(LensGestureOperationMapper.TryGetSwapVariant(PortableLensCell.Circle, PortableLensCell.Star,
            out _, out string variant));
        var intent = new PortableLensSwapIntent(swap, 0, 0, PortableLensCell.Circle, PortableLensCell.Star, variant);
        var evaluation = evaluator.EvaluateUiRequest(board, intent, direct);
        Assert.True(evaluation.IsAccepted, evaluation.RejectionReason);
        Assert.Equal(OperationMoveAccounting.LogicalOperation, evaluation.Plan!.Accounting);
        Assert.Contains(evaluation.Plan.Expansion, move => move.Axis == RecordedMoveAxis.Column);
        Assert.False(evaluator.EvaluateUiRequest(board, intent, fallback).IsAccepted);
        var unauthorized = new RecordedOperation(swap, intent.ToRecordingParameters(), evaluation.Plan.Expansion,
            OperationPlanPurpose.Solve, OperationMoveAccounting.PrimitiveMoves, evaluation.Plan.Expansion.Count);
        Assert.Throws<ArgumentException>(() => new SessionRecording(Identity, new[] { unauthorized }, fallback));
        Assert.Throws<InvalidOperationException>(() =>
            ShuffleChallengeFactory.GenerateRuleAware(3, 3, 1, fallback, evaluator, new Random(3)));
    }

    [Fact]
    public void AsymmetricRulesOnlyGenerateSlidesWithLegalInverses()
    {
        var registry = OperationRegistry.CreateDefault();
        var evaluator = new OperationRuleEvaluator(registry);
        var compatible = new PuzzleRuleSet("overlap", [Slide], [RowSlide], [Slide], registry);
        var challenge = ShuffleChallengeFactory.GenerateRuleAware(3, 3, 6, compatible, evaluator, new Random(3));
        Assert.All(challenge.ScrambleMoves, move => Assert.Equal(RecordedMoveAxis.Row, move.Axis));
        Assert.All(challenge.Solution.Moves, move => Assert.Equal(RecordedMoveAxis.Row, move.Axis));
        Assert.NotNull(challenge.Solution.GetVerifiedGoalMoveCost());

        var incompatible = new PuzzleRuleSet("disjoint", [ColumnSlide], [RowSlide], [Slide], registry);
        Assert.Throws<InvalidOperationException>(() =>
            ShuffleChallengeFactory.GenerateRuleAware(3, 3, 6, incompatible, evaluator, new Random(3)));
    }

    [Fact]
    public void RestrictedPresetCanRescrambleOnSmallBoardAndCheckThePreviousSolutionAxes()
    {
        var rules = PuzzleRuleSet.CreateTwoWideSlide();
        Assert.True(rules.IsBoardCompatible(2, 2));
        Assert.Contains("RowSlide remains available", rules.GetBoardCompatibilityMessage(2, 2));
        var evaluator = new OperationRuleEvaluator(rules.Registry);
        var random = new Random(9);
        var first = ShuffleChallengeFactory.GenerateRuleAware(2, 2, 3, rules, evaluator, random);
        var start = first.ScrambledCells;
        Assert.True(ShuffleChallengeFactory.HasVerifiedStart(start, first.Solution, rules));
        Assert.False(ShuffleChallengeFactory.HasVerifiedStart(start, first.Solution, rules.WithUIEnabled([ColumnSlide])));
        var newMath = new PuzzleRuleSet(rules.Name, rules.ScrambleAllowed, [ColumnSlide], [Slide], rules.Registry);
        Assert.False(ShuffleChallengeFactory.HasVerifiedStart(start, first.Solution, newMath));
        Assert.False(newMath.IsBoardCompatible(2, 2));

        var second = ShuffleChallengeFactory.GenerateRuleAware(start, first.Solution, 3, rules, evaluator, random);
        Assert.Equal(start, second.ScramblePlans[0].SourceBoard.Cells);
        Assert.All(second.Solution.Moves, move => Assert.Equal(RecordedMoveAxis.Row, move.Axis));
        Assert.NotNull(second.Solution.GetVerifiedGoalMoveCost());
    }
}
