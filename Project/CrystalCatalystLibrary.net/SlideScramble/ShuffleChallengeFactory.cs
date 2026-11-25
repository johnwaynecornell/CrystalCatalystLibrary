namespace SlideScramble;

public sealed class ShuffleChallenge
{
    private readonly int[,] scrambledCells;
    private readonly RecordedOperation[] scrambleOperations;
    private readonly RecordedMove[] scrambleMoves;

    public int[,] ScrambledCells => (int[,])scrambledCells.Clone();
    public IReadOnlyList<RecordedOperation> ScrambleOperations => Array.AsReadOnly(scrambleOperations);
    public IReadOnlyList<RecordedMove> ScrambleMoves => Array.AsReadOnly(scrambleMoves);
    public SessionRecording Solution { get; }

    internal ShuffleChallenge(int[,] scrambledCells, IEnumerable<RecordedOperation> scrambleOperations,
        IEnumerable<RecordedMove> scrambleMoves, SessionRecording solution)
    {
        this.scrambledCells = (int[,])scrambledCells.Clone();
        this.scrambleOperations = scrambleOperations.ToArray();
        this.scrambleMoves = scrambleMoves.ToArray();
        Solution = solution;
    }
}

/// <summary>Builds Shuffle-only challenges and their exact inverse operation recordings.</summary>
public static class ShuffleChallengeFactory
{
    private static readonly (PortableLensCell Start, PortableLensCell Target)[] Gestures =
    {
        (PortableLensCell.Circle, PortableLensCell.Star),
        (PortableLensCell.Star, PortableLensCell.Circle),
        (PortableLensCell.Circle, PortableLensCell.Triangle),
        (PortableLensCell.Triangle, PortableLensCell.Circle),
        (PortableLensCell.Star, PortableLensCell.Square),
        (PortableLensCell.Square, PortableLensCell.Star),
        (PortableLensCell.Triangle, PortableLensCell.Square),
        (PortableLensCell.Square, PortableLensCell.Triangle)
    };

    public static ShuffleChallenge Generate(int width, int height, int operationCount, PuzzleRuleSet rules,
        OperationRuleEvaluator evaluator, Random random)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(evaluator);
        ArgumentNullException.ThrowIfNull(random);
        if (width < 3 || height < 3) throw new ArgumentOutOfRangeException(nameof(width), "Shuffle boards must be at least 3×3.");
        if (operationCount < 1) throw new ArgumentOutOfRangeException(nameof(operationCount));

        var cells = Solved(width, height);
        var scramblePlans = new List<(ShuffleOperationIntent Intent, OperationPlan Plan)>(operationCount + 1);
        for (int i = 0; i < operationCount; i++)
            AddScrambleOperation(cells, rules, evaluator, random, scramblePlans);
        if (IsSolved(cells)) AddScrambleOperation(cells, rules, evaluator, random, scramblePlans);

        int[,] scrambled = (int[,])cells.Clone();
        var scrambleOperations = scramblePlans.Select(entry => ToRecordedOperation(entry.Intent, entry.Plan)).ToArray();
        var scrambleMoves = scrambleOperations.SelectMany(operation => operation.PrimitiveMoves).ToArray();
        var solutionOperations = new List<RecordedOperation>(scramblePlans.Count);
        for (int i = scramblePlans.Count - 1; i >= 0; i--)
        {
            var original = scramblePlans[i].Intent;
            var inverse = original with { StartCell = original.TargetCell, TargetCell = original.StartCell };
            var evaluation = evaluator.EvaluateUiRequest(new OperationBoardSnapshot(cells), inverse, rules);
            if (!evaluation.IsAccepted)
                throw new InvalidOperationException(evaluation.RejectionReason ?? "Shuffle inverse was rejected.");

            OperationPlan plan = evaluation.Plan!;
            solutionOperations.Add(ToRecordedOperation(inverse, plan));
            Apply(cells, plan.Expansion);
        }

        if (!IsSolved(cells))
            throw new InvalidOperationException("Generated Shuffle inverses did not restore the solved board.");

        var solution = new SessionRecording(scrambled, solutionOperations, rules, rules.Registry);
        return new ShuffleChallenge(scrambled, scrambleOperations, scrambleMoves, solution);
    }

    private static void AddScrambleOperation(int[,] cells, PuzzleRuleSet rules, OperationRuleEvaluator evaluator,
        Random random, ICollection<(ShuffleOperationIntent Intent, OperationPlan Plan)> operations)
    {
        var (start, target) = Gestures[random.Next(Gestures.Length)];
        var intent = new ShuffleOperationIntent(random.Next(cells.GetLength(1)), random.Next(cells.GetLength(0)),
            start, target);
        var evaluation = evaluator.EvaluateScrambleRequest(new OperationBoardSnapshot(cells), intent, rules);
        if (!evaluation.IsAccepted)
            throw new InvalidOperationException(evaluation.RejectionReason ?? "Shuffle scramble was rejected.");

        OperationPlan plan = evaluation.Plan!;
        operations.Add((intent, plan));
        Apply(cells, plan.Expansion);
    }

    private static RecordedOperation ToRecordedOperation(ShuffleOperationIntent intent, OperationPlan plan) =>
        new(plan.OperationId, intent.ToRecordingParameters(), plan.Expansion, plan.Purpose, plan.Accounting,
            plan.MoveCost);

    private static int[,] Solved(int width, int height)
    {
        var cells = new int[height, width];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
            cells[y, x] = y * width + x;
        return cells;
    }

    private static bool IsSolved(int[,] cells)
    {
        for (int y = 0; y < cells.GetLength(0); y++)
        for (int x = 0; x < cells.GetLength(1); x++)
            if (cells[y, x] != y * cells.GetLength(1) + x)
                return false;
        return true;
    }

    private static void Apply(int[,] cells, IEnumerable<RecordedMove> moves)
    {
        foreach (var move in moves)
        {
            int length = move.Axis == RecordedMoveAxis.Row ? cells.GetLength(1) : cells.GetLength(0);
            int index = move.Index;
            var line = new int[length];
            for (int i = 0; i < length; i++)
                line[i] = move.Axis == RecordedMoveAxis.Row ? cells[index, i] : cells[i, index];
            int shift = (move.Delta % length + length) % length;
            for (int i = 0; i < length; i++)
            {
                int value = line[(i + shift) % length];
                if (move.Axis == RecordedMoveAxis.Row) cells[index, i] = value;
                else cells[i, index] = value;
            }
        }
    }
}