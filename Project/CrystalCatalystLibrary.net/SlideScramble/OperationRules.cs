namespace SlideScramble;

/// <summary>Independent, immutable authorization sets for puzzle generation, solving, and player input.</summary>
public sealed class PuzzleRuleSet
{
    private readonly HashSet<string> scrambleAllowed;
    private readonly HashSet<string> solveAllowed;
    private readonly HashSet<string> uiEnabled;
    private readonly OperationRegistry registry;

    public string Name { get; }
    internal OperationRegistry Registry => registry;
    public IReadOnlyCollection<string> ScrambleAllowed { get; }
    public IReadOnlyCollection<string> SolveAllowed { get; }
    public IReadOnlyCollection<string> UIEnabled { get; }

    public PuzzleRuleSet()
        : this("Classic", new[] { PuzzleOperationIds.Slide }, new[] { PuzzleOperationIds.Slide },
            new[] { PuzzleOperationIds.Slide }, OperationRegistry.CreateDefault())
    {
    }

    public PuzzleRuleSet(string name, IEnumerable<string> scrambleAllowed, IEnumerable<string> solveAllowed,
        IEnumerable<string> uiEnabled, OperationRegistry registry)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(registry);
        Name = name;
        this.registry = registry;
        this.scrambleAllowed = ValidateSet(scrambleAllowed, registry, nameof(scrambleAllowed));
        this.solveAllowed = ValidateSet(solveAllowed, registry, nameof(solveAllowed));
        this.uiEnabled = ValidateSet(uiEnabled, registry, nameof(uiEnabled));
        ScrambleAllowed = Array.AsReadOnly(this.scrambleAllowed.Order(StringComparer.Ordinal).ToArray());
        SolveAllowed = Array.AsReadOnly(this.solveAllowed.Order(StringComparer.Ordinal).ToArray());
        UIEnabled = Array.AsReadOnly(this.uiEnabled.Order(StringComparer.Ordinal).ToArray());
    }

    public static PuzzleRuleSet CreateClassic(OperationRegistry? registry = null)
    {
        var selectedRegistry = registry ?? OperationRegistry.CreateDefault();
        return new PuzzleRuleSet("Classic", new[] { PuzzleOperationIds.Slide }, new[] { PuzzleOperationIds.Slide },
            new[] { PuzzleOperationIds.Slide }, selectedRegistry);
    }

    public static PuzzleRuleSet CreateShuffleLogic(OperationRegistry? registry = null)
    {
        var selectedRegistry = registry ?? OperationRegistry.CreateDefault();
        if (!selectedRegistry.TryGetOperation(PuzzleOperationIds.Shuffle, out _))
            throw new ArgumentException("ShuffleLogic requires a registered Shuffle resolver.", nameof(registry));

        return new PuzzleRuleSet("ShuffleLogic", new[] { PuzzleOperationIds.Shuffle },
            new[] { PuzzleOperationIds.Shuffle }, new[] { PuzzleOperationIds.Shuffle }, selectedRegistry);
    }

    /// <summary>Returns an independent copy of the immutable rule-set snapshot.</summary>
    public PuzzleRuleSet Snapshot() => new(Name, ScrambleAllowed, SolveAllowed, UIEnabled, registry);

    internal bool AllowsScramble(string operationId) => scrambleAllowed.Contains(operationId);
    internal bool AllowsSolve(string operationId) => solveAllowed.Contains(operationId);
    internal bool AllowsUi(string operationId) => uiEnabled.Contains(operationId);

    private static HashSet<string> ValidateSet(IEnumerable<string> operationIds, OperationRegistry registry,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(operationIds, parameterName);
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (string operationId in operationIds)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(operationId, parameterName);
            if (!registry.IsKnownOperation(operationId))
                throw new ArgumentException($"Unknown operation ID '{operationId}'.", parameterName);
            result.Add(operationId);
        }

        return result;
    }
}

public enum OperationPlanPurpose
{
    Solve,
    Scramble
}

public enum OperationMoveAccounting
{
    None,
    LogicalOperation,
    PrimitiveMoves
}

/// <summary>A validated, immutable plan; applying it is a separate transactional responsibility.</summary>
public sealed class OperationPlan
{
    private readonly IReadOnlyList<RecordedMove> expansion;

    public string OperationId { get; }
    public IOperationIntent Intent { get; }
    public OperationBoardSnapshot SourceBoard { get; }
    public OperationPlanPurpose Purpose { get; }
    public IReadOnlyList<RecordedMove> Expansion => expansion;
    public OperationMoveAccounting Accounting { get; }
    public int? MoveCost { get; }

    internal OperationPlan(string operationId, IOperationIntent intent, OperationBoardSnapshot sourceBoard,
        OperationPlanPurpose purpose, IEnumerable<RecordedMove> expansion, OperationMoveAccounting accounting,
        int? moveCost)
    {
        OperationId = operationId;
        Intent = intent;
        SourceBoard = sourceBoard.Copy();
        Purpose = purpose;
        this.expansion = Array.AsReadOnly(expansion.ToArray());
        Accounting = accounting;
        MoveCost = moveCost;
    }
}

public sealed class OperationEvaluation
{
    public bool IsAccepted => Plan != null;
    public OperationPlan? Plan { get; }
    public string? RejectionReason { get; }

    private OperationEvaluation(OperationPlan? plan, string? rejectionReason)
    {
        Plan = plan;
        RejectionReason = rejectionReason;
    }

    internal static OperationEvaluation Accept(OperationPlan plan) => new(plan, null);
    internal static OperationEvaluation Reject(string reason) => new(null, reason);
}

/// <summary>Pure authorization and move-accounting logic. It never mutates a board or score.</summary>
public sealed class OperationRuleEvaluator
{
    private readonly OperationRegistry registry;

    public OperationRuleEvaluator(OperationRegistry registry)
    {
        this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public OperationEvaluation EvaluateUiRequest(OperationBoardSnapshot board, IOperationIntent? intent,
        PuzzleRuleSet rules)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(rules);
        string? error = ValidateIntent(intent);
        if (error != null) return OperationEvaluation.Reject(error);
        if (!rules.AllowsUi(intent!.OperationId))
            return OperationEvaluation.Reject($"Operation '{intent.OperationId}' is not enabled in the UI.");
        error = Resolve(board, intent, out var resolution);
        if (error != null) return OperationEvaluation.Reject(error);

        if (rules.AllowsSolve(intent.OperationId))
            return OperationEvaluation.Accept(new OperationPlan(intent.OperationId, intent, board,
                OperationPlanPurpose.Solve, resolution!.Expansion, OperationMoveAccounting.LogicalOperation, 1));

        if (!rules.AllowsSolve(PuzzleOperationIds.Slide))
            return OperationEvaluation.Reject(
                "The requested operation is not solve-allowed and Slide is not solve-allowed.");

        return OperationEvaluation.Accept(new OperationPlan(intent.OperationId, intent, board,
            OperationPlanPurpose.Solve, resolution!.Expansion, OperationMoveAccounting.PrimitiveMoves,
            resolution.Expansion.Count));
    }

    public OperationEvaluation EvaluateScrambleRequest(OperationBoardSnapshot board, IOperationIntent? intent,
        PuzzleRuleSet rules)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(rules);
        string? error = ValidateIntent(intent);
        if (error != null) return OperationEvaluation.Reject(error);
        if (!rules.AllowsScramble(intent!.OperationId))
            return OperationEvaluation.Reject($"Operation '{intent.OperationId}' is not allowed for scrambling.");
        error = Resolve(board, intent, out var resolution);
        if (error != null) return OperationEvaluation.Reject(error);

        return OperationEvaluation.Accept(new OperationPlan(intent.OperationId, intent, board,
            OperationPlanPurpose.Scramble, resolution!.Expansion, OperationMoveAccounting.None, null));
    }

    private string? ValidateIntent(IOperationIntent? intent)
    {
        if (intent == null) return "An operation intent is required.";
        if (string.IsNullOrWhiteSpace(intent.OperationId)) return "Operation ID is required.";
        if (!registry.IsKnownOperation(intent.OperationId))
            return $"Unknown operation ID '{intent.OperationId}'.";

        return null;
    }

    private string? Resolve(OperationBoardSnapshot board, IOperationIntent intent, out OperationResolution? resolution)
    {
        resolution = null;
        if (!registry.TryGetOperation(intent.OperationId, out var operation))
            return $"Operation '{intent.OperationId}' has no registered resolver.";
        if (!string.Equals(operation.Id, intent.OperationId, StringComparison.Ordinal))
            return $"Registered resolver identity changed for operation '{intent.OperationId}'.";

        resolution = operation.Resolve(board, intent);
        if (resolution == null) return "Operation resolver returned no result.";
        if (!string.Equals(resolution.OperationId, intent.OperationId, StringComparison.Ordinal))
            return "Operation resolver returned a mismatched operation ID.";
        if (!resolution.IsValid) return resolution.Error ?? "Operation parameters are invalid.";
        if (resolution.Expansion.Count == 0) return "Operation resolution produced no primitive moves.";

        foreach (var move in resolution.Expansion)
            if (!IsValidPrimitive(move, board))
                return "Operation expansion contains an invalid Slide move.";

        return null;
    }

    private static bool IsValidPrimitive(RecordedMove move, OperationBoardSnapshot board)
    {
        if (move == null || !Enum.IsDefined(move.Axis) || move.StartX < 0 || move.StartX >= board.Width ||
            move.StartY < 0 || move.StartY >= board.Height)
            return false;

        int lineCount = move.Axis == RecordedMoveAxis.Row ? board.Height : board.Width;
        int lineLength = move.Axis == RecordedMoveAxis.Row ? board.Width : board.Height;
        return move.Index >= 0 && move.Index < lineCount && move.Delta % lineLength != 0;
    }
}