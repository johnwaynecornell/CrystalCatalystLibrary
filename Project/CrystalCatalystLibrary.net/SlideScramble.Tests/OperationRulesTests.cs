using Xunit;

namespace SlideScramble.Tests;

public class OperationRulesTests
{
    private const string CompositeId = PuzzleOperationIds.ColumnSwap;

    [Fact]
    public void EightPresetsHaveTheirExactIndependentOperationSets()
    {
        var registry = OperationRegistry.CreateDefault();
        AssertPreset(PuzzleRuleSet.CreateClassic(registry), "Classic", PuzzleOperationIds.Slide);
        AssertPreset(PuzzleRuleSet.CreateShuffleLogic(registry), "ShuffleLogic", PuzzleOperationIds.Shuffle);
        AssertPreset(PuzzleRuleSet.CreateSwapLogic(registry), "SwapLogic", PuzzleOperationIds.ColumnSwap,
            PuzzleOperationIds.RowSwap);
        AssertPreset(PuzzleRuleSet.CreateTwoWide(registry), "TwoWide", PuzzleOperationIds.Slide,
            PuzzleOperationIds.ColumnSwap);
        AssertPreset(PuzzleRuleSet.CreateTwoTall(registry), "TwoTall", PuzzleOperationIds.Slide,
            PuzzleOperationIds.RowSwap);
        AssertPreset(PuzzleRuleSet.CreateHybrid(registry), "Hybrid", PuzzleOperationIds.Slide,
            PuzzleOperationIds.Shuffle);
        AssertPreset(PuzzleRuleSet.CreateZeroSlide(registry), "ZeroSlide", PuzzleOperationIds.Shuffle,
            PuzzleOperationIds.ColumnSwap, PuzzleOperationIds.RowSwap);
        AssertPreset(PuzzleRuleSet.CreateFreeStyle(registry), "FreeStyle", PuzzleOperationIds.Slide,
            PuzzleOperationIds.Shuffle, PuzzleOperationIds.ColumnSwap, PuzzleOperationIds.RowSwap);
    }

    [Fact]
    public void PresetsRequireEveryResolverTheyEnable()
    {
        var incompleteRegistry = new OperationRegistry();
        Assert.Throws<ArgumentException>(() => PuzzleRuleSet.CreateShuffleLogic(incompleteRegistry));
        Assert.Throws<ArgumentException>(() => PuzzleRuleSet.CreateSwapLogic(incompleteRegistry));
        Assert.Throws<ArgumentException>(() => PuzzleRuleSet.CreateTwoWide(incompleteRegistry));
        Assert.Throws<ArgumentException>(() => PuzzleRuleSet.CreateTwoTall(incompleteRegistry));
        Assert.Throws<ArgumentException>(() => PuzzleRuleSet.CreateHybrid(incompleteRegistry));
        Assert.Throws<ArgumentException>(() => PuzzleRuleSet.CreateZeroSlide(incompleteRegistry));
        Assert.Throws<ArgumentException>(() => PuzzleRuleSet.CreateFreeStyle(incompleteRegistry));
        Assert.Equal(new[] { PuzzleOperationIds.Slide }, PuzzleRuleSet.CreateClassic(incompleteRegistry).UIEnabled);
    }

    [Fact]
    public void TwoWideAndTwoTallDirectlyCountTheirSlideAndSwapOperations()
    {
        var board = CreateSolvedSnapshot(4);
        var twoWide = PuzzleRuleSet.CreateTwoWide();
        var wideEvaluator = new OperationRuleEvaluator(twoWide.Registry);
        var wideSlide = wideEvaluator.EvaluateUiRequest(board,
            new SlideOperationIntent(RecordedMoveAxis.Row, 0, 1), twoWide);
        var wideSwap = wideEvaluator.EvaluateUiRequest(board,
            new PortableLensSwapIntent(PuzzleOperationIds.ColumnSwap, 0, 0, PortableLensCell.Circle,
                PortableLensCell.Star, "UpLeft"), twoWide);
        AssertDirectLogicalMove(wideSlide, 1);
        AssertDirectLogicalMove(wideSwap, 8);

        var twoTall = PuzzleRuleSet.CreateTwoTall();
        var tallEvaluator = new OperationRuleEvaluator(twoTall.Registry);
        var tallSlide = tallEvaluator.EvaluateUiRequest(board,
            new SlideOperationIntent(RecordedMoveAxis.Column, 0, 1), twoTall);
        var tallSwap = tallEvaluator.EvaluateUiRequest(board,
            new PortableLensSwapIntent(PuzzleOperationIds.RowSwap, 0, 0, PortableLensCell.Circle,
                PortableLensCell.Triangle, "LeftUp"), twoTall);
        AssertDirectLogicalMove(tallSlide, 1);
        AssertDirectLogicalMove(tallSwap, 8);
    }

    [Fact]
    public void TwoWideAndTwoTallUseSlideFallbackForOtherLensOperations()
    {
        var board = CreateSolvedSnapshot(4);
        var allLensInputs = new[]
        {
            PuzzleOperationIds.Slide,
            PuzzleOperationIds.Shuffle,
            PuzzleOperationIds.ColumnSwap,
            PuzzleOperationIds.RowSwap
        };

        var twoWide = PuzzleRuleSet.CreateTwoWide().WithUIEnabled(allLensInputs);
        var wideEvaluator = new OperationRuleEvaluator(twoWide.Registry);
        var wideRowSwap = wideEvaluator.EvaluateUiRequest(board,
            new PortableLensSwapIntent(PuzzleOperationIds.RowSwap, 0, 0, PortableLensCell.Circle,
                PortableLensCell.Triangle, "LeftUp"), twoWide);
        var wideShuffle = wideEvaluator.EvaluateUiRequest(board,
            new ShuffleOperationIntent(0, 0, PortableLensCell.Circle, PortableLensCell.Star), twoWide);
        AssertPrimitiveFallback(wideRowSwap, 8);
        AssertPrimitiveFallback(wideShuffle, 4);

        var twoTall = PuzzleRuleSet.CreateTwoTall().WithUIEnabled(allLensInputs);
        var tallEvaluator = new OperationRuleEvaluator(twoTall.Registry);
        var tallColumnSwap = tallEvaluator.EvaluateUiRequest(board,
            new PortableLensSwapIntent(PuzzleOperationIds.ColumnSwap, 0, 0, PortableLensCell.Circle,
                PortableLensCell.Star, "UpLeft"), twoTall);
        var tallShuffle = tallEvaluator.EvaluateUiRequest(board,
            new ShuffleOperationIntent(0, 0, PortableLensCell.Circle, PortableLensCell.Star), twoTall);
        AssertPrimitiveFallback(tallColumnSwap, 8);
        AssertPrimitiveFallback(tallShuffle, 4);
    }

    [Fact]
    public void BoardCompatibilityAllowsSlideCapableRulesButRejectsLensOnlyPresetsBelowThreeByThree()
    {
        Assert.True(PuzzleRuleSet.CreateClassic().IsBoardCompatible(2, 2));
        Assert.False(PuzzleRuleSet.CreateShuffleLogic().IsBoardCompatible(2, 2));
        Assert.False(PuzzleRuleSet.CreateSwapLogic().IsBoardCompatible(2, 2));
        Assert.False(PuzzleRuleSet.CreateZeroSlide().IsBoardCompatible(2, 2));
        Assert.True(PuzzleRuleSet.CreateTwoWide().IsBoardCompatible(2, 2));
        Assert.True(PuzzleRuleSet.CreateTwoTall().IsBoardCompatible(2, 2));
        Assert.True(PuzzleRuleSet.CreateFreeStyle().IsBoardCompatible(2, 2));
        Assert.Contains("Slide remains available", PuzzleRuleSet.CreateTwoWide().GetBoardCompatibilityMessage(2, 2));
        Assert.Null(PuzzleRuleSet.CreateFreeStyle().GetBoardCompatibilityMessage(3, 3));
    }

    [Fact]
    public void DefaultRulesPermitOnlyOrdinarySlides()
    {
        var rules = new PuzzleRuleSet();
        var registry = OperationRegistry.CreateDefault();

        Assert.Equal(new[] { PuzzleOperationIds.Slide }, rules.ScrambleAllowed);
        Assert.Equal(new[] { PuzzleOperationIds.Slide }, rules.SolveAllowed);
        Assert.Equal(new[] { PuzzleOperationIds.Slide }, rules.UIEnabled);
        Assert.Contains(PuzzleOperationIds.Shuffle, registry.RegisteredOperationIds);
    }

    [Fact]
    public void HybridPresetSupportsSlideAndShuffleAndRequiresShuffleResolver()
    {
        var rules = PuzzleRuleSet.CreateHybrid();
        var expected = new[] { PuzzleOperationIds.Shuffle, PuzzleOperationIds.Slide };
        Assert.Equal("Hybrid", rules.Name);
        Assert.Equal(expected, rules.ScrambleAllowed);
        Assert.Equal(expected, rules.SolveAllowed);
        Assert.Equal(expected, rules.UIEnabled);
        Assert.Throws<ArgumentException>(() => PuzzleRuleSet.CreateHybrid(new OperationRegistry()));
    }

    [Fact]
    public void ShuffleLogicPresetAllowsOnlyShuffleAndChargesOneLogicalMove()
    {
        var registry = OperationRegistry.CreateDefault();
        var rules = PuzzleRuleSet.CreateShuffleLogic(registry);
        var intent = new ShuffleOperationIntent(2, 1, PortableLensCell.Circle, PortableLensCell.Star);
        var result = new OperationRuleEvaluator(registry).EvaluateUiRequest(
            new OperationBoardSnapshot(new int[3, 3]
            {
                { 0, 1, 2 },
                { 3, 4, 5 },
                { 6, 7, 8 }
            }), intent, rules);

        Assert.Equal("ShuffleLogic", rules.Name);
        Assert.Equal(new[] { PuzzleOperationIds.Shuffle }, rules.ScrambleAllowed);
        Assert.Equal(new[] { PuzzleOperationIds.Shuffle }, rules.SolveAllowed);
        Assert.Equal(new[] { PuzzleOperationIds.Shuffle }, rules.UIEnabled);
        Assert.True(result.IsAccepted, result.RejectionReason);
        Assert.Equal(OperationMoveAccounting.LogicalOperation, result.Plan!.Accounting);
        Assert.Equal(1, result.Plan.MoveCost);
        Assert.Equal(4, result.Plan.Expansion.Count);
    }

    [Fact]
    public void UiEnabledShuffleFallsBackToFourAuthorizedSlidesWhenDirectSolveIsDisallowed()
    {
        var registry = OperationRegistry.CreateDefault();
        var rules = new PuzzleRuleSet("Slide fallback", new[] { PuzzleOperationIds.Slide },
            new[] { PuzzleOperationIds.Slide }, new[] { PuzzleOperationIds.Shuffle }, registry);
        var result = new OperationRuleEvaluator(registry).EvaluateUiRequest(
            new OperationBoardSnapshot(new int[3, 3]
            {
                { 0, 1, 2 },
                { 3, 4, 5 },
                { 6, 7, 8 }
            }), new ShuffleOperationIntent(0, 0, PortableLensCell.Circle, PortableLensCell.Star), rules);

        Assert.True(result.IsAccepted, result.RejectionReason);
        Assert.Equal(OperationMoveAccounting.PrimitiveMoves, result.Plan!.Accounting);
        Assert.Equal(4, result.Plan.MoveCost);
        Assert.Equal(4, result.Plan.Expansion.Count);
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
        var registry = new OperationRegistry();
        Assert.Contains(PuzzleOperationIds.Shuffle, registry.KnownOperationIds);
        Assert.DoesNotContain(PuzzleOperationIds.Shuffle, registry.RegisteredOperationIds);

        var rules = new PuzzleRuleSet("Reserved", new[] { PuzzleOperationIds.Shuffle }, Array.Empty<string>(),
            Array.Empty<string>(), registry);
        var result = new OperationRuleEvaluator(registry).EvaluateScrambleRequest(Board(), new ReservedIntent(), rules);
        Assert.False(result.IsAccepted);
        Assert.Contains("no registered resolver", result.RejectionReason);
    }

    private static void AssertPreset(PuzzleRuleSet rules, string name, params string[] operationIds)
    {
        string[] expected = operationIds.Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(name, rules.Name);
        Assert.Equal(expected, rules.ScrambleAllowed);
        Assert.Equal(expected, rules.SolveAllowed);
        Assert.Equal(expected, rules.UIEnabled);
    }

    private static void AssertDirectLogicalMove(OperationEvaluation result, int expansionCount)
    {
        Assert.True(result.IsAccepted, result.RejectionReason);
        Assert.Equal(OperationMoveAccounting.LogicalOperation, result.Plan!.Accounting);
        Assert.Equal(1, result.Plan.MoveCost);
        Assert.Equal(expansionCount, result.Plan.Expansion.Count);
    }

    private static void AssertPrimitiveFallback(OperationEvaluation result, int moveCost)
    {
        Assert.True(result.IsAccepted, result.RejectionReason);
        Assert.Equal(OperationMoveAccounting.PrimitiveMoves, result.Plan!.Accounting);
        Assert.Equal(moveCost, result.Plan.MoveCost);
        Assert.Equal(moveCost, result.Plan.Expansion.Count);
    }

    private static OperationBoardSnapshot CreateSolvedSnapshot(int size)
    {
        var cells = new int[size, size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            cells[y, x] = y * size + x;
        return new OperationBoardSnapshot(cells);
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