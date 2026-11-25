using Xunit;

namespace SlideScramble.Tests;

public class ShuffleChallengeFactoryTests
{
    [Theory]
    [InlineData(3, 3, 1, 712)]
    [InlineData(5, 3, 7, 29)]
    [InlineData(7, 6, 14, 491)]
    public void GeneratedChallengesAreShuffleOnlyDeterministicAndHaveRecordedLegalInverses(
        int width, int height, int operationCount, int seed)
    {
        var registry = OperationRegistry.CreateDefault();
        var rules = PuzzleRuleSet.CreateShuffleLogic(registry);
        var evaluator = new OperationRuleEvaluator(registry);
        var challenge = ShuffleChallengeFactory.Generate(width, height, operationCount, rules, evaluator,
            new Random(seed));
        var repeated = ShuffleChallengeFactory.Generate(width, height, operationCount, rules, evaluator,
            new Random(seed));

        Assert.Equal(challenge.ScrambledCells, repeated.ScrambledCells);
        Assert.Equal(challenge.ScrambleMoves, repeated.ScrambleMoves);
        Assert.NotEqual(Solved(width, height), challenge.ScrambledCells);
        Assert.Equal(operationCount, challenge.ScrambleOperations.Count);
        Assert.All(challenge.ScrambleOperations, operation =>
        {
            Assert.Equal(PuzzleOperationIds.Shuffle, operation.OperationId);
            Assert.Equal(OperationPlanPurpose.Scramble, operation.Purpose);
            Assert.Equal(OperationMoveAccounting.None, operation.Accounting);
            Assert.Null(operation.MoveCost);
            Assert.Equal(4, operation.PrimitiveMoves.Count);
        });

        var solution = challenge.Solution;
        Assert.Equal("ShuffleLogic", solution.RuleSet.Name);
        Assert.Equal(new[] { PuzzleOperationIds.Shuffle }, solution.RuleSet.ScrambleAllowed);
        Assert.Equal(new[] { PuzzleOperationIds.Shuffle }, solution.RuleSet.SolveAllowed);
        Assert.Equal(new[] { PuzzleOperationIds.Shuffle }, solution.RuleSet.UIEnabled);
        Assert.Equal(challenge.ScrambledCells, solution.InitialCells);
        Assert.Equal(challenge.ScrambleOperations.Count, solution.Operations.Count);
        Assert.All(solution.Operations, operation =>
        {
            Assert.Equal(PuzzleOperationIds.Shuffle, operation.OperationId);
            Assert.Equal(OperationPlanPurpose.Solve, operation.Purpose);
            Assert.Equal(OperationMoveAccounting.LogicalOperation, operation.Accounting);
            Assert.Equal(1, operation.MoveCost);
            Assert.Equal(4, operation.PrimitiveMoves.Count);
        });
        Assert.Equal(solution.Moves.Count, solution.GetVerifiedGoal());
        Assert.Equal(solution.Operations.Count, solution.GetVerifiedGoalMoveCost());

        var roundTrip = SessionRecording.FromJson(solution.ToJson());
        Assert.Equal(solution.RuleSet.Name, roundTrip.RuleSet.Name);
        Assert.Equal(solution.ToJson(), roundTrip.ToJson());
        var grid = new Grid(width, height, 32);
        var controller = new SessionRecordingController(grid);
        controller.LoadPlayback(roundTrip);
        while (controller.Step()) { }
        Assert.Equal(Solved(width, height), Capture(grid));
        Assert.True(controller.IsOperationBoundary(controller.Position));
    }

    [Fact]
    public void ShuffleChallengeCannotBeGeneratedUnderClassicRules()
    {
        var registry = OperationRegistry.CreateDefault();
        Assert.Throws<InvalidOperationException>(() => ShuffleChallengeFactory.Generate(3, 3, 1,
            PuzzleRuleSet.CreateClassic(), new OperationRuleEvaluator(registry), new Random(1)));
    }

    private static int[,] Solved(int width, int height)
    {
        var cells = new int[height, width];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
            cells[y, x] = y * width + x;
        return cells;
    }

    private static int[,] Capture(Grid grid)
    {
        var cells = new int[grid.Height, grid.Width];
        for (int y = 0; y < grid.Height; y++)
        for (int x = 0; x < grid.Width; x++)
            cells[y, x] = grid.Tiles[y, x].OriginY * grid.Width + grid.Tiles[y, x].OriginX;
        return cells;
    }
}