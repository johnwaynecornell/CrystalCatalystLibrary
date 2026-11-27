namespace SlideScramble;

public sealed class ShuffleChallenge
{
    private readonly int[,] scrambledCells;
    private readonly RecordedOperation[] scrambleOperations;
    private readonly OperationPlan[] scramblePlans;
    private readonly RecordedMove[] scrambleMoves;

    public int[,] ScrambledCells => (int[,])scrambledCells.Clone();
    public IReadOnlyList<RecordedOperation> ScrambleOperations => Array.AsReadOnly(scrambleOperations);
    public IReadOnlyList<OperationPlan> ScramblePlans => Array.AsReadOnly(scramblePlans);
    public IReadOnlyList<RecordedMove> ScrambleMoves => Array.AsReadOnly(scrambleMoves);
    public SessionRecording Solution { get; }

    internal ShuffleChallenge(int[,] scrambledCells, IEnumerable<RecordedOperation> scrambleOperations,
        IEnumerable<OperationPlan> scramblePlans, IEnumerable<RecordedMove> scrambleMoves,
        SessionRecording solution)
    {
        this.scrambledCells = (int[,])scrambledCells.Clone();
        this.scrambleOperations = scrambleOperations.ToArray();
        this.scramblePlans = scramblePlans.ToArray();
        this.scrambleMoves = scrambleMoves.ToArray();
        Solution = solution;
    }
}

/// <summary>Builds challenges and their verified inverse operation recordings.</summary>
public static class ShuffleChallengeFactory
{
    private readonly record struct CandidateGroupKey(
        string OperationId,
        PortableLensCell? StartCell,
        PortableLensCell? TargetCell);

    private sealed record CandidateGroup(CandidateGroupKey Key, IOperationIntent[] Intents);

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
        return new ShuffleChallenge(scrambled, scrambleOperations, scramblePlans.Select(entry => entry.Plan),
            scrambleMoves, solution);
    }

    /// <summary>Builds a challenge from supported, authorized operations with a verified player-accessible inverse.</summary>
    public static ShuffleChallenge GenerateRuleAware(int width, int height, int operationCount, PuzzleRuleSet rules,
        OperationRuleEvaluator evaluator, Random random)
    {
        if (width < 2) throw new ArgumentOutOfRangeException(nameof(width));
        if (height < 2) throw new ArgumentOutOfRangeException(nameof(height));
        return GenerateRuleAware(Solved(width, height), null, operationCount, rules, evaluator, random);
    }

    // previousSolution comes from this generator or from a verified extension after a player move.
    internal static ShuffleChallenge GenerateRuleAware(int[,] start, SessionRecording? previousSolution,
        int operationCount, PuzzleRuleSet rules, OperationRuleEvaluator evaluator, Random random)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(evaluator);
        ArgumentNullException.ThrowIfNull(random);
        if (!HasVerifiedStart(start, previousSolution, rules))
            throw new InvalidOperationException("The current board has no verified solution under these rules.");

        int width = start.GetLength(1);
        int height = start.GetLength(0);
        if (operationCount < 1) throw new ArgumentOutOfRangeException(nameof(operationCount));

        var candidateGroups = CandidateIntents(width, height, rules)
            .GroupBy(GetCandidateGroupKey)
            .Select(group => new CandidateGroup(group.Key, group.ToArray()))
            .ToArray();
        var rulesEvaluator = new OperationRuleEvaluator(rules.Registry);
        var cells = (int[,])start.Clone();
        var scramble = new List<RecordedOperation>(operationCount);
        var scramblePlans = new List<OperationPlan>(operationCount);
        var inverses = new List<IReadOnlyList<RecordedOperation>>(operationCount);
        var usedGroups = new HashSet<CandidateGroupKey>();
        for (int step = 0; step < operationCount; step++)
        {
            CandidateGroup[] unvisitedGroups =
                candidateGroups.Where(group => !usedGroups.Contains(group.Key)).ToArray();
            if (unvisitedGroups.Length == 0)
            {
                usedGroups.Clear();
                unvisitedGroups = candidateGroups;
            }

            var groupsToTry = Shuffle(unvisitedGroups, random)
                .Concat(Shuffle(candidateGroups.Where(group => usedGroups.Contains(group.Key)), random));

            bool added = false;
            foreach (CandidateGroup group in groupsToTry)
            {
                foreach (IOperationIntent intent in Shuffle(group.Intents, random))
                {
                    var evaluation =
                        evaluator.EvaluateScrambleRequest(new OperationBoardSnapshot(cells), intent, rules);
                    if (!evaluation.IsAccepted) continue;
                    var registryEvaluation =
                        rulesEvaluator.EvaluateScrambleRequest(new OperationBoardSnapshot(cells), intent, rules);
                    if (!Equivalent(evaluation, registryEvaluation)) continue;
                    var next = (int[,])cells.Clone();
                    Apply(next, evaluation.Plan!.Expansion);
                    if (SameCells(cells, next) || (step == operationCount - 1 && IsSolved(next))) continue;
                    var inverse = TryInverse(cells, next, intent, evaluation.Plan, rules, evaluator, rulesEvaluator);
                    if (inverse == null) continue;
                    scramble.Add(ToRecordedOperation(intent, evaluation.Plan));
                    scramblePlans.Add(evaluation.Plan!);
                    inverses.Add(inverse);
                    cells = next;
                    usedGroups.Add(group.Key);
                    added = true;
                    break;
                }

                if (added) break;
            }

            if (!added)
                throw new InvalidOperationException("No safe compatible scramble and player-accessible inverse is available for these rules and board dimensions.");
        }

        var solutionOperations = inverses.AsEnumerable().Reverse().SelectMany(operations => operations)
            .Concat(IsSolved(start) ? Array.Empty<RecordedOperation>() : previousSolution!.Operations).ToArray();
        var verified = (int[,])cells.Clone();
        Apply(verified, solutionOperations.SelectMany(operation => operation.PrimitiveMoves));
        if (!IsSolved(verified)) throw new InvalidOperationException("Generated inverses did not restore the solved board.");
        var solution = new SessionRecording(cells, solutionOperations, rules, rules.Registry);
        return new ShuffleChallenge(cells, scramble, scramblePlans,
            scramble.SelectMany(operation => operation.PrimitiveMoves), solution);
    }

    internal static bool HasVerifiedStart(int[,] start, SessionRecording? solution, PuzzleRuleSet rules) =>
        IsSolved(start) || (solution != null &&
                            ReferenceEquals(solution.RuleSet.Registry, rules.Registry) &&
                            solution.RuleSet.HasSameMathematicalPermissions(rules) &&
                            solution.Operations.All(operation => rules.UIEnabled.Contains(operation.OperationId)) &&
                            RecordedOperationPlaybackStep.BoardsEqual(start, solution.InitialCells));

    internal static SessionRecording? TrackSolveMove(int[,] before, int[,] after, IOperationIntent intent,
        SessionRecording? previousSolution, PuzzleRuleSet rules)
    {
        if (IsSolved(after) || !HasVerifiedStart(before, previousSolution, rules)) return null;

        var evaluator = new OperationRuleEvaluator(rules.Registry);
        var plan = evaluator.EvaluateUiRequest(new OperationBoardSnapshot(before), intent, rules).Plan;
        if (plan == null) return null;

        var inverse = TryInverse(before, after, intent, plan, rules, evaluator, evaluator);
        if (inverse == null) return null;

        return new SessionRecording(after,
            inverse.Concat(IsSolved(before) ? Array.Empty<RecordedOperation>() : previousSolution!.Operations),
            rules, rules.Registry);
    }

    private static IEnumerable<IOperationIntent> CandidateIntents(int width, int height, PuzzleRuleSet rules)
    {
        if (rules.ScrambleAllowed.Contains(PuzzleOperationIds.Slide) &&
            rules.Registry.TryGetOperation(PuzzleOperationIds.Slide, out _))
        {
            foreach (var axis in new[] { RecordedMoveAxis.Row, RecordedMoveAxis.Column })
            {
                int count = axis == RecordedMoveAxis.Row ? height : width;
                int length = axis == RecordedMoveAxis.Row ? width : height;
                for (int index = 0; index < count; index++)
                for (int delta = 1; delta < length; delta++)
                {
                    yield return new SlideOperationIntent(axis, index, delta);
                    yield return new SlideOperationIntent(axis, index, -delta);
                }
            }
        }

        if (width < 3 || height < 3) yield break;
        foreach (string id in new[] { PuzzleOperationIds.Shuffle, PuzzleOperationIds.RowSwap, PuzzleOperationIds.ColumnSwap })
        {
            if (!rules.ScrambleAllowed.Contains(id) || !rules.Registry.TryGetOperation(id, out _)) continue;
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            foreach (var (start, target) in Gestures)
            {
                if (id == PuzzleOperationIds.Shuffle)
                    yield return new ShuffleOperationIntent(x, y, start, target);
                else if (LensGestureOperationMapper.TryGetSwapVariant(start, target, out var family, out string variant) &&
                         (id == PuzzleOperationIds.RowSwap) == (family == LensOperationFamily.RowSwap))
                    yield return new PortableLensSwapIntent(id, x, y, start, target, variant);
            }
        }
    }

    private static CandidateGroupKey GetCandidateGroupKey(IOperationIntent intent) => intent switch
    {
        ShuffleOperationIntent shuffle => new CandidateGroupKey(PuzzleOperationIds.Shuffle, shuffle.StartCell,
            shuffle.TargetCell),
        SlideOperationIntent => new CandidateGroupKey(PuzzleOperationIds.Slide, null, null),
        PortableLensSwapIntent swap => new CandidateGroupKey(swap.OperationId, null, null),
        _ => throw new InvalidOperationException("Unsupported challenge operation.")
    };

    private static T[] Shuffle<T>(IEnumerable<T> values, Random random)
    {
        T[] result = values.ToArray();
        for (int i = result.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (result[i], result[j]) = (result[j], result[i]);
        }

        return result;
    }

    private static IReadOnlyList<RecordedOperation>? TryInverse(int[,] before, int[,] after, IOperationIntent intent,
        OperationPlan scramble, PuzzleRuleSet rules, OperationRuleEvaluator evaluator, OperationRuleEvaluator rulesEvaluator)
    {
        IOperationIntent inverse = intent switch
        {
            SlideOperationIntent slide => slide with { Delta = -slide.Delta },
            ShuffleOperationIntent shuffle => shuffle with { StartCell = shuffle.TargetCell, TargetCell = shuffle.StartCell },
            PortableLensSwapIntent swap => swap,
            _ => throw new InvalidOperationException("Unsupported challenge operation.")
        };
        var evaluation = evaluator.EvaluateUiRequest(new OperationBoardSnapshot(after), inverse, rules);
        var registryEvaluation = rulesEvaluator.EvaluateUiRequest(new OperationBoardSnapshot(after), inverse, rules);
        if (Equivalent(evaluation, registryEvaluation))
        {
            var result = (int[,])after.Clone();
            Apply(result, evaluation.Plan!.Expansion);
            if (SameCells(before, result)) return new[] { ToRecordedOperation(inverse, evaluation.Plan) };
        }

        var current = (int[,])after.Clone();
        var operations = new List<RecordedOperation>();
        foreach (var move in scramble.Expansion.Reverse())
        {
            int length = move.Axis == RecordedMoveAxis.Row ? current.GetLength(1) : current.GetLength(0);
            var slide = new SlideOperationIntent(move.Axis, move.Index, -(move.Delta % length));
            var slideEvaluation = evaluator.EvaluateUiRequest(new OperationBoardSnapshot(current), slide, rules);
            var registrySlideEvaluation = rulesEvaluator.EvaluateUiRequest(new OperationBoardSnapshot(current), slide, rules);
            if (!Equivalent(slideEvaluation, registrySlideEvaluation)) return null;
            operations.Add(ToRecordedOperation(slide, slideEvaluation.Plan!));
            Apply(current, slideEvaluation.Plan!.Expansion);
        }
        return SameCells(before, current) ? operations : null;
    }

    private static bool Equivalent(OperationEvaluation left, OperationEvaluation right) =>
        left.IsAccepted && right.IsAccepted && left.Plan!.Purpose == right.Plan!.Purpose &&
        left.Plan.Accounting == right.Plan.Accounting && left.Plan.MoveCost == right.Plan.MoveCost &&
        left.Plan.Expansion.SequenceEqual(right.Plan.Expansion);

    private static bool SameCells(int[,] left, int[,] right)
    {
        for (int y = 0; y < left.GetLength(0); y++)
        for (int x = 0; x < left.GetLength(1); x++)
            if (left[y, x] != right[y, x]) return false;
        return true;
    }

    private static RecordedOperation ToRecordedOperation(IOperationIntent intent, OperationPlan plan) => intent switch
    {
        SlideOperationIntent => RecordedOperation.CreateSlide(plan.Expansion.Single(), plan.Purpose),
        ShuffleOperationIntent shuffle => ToRecordedOperation(shuffle, plan),
        PortableLensSwapIntent swap => new RecordedOperation(plan.OperationId, swap.ToRecordingParameters(),
            plan.Expansion, plan.Purpose, plan.Accounting, plan.MoveCost),
        _ => throw new InvalidOperationException("Unsupported challenge operation.")
    };

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