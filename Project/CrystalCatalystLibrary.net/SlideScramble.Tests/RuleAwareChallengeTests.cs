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
        foreach (var (plan, operation) in challenge.ScramblePlans.Zip(challenge.ScrambleOperations))
        {
            var visual = new OperationAnimationPlan(plan.SourceBoard, plan);
            var timeline = new OperationAnimationTimeline(visual, OperationAnimationStyle.Primitive);
            if (operation.Parameters.TryGetProperty("lensAnchor", out var anchor))
                Assert.Equal((anchor[0].GetInt32(), anchor[1].GetInt32()),
                    timeline.GetVisibleLensAnchor(showOverlay: true));
            else
                Assert.Null(timeline.GetVisibleLensAnchor(showOverlay: true));
            Assert.Null(timeline.GetVisibleLensAnchor(showOverlay: false));
        }
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

    [Theory]
    [InlineData("Classic")]
    [InlineData("ShuffleLogic")]
    [InlineData("SwapLogic")]
    [InlineData("TwoWide")]
    [InlineData("TwoTall")]
    [InlineData("Hybrid")]
    [InlineData("ZeroSlide")]
    [InlineData("FreeStyle")]
    public void EveryPresetGeneratesOnlyAllowedScramblesAndAReplayableSolution(string presetName)
    {
        var registry = OperationRegistry.CreateDefault();
        var rules = CreatePreset(presetName, registry);
        var evaluator = new OperationRuleEvaluator(registry);
        var challenge = ShuffleChallengeFactory.GenerateRuleAware(4, 4, 8, rules, evaluator, new Random(87));

        Assert.All(challenge.ScrambleOperations,
            operation => Assert.Contains(operation.OperationId, rules.ScrambleAllowed));
        VerifyOperations(Solved(4, 4), challenge.ScrambleOperations, rules, evaluator, challenge.ScrambledCells);
        VerifyOperations(challenge.ScrambledCells, challenge.Solution.Operations, rules, evaluator, Solved(4, 4));
        Assert.NotNull(challenge.Solution.GetVerifiedGoalMoveCost());

        var restored = SessionRecording.FromJson(challenge.Solution.ToJson());
        Assert.Equal(rules.Name, restored.RuleSet.Name);
        Assert.Equal(rules.ScrambleAllowed, restored.RuleSet.ScrambleAllowed);
        Assert.Equal(rules.SolveAllowed, restored.RuleSet.SolveAllowed);
        Assert.Equal(rules.UIEnabled, restored.RuleSet.UIEnabled);
        Assert.Equal(8, restored.Operations.Count);

        var grid = new Grid(4, 4, 32);
        var controller = new SessionRecordingController(grid);
        controller.LoadPlayback(restored);
        while (controller.Step()) { }
        Assert.Equal(Solved(4, 4), OperationBoardSnapshot.FromGrid(grid).Cells);
    }

    [Fact]
    public void SwapLabyrinthCatalogEntriesKeepTheirSwapRulesAndReplayToIdentity()
    {
        string catalogPath = FindCatalogPath();
        var catalog = ChallengeCatalog.FromJson(File.ReadAllText(catalogPath));
        Assert.Contains("Swap Labyrinth - Full", catalog.Names);
        Assert.Contains("Swap Labyrinth - 3 cycle Finish", catalog.Names);

        VerifySwapLabyrinth(catalog, "Swap Labyrinth - Full", 12);
        VerifySwapLabyrinth(catalog, "Swap Labyrinth - 3 cycle Finish", 4);
    }

    [Theory]
    [InlineData(PuzzleOperationIds.Shuffle, PuzzleOperationIds.Shuffle)]
    [InlineData(PuzzleOperationIds.RowSwap, PuzzleOperationIds.Slide)]
    public void RepeatedScrambleComposesWithTheCurrentBoardAndReplaysToIdentity(string scrambleId, string solveId)
    {
        var registry = OperationRegistry.CreateDefault();
        var rules = new PuzzleRuleSet("test", new[] { scrambleId }, new[] { solveId }, new[] { solveId }, registry);
        var evaluator = new OperationRuleEvaluator(registry);
        var random = new Random(23);
        var grid = new Grid(4, 4, 32);
        var controller = new SessionRecordingController(grid, rules);
        var first = ShuffleChallengeFactory.GenerateRuleAware(4, 4, 4, rules, evaluator, random);
        controller.LoadPlayback(first.Solution);
        controller.StopPlayback();

        var start = OperationBoardSnapshot.FromGrid(grid).Cells;
        Assert.False(grid.IsSolved());
        var second = ShuffleChallengeFactory.GenerateRuleAware(start, first.Solution, 4, rules, evaluator, random);
        Assert.Equal(start, second.ScramblePlans[0].SourceBoard.Cells);
        Assert.Equal(start, OperationBoardSnapshot.FromGrid(grid).Cells);
        Assert.All(second.ScrambleOperations, operation => Assert.Equal(scrambleId, operation.OperationId));
        foreach (var (plan, operation) in second.ScramblePlans.Zip(second.ScrambleOperations))
            controller.ApplyOperation(plan, operation.Parameters);

        Assert.Equal(second.ScrambledCells, OperationBoardSnapshot.FromGrid(grid).Cells);
        Assert.All(second.Solution.Operations, operation => Assert.Equal(solveId, operation.OperationId));
        VerifyOperations(second.ScrambledCells, second.Solution.Operations, rules, evaluator, Solved(4, 4));

        var recording = SessionRecording.FromJson(second.Solution.ToJson());
        controller.LoadPlayback(recording);
        while (controller.Step()) { }
        Assert.True(grid.IsSolved());
        while (controller.StepBack()) { }
        Assert.Equal(second.ScrambledCells, OperationBoardSnapshot.FromGrid(grid).Cells);
    }

    [Fact]
    public void VerifiedStartFollowsPlayerMovesAndRejectsUnprovenBoards()
    {
        var rules = PuzzleRuleSet.CreateTwoWide();
        var evaluator = new OperationRuleEvaluator(rules.Registry);
        var grid = new Grid(4, 4, 32);
        var controller = new SessionRecordingController(grid, rules);
        var first = ShuffleChallengeFactory.GenerateRuleAware(4, 4, 4, rules, evaluator, new Random(8));
        controller.LoadPlayback(first.Solution);
        controller.StopPlayback();
        var before = OperationBoardSnapshot.FromGrid(grid).Cells;
        var intent = new SlideOperationIntent(RecordedMoveAxis.Row, 0, 1);
        controller.MoveRow(0, 1);
        var after = OperationBoardSnapshot.FromGrid(grid).Cells;

        Assert.False(ShuffleChallengeFactory.HasVerifiedStart(after, first.Solution, rules));
        Assert.Throws<InvalidOperationException>(() =>
            ShuffleChallengeFactory.GenerateRuleAware(after, first.Solution, 4, rules, evaluator, new Random(9)));
        var verified = ShuffleChallengeFactory.TrackSolveMove(before, after, intent, first.Solution, rules);
        Assert.NotNull(verified);
        Assert.True(ShuffleChallengeFactory.HasVerifiedStart(after, verified, rules));
        var incompatible = PuzzleRuleSet.CreateSwapLogic(rules.Registry);
        Assert.False(ShuffleChallengeFactory.HasVerifiedStart(after, verified, incompatible));

        var next = ShuffleChallengeFactory.GenerateRuleAware(after, verified, 4, rules, evaluator, new Random(9));
        VerifyOperations(next.ScrambledCells, next.Solution.Operations, rules, evaluator, Solved(4, 4));

        grid.Reset();
        Assert.True(grid.IsSolved());
        Assert.True(ShuffleChallengeFactory.HasVerifiedStart(OperationBoardSnapshot.FromGrid(grid).Cells, null, rules));
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

    private static PuzzleRuleSet CreatePreset(string name, OperationRegistry registry) => name switch
    {
        "Classic" => PuzzleRuleSet.CreateClassic(registry),
        "ShuffleLogic" => PuzzleRuleSet.CreateShuffleLogic(registry),
        "SwapLogic" => PuzzleRuleSet.CreateSwapLogic(registry),
        "TwoWide" => PuzzleRuleSet.CreateTwoWide(registry),
        "TwoTall" => PuzzleRuleSet.CreateTwoTall(registry),
        "Hybrid" => PuzzleRuleSet.CreateHybrid(registry),
        "ZeroSlide" => PuzzleRuleSet.CreateZeroSlide(registry),
        "FreeStyle" => PuzzleRuleSet.CreateFreeStyle(registry),
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown test preset")
    };

    private static void VerifySwapLabyrinth(ChallengeCatalog catalog, string name, int expectedOperationCount)
    {
        Assert.True(catalog.TryGet(name, out var recording));
        Assert.NotNull(recording);
        using (var serialized = System.Text.Json.JsonDocument.Parse(recording.ToJson()))
            Assert.Equal(SessionRecording.Version, serialized.RootElement.GetProperty("version").GetInt32());
        Assert.Equal(4, recording.Width);
        Assert.Equal(4, recording.Height);
        Assert.Equal("SwapLogic", recording.RuleSet.Name);
        string[] expectedSwapIds = { PuzzleOperationIds.ColumnSwap, PuzzleOperationIds.RowSwap };
        Assert.Equal(expectedSwapIds, recording.RuleSet.ScrambleAllowed);
        Assert.Equal(expectedSwapIds, recording.RuleSet.SolveAllowed);
        Assert.Equal(expectedSwapIds, recording.RuleSet.UIEnabled);
        Assert.Equal(expectedOperationCount, recording.Operations.Count);
        Assert.All(recording.Operations, operation =>
        {
            Assert.Contains(operation.OperationId, expectedSwapIds);
            Assert.Equal(OperationMoveAccounting.LogicalOperation, operation.Accounting);
            Assert.Equal(1, operation.MoveCost);
            Assert.Equal(8, operation.PrimitiveMoves.Count);
        });
        Assert.NotNull(recording.GetVerifiedGoalMoveCost());

        var grid = new Grid(recording.Width, recording.Height, 32);
        var controller = new SessionRecordingController(grid);
        controller.LoadPlayback(recording);
        while (controller.Step()) { }
        Assert.Equal(Solved(recording.Width, recording.Height), OperationBoardSnapshot.FromGrid(grid).Cells);
    }

    private static string FindCatalogPath()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            string path = Path.Combine(directory.FullName, "SlideScramble", "challenges.json");
            if (File.Exists(path)) return path;
        }

        throw new FileNotFoundException("Could not locate the SlideScramble challenge catalog from the test directory.");
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