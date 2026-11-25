using Xunit;

namespace SlideScramble.Tests;

public class OperationRulesTests
{
    private const string CompositeId = PuzzleOperationIds.ColumnSwap;

    [Fact]
    public void DefaultRulesPermitOnlyOrdinarySlides()
    {
        var rules = new PuzzleRuleSet();

        Assert.Equal(new[] { PuzzleOperationIds.Slide }, rules.ScrambleAllowed);
        Assert.Equal(new[] { PuzzleOperationIds.Slide }, rules.SolveAllowed);
        Assert.Equal(new[] { PuzzleOperationIds.Slide }, rules.UIEnabled);
    }

    [Fact]
    public void RuleSetsAreIndependentDeduplicateAndSnapshotInput()
    {
        var registry = RegistryWithComposite();
        var scramble = new List<string> { CompositeId, CompositeId };
        var solve = new List<string> { PuzzleOperationIds.Slide };
        var ui = new List<string> { CompositeId };
        var rules = new PuzzleRuleSet("Independent", scramble, solve, ui, registry);
        scramble.Clear();
        solve.Clear();
        ui.Clear();

        Assert.Equal(new[] { CompositeId }, rules.ScrambleAllowed);
        Assert.Equal(new[] { PuzzleOperationIds.Slide }, rules.SolveAllowed);
        Assert.Equal(new[] { CompositeId }, rules.UIEnabled);
        Assert.NotSame(rules, rules.Snapshot());
    }

    [Fact]
    public void UnknownOperationIdsAreRejected()
    {
        var registry = OperationRegistry.CreateDefault();
        Assert.Throws<ArgumentException>(() => new PuzzleRuleSet("Invalid", new[] { "NotRegistered" },
            Array.Empty<string>(), Array.Empty<string>(), registry));

        var evaluator = new OperationRuleEvaluator(registry);
        var result = evaluator.EvaluateUiRequest(Board(), new UnknownIntent(), new PuzzleRuleSet());
        Assert.False(result.IsAccepted);
        Assert.Contains("Unknown operation ID", result.RejectionReason);
    }

    [Fact]
    public void UiEnabledCompositeFallsBackToSolveAllowedSlidesAndCountsEachPrimitive()
    {
        var registry = RegistryWithComposite();
        var rules = new PuzzleRuleSet("Slide fallback", new[] { PuzzleOperationIds.Slide },
            new[] { PuzzleOperationIds.Slide }, new[] { CompositeId }, registry);
        var result = new OperationRuleEvaluator(registry).EvaluateUiRequest(Board(), new CompositeIntent(1), rules);

        Assert.True(result.IsAccepted);
        Assert.Equal(OperationMoveAccounting.PrimitiveMoves, result.Plan!.Accounting);
        Assert.Equal(2, result.Plan.MoveCost);
        Assert.Equal(2, result.Plan.Expansion.Count);
    }

    [Fact]
    public void DirectlySolveAllowedCompositeCountsAsOneLogicalMove()
    {
        var registry = RegistryWithComposite();
        var rules = new PuzzleRuleSet("Direct composite", Array.Empty<string>(), new[] { CompositeId },
            new[] { CompositeId }, registry);
        var result = new OperationRuleEvaluator(registry).EvaluateUiRequest(Board(), new CompositeIntent(1), rules);

        Assert.True(result.IsAccepted);
        Assert.Equal(OperationMoveAccounting.LogicalOperation, result.Plan!.Accounting);
        Assert.Equal(1, result.Plan.MoveCost);
        Assert.Equal(2, result.Plan.Expansion.Count);
    }

    [Fact]
    public void ExpansionWithoutSolveAllowedSlidesIsRejectedWithoutMutation()
    {
        var registry = RegistryWithComposite();
        var cells = Cells();
        var board = new OperationBoardSnapshot(cells);
        var before = board.Cells;
        var rules = new PuzzleRuleSet("No primitives", Array.Empty<string>(), Array.Empty<string>(),
            new[] { CompositeId }, registry);
        var result = new OperationRuleEvaluator(registry).EvaluateUiRequest(board, new CompositeIntent(1), rules);

        Assert.False(result.IsAccepted);
        Assert.Null(result.Plan);
        Assert.Equal(before, board.Cells);
        Assert.Equal(cells, board.Cells);
    }

    [Fact]
    public void InvalidPrimitiveExpansionIsRejectedWithoutMutation()
    {
        var registry = new OperationRegistry(new IPuzzleOperation[] { new InvalidExpansionOperation() });
        var board = Board();
        var before = board.Cells;
        var rules = new PuzzleRuleSet("Invalid expansion", Array.Empty<string>(),
            new[] { PuzzleOperationIds.Slide }, new[] { PuzzleOperationIds.RowSwap }, registry);
        var result = new OperationRuleEvaluator(registry).EvaluateUiRequest(board, new InvalidExpansionIntent(), rules);

        Assert.False(result.IsAccepted);
        Assert.Null(result.Plan);
        Assert.Equal(before, board.Cells);
    }

    [Fact]
    public void UnauthorizedRequestsDoNotInvokeOperationResolver()
    {
        var operation = new CountingOperation();
        var registry = new OperationRegistry(new IPuzzleOperation[] { operation });
        var rules = new PuzzleRuleSet("Disabled", Array.Empty<string>(), Array.Empty<string>(),
            Array.Empty<string>(), registry);
        var evaluator = new OperationRuleEvaluator(registry);

        var ui = evaluator.EvaluateUiRequest(Board(), new CountingIntent(), rules);
        var scramble = evaluator.EvaluateScrambleRequest(Board(), new CountingIntent(), rules);

        Assert.False(ui.IsAccepted);
        Assert.False(scramble.IsAccepted);
        Assert.Equal(0, operation.ResolveCount);
    }

    [Fact]
    public void ScrambleAuthorizationIsIndependentFromUiAndSolveAuthorization()
    {
        var registry = RegistryWithComposite();
        var rules = new PuzzleRuleSet("Scramble only", new[] { CompositeId }, Array.Empty<string>(),
            Array.Empty<string>(), registry);
        var evaluator = new OperationRuleEvaluator(registry);

        var scramble = evaluator.EvaluateScrambleRequest(Board(), new CompositeIntent(1), rules);
        var ui = evaluator.EvaluateUiRequest(Board(), new CompositeIntent(1), rules);

        Assert.True(scramble.IsAccepted);
        Assert.Equal(OperationPlanPurpose.Scramble, scramble.Plan!.Purpose);
        Assert.Null(scramble.Plan.MoveCost);
        Assert.Equal(OperationMoveAccounting.None, scramble.Plan.Accounting);
        Assert.False(ui.IsAccepted);
    }

    [Theory]
    [InlineData(RecordedMoveAxis.Row, 2, 1)]
    [InlineData(RecordedMoveAxis.Column, 0, 0)]
    public void InvalidSlideParametersAreRejectedWithoutMutatingBoard(RecordedMoveAxis axis, int index, int delta)
    {
        var board = Board();
        var before = board.Cells;
        var rules = new PuzzleRuleSet();
        var result = new OperationRuleEvaluator(OperationRegistry.CreateDefault())
            .EvaluateUiRequest(board, new SlideOperationIntent(axis, index, delta), rules);

        Assert.False(result.IsAccepted);
        Assert.Null(result.Plan);
        Assert.Equal(before, board.Cells);
    }

    [Fact]
    public void NullOperationIntentIsRejectedWithoutEffect()
    {
        var board = Board();
        var before = board.Cells;
        var result = new OperationRuleEvaluator(OperationRegistry.CreateDefault())
            .EvaluateUiRequest(board, null, new PuzzleRuleSet());

        Assert.False(result.IsAccepted);
        Assert.Equal(before, board.Cells);
    }

    [Fact]
    public void SnapshotAndResolutionArePureAndDeterministic()
    {
        var cells = Cells();
        var board = new OperationBoardSnapshot(cells);
        cells[0, 0] = 5;
        var rules = new PuzzleRuleSet();
        var evaluator = new OperationRuleEvaluator(OperationRegistry.CreateDefault());
        var intent = new SlideOperationIntent(RecordedMoveAxis.Row, 0, 1);

        var first = evaluator.EvaluateUiRequest(board, intent, rules).Plan!;
        var second = evaluator.EvaluateUiRequest(board, intent, rules).Plan!;

        Assert.Equal(0, board.TileAt(0, 0));
        Assert.Equal(first.Expansion, second.Expansion);
        Assert.Equal(first.SourceBoard.Cells, second.SourceBoard.Cells);
        Assert.Equal(1, first.MoveCost);
    }

    [Fact]
    public void ReservedLensIdsAreKnownButDoNotHaveDefaultResolvers()
    {
        var registry = OperationRegistry.CreateDefault();
        Assert.Contains(PuzzleOperationIds.Shuffle, registry.KnownOperationIds);
        Assert.DoesNotContain(PuzzleOperationIds.Shuffle, registry.RegisteredOperationIds);

        var rules = new PuzzleRuleSet("Reserved", new[] { PuzzleOperationIds.Shuffle }, Array.Empty<string>(),
            Array.Empty<string>(), registry);
        var result = new OperationRuleEvaluator(registry).EvaluateScrambleRequest(Board(), new ReservedIntent(), rules);
        Assert.False(result.IsAccepted);
        Assert.Contains("no registered resolver", result.RejectionReason);
    }

    private static OperationRegistry RegistryWithComposite() =>
        new(new IPuzzleOperation[] { new CompositeOperation() });

    private static OperationBoardSnapshot Board() => new(Cells());

    private static int[,] Cells() => new[,]
    {
        { 0, 1, 2 },
        { 3, 4, 5 }
    };

    private sealed record CompositeIntent(int Parameters) : IOperationIntent
    {
        public string OperationId => CompositeId;
    }

    private sealed record UnknownIntent : IOperationIntent
    {
        public string OperationId => "NotRegistered";
    }

    private sealed record ReservedIntent : IOperationIntent
    {
        public string OperationId => PuzzleOperationIds.Shuffle;
    }

    private sealed record InvalidExpansionIntent : IOperationIntent
    {
        public string OperationId => PuzzleOperationIds.RowSwap;
    }

    private sealed record CountingIntent : IOperationIntent
    {
        public string OperationId => PuzzleOperationIds.RowSwap;
    }

    private sealed class CompositeOperation : IPuzzleOperation
    {
        public string Id => CompositeId;

        public OperationResolution Resolve(OperationBoardSnapshot board, IOperationIntent intent)
        {
            if (intent is not CompositeIntent { Parameters: 1 })
                return OperationResolution.Rejected(Id, "Composite parameter is invalid.");

            return OperationResolution.Success(Id, new[]
            {
                new RecordedMove(RecordedMoveAxis.Row, 0, 1),
                new RecordedMove(RecordedMoveAxis.Column, 1, -1)
            });
        }
    }

    private sealed class InvalidExpansionOperation : IPuzzleOperation
    {
        public string Id => PuzzleOperationIds.RowSwap;

        public OperationResolution Resolve(OperationBoardSnapshot board, IOperationIntent intent) =>
            OperationResolution.Success(Id, new[] { new RecordedMove(RecordedMoveAxis.Row, board.Height, 1) });
    }

    private sealed class CountingOperation : IPuzzleOperation
    {
        public string Id => PuzzleOperationIds.RowSwap;
        public int ResolveCount { get; private set; }

        public OperationResolution Resolve(OperationBoardSnapshot board, IOperationIntent intent)
        {
            ResolveCount++;
            return OperationResolution.Success(Id, new[] { new RecordedMove(RecordedMoveAxis.Row, 0, 1) });
        }
    }
}