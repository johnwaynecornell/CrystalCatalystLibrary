using Xunit;

namespace SlideScramble.Tests;

public class RuleAwareChallengeTests
{
    [Theory]
    [InlineData(PuzzleOperationIds.Slide, 2, 2)]
    [InlineData(PuzzleOperationIds.Shuffle, 5, 3)]
    [InlineData(PuzzleOperationIds.RowSwap, 4, 4)]
    [InlineData(PuzzleOperationIds.ColumnSwap, 3, 5)]
    public void GeneratesLegalReplayableInverse(string id, int width, int height)
    {
        var registry = OperationRegistry.CreateDefault();
        var rules = new PuzzleRuleSet("test", new[] { id }, new[] { id }, new[] { id }, registry);
        var evaluator = new OperationRuleEvaluator(registry);
        var challenge = ShuffleChallengeFactory.GenerateRuleAware(width, height, 12, rules, evaluator, new Random(23));
        var again = ShuffleChallengeFactory.GenerateRuleAware(width, height, 12, rules, evaluator, new Random(23));
        Assert.Equal(challenge.ScrambledCells, again.ScrambledCells);
        Assert.NotEqual(Solved(width, height), challenge.ScrambledCells);
        Assert.Equal(12, challenge.ScrambleOperations.Count);
        Assert.All(challenge.ScrambleOperations, op => Assert.Equal(id, op.OperationId));
        Assert.All(challenge.Solution.Operations, op => Assert.Equal(id, op.OperationId));
        VerifyOperations(Solved(width, height), challenge.ScrambleOperations, rules, evaluator, challenge.ScrambledCells);
        VerifyOperations(challenge.ScrambledCells, challenge.Solution.Operations, rules, evaluator, Solved(width, height));
        var recording = SessionRecording.FromJson(challenge.Solution.ToJson());
        var grid = new Grid(width, height, 32);
        var controller = new SessionRecordingController(grid);
        controller.LoadPlayback(recording);
        while (controller.Step()) { }
        Assert.Equal(Solved(width, height), OperationBoardSnapshot.FromGrid(grid).Cells);
        Assert.NotNull(recording.GetVerifiedGoalMoveCost());
    }

    [Fact]
    public void SlideFallbackIsIndividuallyEvaluatedAndRecorded()
    {
        var registry = OperationRegistry.CreateDefault();
        var rules = new PuzzleRuleSet("fallback", new[] { PuzzleOperationIds.RowSwap },
            new[] { PuzzleOperationIds.Slide }, new[] { PuzzleOperationIds.Slide }, registry);
        var challenge = ShuffleChallengeFactory.GenerateRuleAware(4, 4, 3, rules,
            new OperationRuleEvaluator(registry), new Random(7));
        Assert.All(challenge.Solution.Operations, op =>
        {
            Assert.Equal(PuzzleOperationIds.Slide, op.OperationId);
            Assert.Equal(OperationMoveAccounting.LogicalOperation, op.Accounting);
        });
        VerifyOperations(challenge.ScrambledCells, challenge.Solution.Operations, rules,
            new OperationRuleEvaluator(registry), Solved(4, 4));
        Assert.NotNull(challenge.Solution.GetVerifiedGoalMoveCost());
    }

    [Fact]
    public void MixedRulesChooseCompatibleOperationsAndPreserveRules()
    {
        var registry = OperationRegistry.CreateDefault();
        var rules = new PuzzleRuleSet("mixed", new[] { PuzzleOperationIds.Shuffle, PuzzleOperationIds.Slide },
            new[] { PuzzleOperationIds.Slide }, new[] { PuzzleOperationIds.Slide }, registry);
        var challenge = ShuffleChallengeFactory.GenerateRuleAware(3, 3, 20, rules,
            new OperationRuleEvaluator(registry), new Random(5));
        Assert.Equal(rules.ScrambleAllowed, challenge.Solution.RuleSet.ScrambleAllowed);
        Assert.Equal(rules.UIEnabled, challenge.Solution.RuleSet.UIEnabled);
        VerifyOperations(Solved(3, 3), challenge.ScrambleOperations, rules, new OperationRuleEvaluator(registry), challenge.ScrambledCells);
        VerifyOperations(challenge.ScrambledCells, challenge.Solution.Operations, rules, new OperationRuleEvaluator(registry), Solved(3, 3));
        Assert.NotNull(challenge.Solution.GetVerifiedGoalMoveCost());
    }

    [Fact]
    public void UiEnabledLensWithSlideOnlySolveUsesPrimitiveAccounting()
    {
        var registry = OperationRegistry.CreateDefault();
        var rules = new PuzzleRuleSet("expanded", new[] { PuzzleOperationIds.RowSwap },
            new[] { PuzzleOperationIds.Slide }, new[] { PuzzleOperationIds.RowSwap }, registry);
        var evaluator = new OperationRuleEvaluator(registry);
        var challenge = ShuffleChallengeFactory.GenerateRuleAware(3, 4, 5, rules, evaluator, new Random(2));
        Assert.All(challenge.Solution.Operations, op =>
        {
            Assert.Equal(PuzzleOperationIds.RowSwap, op.OperationId);
            Assert.Equal(OperationMoveAccounting.PrimitiveMoves, op.Accounting);
            Assert.Equal(op.PrimitiveMoves.Count, op.MoveCost);
        });
        VerifyOperations(challenge.ScrambledCells, challenge.Solution.Operations, rules, evaluator, Solved(3, 4));
    }

    [Fact]
    public void RejectsIncompatibleUnavailableAndTooSmallOperations()
    {
        var registry = OperationRegistry.CreateDefault();
        var evaluator = new OperationRuleEvaluator(registry);
        var noUi = new PuzzleRuleSet("no UI", new[] { PuzzleOperationIds.Slide },
            new[] { PuzzleOperationIds.Slide }, Array.Empty<string>(), registry);
        Assert.Throws<InvalidOperationException>(() => ShuffleChallengeFactory.GenerateRuleAware(3, 3, 1, noUi, evaluator, new Random(1)));
        var shuffle = PuzzleRuleSet.CreateShuffleLogic(registry);
        Assert.Throws<InvalidOperationException>(() => ShuffleChallengeFactory.GenerateRuleAware(2, 2, 1, shuffle, evaluator, new Random(1)));
        var emptyRegistry = new OperationRegistry();
        var unregistered = new PuzzleRuleSet("unregistered", new[] { PuzzleOperationIds.RowSwap },
            new[] { PuzzleOperationIds.RowSwap }, new[] { PuzzleOperationIds.RowSwap }, emptyRegistry);
        Assert.Throws<InvalidOperationException>(() => ShuffleChallengeFactory.GenerateRuleAware(3, 3, 1, unregistered,
            new OperationRuleEvaluator(emptyRegistry), new Random(1)));
        var incompatible = new PuzzleRuleSet("incompatible", new[] { PuzzleOperationIds.RowSwap },
            new[] { PuzzleOperationIds.ColumnSwap }, new[] { PuzzleOperationIds.RowSwap }, registry);
        Assert.Throws<InvalidOperationException>(() => ShuffleChallengeFactory.GenerateRuleAware(3, 3, 1, incompatible, evaluator, new Random(1)));
    }

    [Fact]
    public void RejectsEvaluatorWhoseResolverDiffersFromTheRulesRegistry()
    {
        var rules = PuzzleRuleSet.CreateShuffleLogic(OperationRegistry.CreateDefault());
        var otherRegistry = new OperationRegistry(new IPuzzleOperation[] { new DifferentShuffle() });
        Assert.Throws<InvalidOperationException>(() => ShuffleChallengeFactory.GenerateRuleAware(3, 3, 1, rules,
            new OperationRuleEvaluator(otherRegistry), new Random(1)));
    }

    private sealed class DifferentShuffle : IPuzzleOperation
    {
        public string Id => PuzzleOperationIds.Shuffle;

        public OperationResolution Resolve(OperationBoardSnapshot board, IOperationIntent intent)
        {
            var shuffle = (ShuffleOperationIntent)intent;
            int delta = shuffle.StartCell < shuffle.TargetCell ? 1 : -1;
            return OperationResolution.Success(Id, new[] { new RecordedMove(RecordedMoveAxis.Row, 0, delta) });
        }
    }

    private static void VerifyOperations(int[,] start, IEnumerable<RecordedOperation> operations, PuzzleRuleSet rules,
        OperationRuleEvaluator evaluator, int[,] expected)
    {
        var session = new SlideScrambleSession(start);
        foreach (var operation in operations)
        {
            var parameters = operation.Parameters;
            IOperationIntent intent;
            if (operation.OperationId == PuzzleOperationIds.Slide)
                intent = new SlideOperationIntent(Enum.Parse<RecordedMoveAxis>(parameters.GetProperty("axis").GetString()!),
                    parameters.GetProperty("index").GetInt32(), parameters.GetProperty("delta").GetInt32());
            else
            {
                int x = parameters.GetProperty("lensAnchor")[0].GetInt32();
                int y = parameters.GetProperty("lensAnchor")[1].GetInt32();
                var from = Enum.Parse<PortableLensCell>(parameters.GetProperty("startCell").GetString()!);
                var to = Enum.Parse<PortableLensCell>(parameters.GetProperty("targetCell").GetString()!);
                intent = operation.OperationId == PuzzleOperationIds.Shuffle
                    ? new ShuffleOperationIntent(x, y, from, to)
                    : new PortableLensSwapIntent(operation.OperationId, x, y, from, to,
                        parameters.GetProperty("variant").GetString()!);
            }
            var snapshot = OperationBoardSnapshot.FromSession(session);
            var evaluation = operation.Purpose == OperationPlanPurpose.Scramble
                ? evaluator.EvaluateScrambleRequest(snapshot, intent, rules)
                : evaluator.EvaluateUiRequest(snapshot, intent, rules);
            Assert.True(evaluation.IsAccepted, evaluation.RejectionReason);
            Assert.Equal(evaluation.Plan!.Expansion, operation.PrimitiveMoves);
            Assert.Equal(evaluation.Plan.Accounting, operation.Accounting);
            Assert.Equal(evaluation.Plan.MoveCost, operation.MoveCost);
            foreach (var move in operation.PrimitiveMoves)
            {
                if (move.Axis == RecordedMoveAxis.Row) session.MoveRow(move.Index, move.Delta, 0);
                else session.MoveColumn(move.Index, move.Delta, 0);
            }
        }
        Assert.Equal(expected, session.Cells);
    }

    private static int[,] Solved(int width, int height)
    {
        var cells = new int[height, width];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++) cells[y, x] = y * width + x;
        return cells;
    }
}