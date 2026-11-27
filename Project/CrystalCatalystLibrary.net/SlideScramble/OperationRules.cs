namespace SlideScramble;

/// <summary>Slide permission names represent two independent axes of the same operation.</summary>
internal static class SlidePermissions
{
    internal static bool IsName(string id) =>
        id is PuzzleOperationIds.Slide or PuzzleOperationIds.RowSlide or PuzzleOperationIds.ColumnSlide;

    internal static bool Allows(IEnumerable<string> ids, RecordedMoveAxis axis) => axis switch
    {
        RecordedMoveAxis.Row => ids.Contains(PuzzleOperationIds.Slide) || ids.Contains(PuzzleOperationIds.RowSlide),
        RecordedMoveAxis.Column => ids.Contains(PuzzleOperationIds.Slide) || ids.Contains(PuzzleOperationIds.ColumnSlide),
        _ => false
    };

    internal static bool Includes(IEnumerable<string> ids, string id) => id switch
    {
        PuzzleOperationIds.RowSlide => Allows(ids, RecordedMoveAxis.Row),
        PuzzleOperationIds.ColumnSlide => Allows(ids, RecordedMoveAxis.Column),
        _ => ids.Contains(id)
    };

    internal static bool Set(HashSet<string> ids, string id, bool enabled)
    {
        if (!IsName(id)) return enabled ? ids.Add(id) : ids.Remove(id);

        bool row = Allows(ids, RecordedMoveAxis.Row);
        bool column = Allows(ids, RecordedMoveAxis.Column);
        bool newRow = id == PuzzleOperationIds.ColumnSlide ? row : enabled;
        bool newColumn = id == PuzzleOperationIds.RowSlide ? column : enabled;
        if (newRow == row && newColumn == column) return false;

        ids.RemoveWhere(IsName);
        if (Name(newRow, newColumn) is { } name) ids.Add(name);
        return true;
    }

    internal static string? Name(bool row, bool column) => (row, column) switch
    {
        (true, true) => PuzzleOperationIds.Slide,
        (true, false) => PuzzleOperationIds.RowSlide,
        (false, true) => PuzzleOperationIds.ColumnSlide,
        _ => null
    };

    internal static IEnumerable<string> MenuEntries(OperationRegistry registry) =>
        registry.RegisteredOperationIds.SelectMany(id => id == PuzzleOperationIds.Slide
            ? new[] { PuzzleOperationIds.RowSlide, PuzzleOperationIds.ColumnSlide }
            : new[] { id });
}

/// <summary>Independent, immutable authorization sets for puzzle generation, solving, and player input.</summary>
public sealed class PuzzleRuleSet
{
    private static readonly string[] PortableLensOperationIds =
    {
        PuzzleOperationIds.Shuffle,
        PuzzleOperationIds.ColumnSwap,
        PuzzleOperationIds.RowSwap
    };

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
        return CreatePreset("Classic", new[] { PuzzleOperationIds.Slide }, selectedRegistry);
    }

    public static PuzzleRuleSet CreateShuffleLogic(OperationRegistry? registry = null)
    {
        var selectedRegistry = registry ?? OperationRegistry.CreateDefault();
        return CreatePreset("ShuffleLogic", new[] { PuzzleOperationIds.Shuffle }, selectedRegistry);
    }

    public static PuzzleRuleSet CreateSwapLogic(OperationRegistry? registry = null)
    {
        var selectedRegistry = registry ?? OperationRegistry.CreateDefault();
        return CreatePreset("SwapLogic", new[] { PuzzleOperationIds.ColumnSwap, PuzzleOperationIds.RowSwap },
            selectedRegistry);
    }

    public static PuzzleRuleSet CreateTwoWide(OperationRegistry? registry = null)
    {
        var selectedRegistry = registry ?? OperationRegistry.CreateDefault();
        return CreatePreset("TwoWide", new[] { PuzzleOperationIds.Slide, PuzzleOperationIds.ColumnSwap },
            selectedRegistry);
    }

    public static PuzzleRuleSet CreateTwoTall(OperationRegistry? registry = null)
    {
        var selectedRegistry = registry ?? OperationRegistry.CreateDefault();
        return CreatePreset("TwoTall", new[] { PuzzleOperationIds.Slide, PuzzleOperationIds.RowSwap },
            selectedRegistry);
    }

    public static PuzzleRuleSet CreateTwoWideSlide(OperationRegistry? registry = null) =>
        CreatePreset("TwoWideSlide", new[] { PuzzleOperationIds.RowSlide, PuzzleOperationIds.ColumnSwap },
            registry ?? OperationRegistry.CreateDefault());

    public static PuzzleRuleSet CreateTwoTallSlide(OperationRegistry? registry = null) =>
        CreatePreset("TwoTallSlide", new[] { PuzzleOperationIds.ColumnSlide, PuzzleOperationIds.RowSwap },
            registry ?? OperationRegistry.CreateDefault());

    public static PuzzleRuleSet CreateTwoWideColumnSlide(OperationRegistry? registry = null) =>
        CreatePreset("TwoWideColumnSlide", new[] { PuzzleOperationIds.ColumnSlide, PuzzleOperationIds.ColumnSwap },
            registry ?? OperationRegistry.CreateDefault());

    public static PuzzleRuleSet CreateTwoTallRowSlide(OperationRegistry? registry = null) =>
        CreatePreset("TwoTallRowSlide", new[] { PuzzleOperationIds.RowSlide, PuzzleOperationIds.RowSwap },
            registry ?? OperationRegistry.CreateDefault());

    public static PuzzleRuleSet CreateHybrid(OperationRegistry? registry = null)
    {
        var selectedRegistry = registry ?? OperationRegistry.CreateDefault();
        return CreatePreset("Hybrid", new[] { PuzzleOperationIds.Slide, PuzzleOperationIds.Shuffle },
            selectedRegistry);
    }

    public static PuzzleRuleSet CreateZeroSlide(OperationRegistry? registry = null)
    {
        var selectedRegistry = registry ?? OperationRegistry.CreateDefault();
        return CreatePreset("ZeroSlide", PortableLensOperationIds, selectedRegistry);
    }

    public static PuzzleRuleSet CreateFreeStyle(OperationRegistry? registry = null)
    {
        var selectedRegistry = registry ?? OperationRegistry.CreateDefault();
        return CreatePreset("FreeStyle", new[]
        {
            PuzzleOperationIds.Slide,
            PuzzleOperationIds.Shuffle,
            PuzzleOperationIds.ColumnSwap,
            PuzzleOperationIds.RowSwap
        }, selectedRegistry);
    }

    /// <summary>Returns an independent copy of the immutable rule-set snapshot.</summary>
    public PuzzleRuleSet Snapshot() => new(Name, ScrambleAllowed, SolveAllowed, UIEnabled, registry);

    /// <summary>Copies the mathematical rule set with a different saved initial UI configuration.</summary>
    public PuzzleRuleSet WithUIEnabled(IEnumerable<string> operationIds) =>
        new(Name, ScrambleAllowed, SolveAllowed, operationIds, registry);

    internal bool HasSameMathematicalPermissions(PuzzleRuleSet other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return scrambleAllowed.SetEquals(other.scrambleAllowed) && solveAllowed.SetEquals(other.solveAllowed);
    }

    internal bool AllowsScramble(string operationId) => scrambleAllowed.Contains(operationId);
    internal bool AllowsSolve(string operationId) => solveAllowed.Contains(operationId);
    internal bool AllowsSolveSlide(RecordedMoveAxis axis) => SlidePermissions.Allows(solveAllowed, axis);
    internal bool AllowsScrambleSlide(RecordedMoveAxis axis) => SlidePermissions.Allows(scrambleAllowed, axis);

    internal string? GetSlideInputRejection(RecordedMoveAxis axis, IReadOnlyCollection<string> inputs)
    {
        string label = axis == RecordedMoveAxis.Row ? "Row slides" : "Column slides";
        if (!AllowsSolveSlide(axis)) return $"{label} aren’t allowed by {Name}.";
        return SlidePermissions.Allows(inputs, axis) ? null : $"{label} are disabled in Live Input.";
    }

    public bool HasUnavailablePortableLensOperations(int width, int height) =>
        (width < 3 || height < 3) &&
        (scrambleAllowed.Concat(solveAllowed).Concat(uiEnabled).Intersect(PortableLensOperationIds).Any());

    public bool IsBoardCompatible(int width, int height)
    {
        if (width <= 0 || height <= 0) return false;
        if (!HasUnavailablePortableLensOperations(width, height)) return true;
        return AvailableSlideName(width, height) != null;
    }

    public string? GetBoardCompatibilityMessage(int width, int height)
    {
        if (width <= 0 || height <= 0) return "Board dimensions must be positive.";
        if (!HasUnavailablePortableLensOperations(width, height)) return null;
        return AvailableSlideName(width, height) is { } slide
            ? $"Portable Lens operations require a 3 × 3 board; {slide} remains available."
            : $"{Name} requires a 3 × 3 board for its configured Portable Lens operations.";
    }

    private string? AvailableSlideName(int width, int height) => SlidePermissions.Name(
        width > 1 && AllowsScrambleSlide(RecordedMoveAxis.Row) && AllowsSolveSlide(RecordedMoveAxis.Row),
        height > 1 && AllowsScrambleSlide(RecordedMoveAxis.Column) && AllowsSolveSlide(RecordedMoveAxis.Column));

    private static PuzzleRuleSet CreatePreset(string name, string[] operations, OperationRegistry registry)
    {
        foreach (string operationId in operations)
            if (!registry.TryGetOperation(SlidePermissions.IsName(operationId) ? PuzzleOperationIds.Slide : operationId, out _))
                throw new ArgumentException($"{name} requires a registered {operationId} resolver.", nameof(registry));

        return new PuzzleRuleSet(name, operations, operations, operations, registry);
    }

    private static HashSet<string> ValidateSet(IEnumerable<string> operationIds, OperationRegistry registry,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(operationIds, parameterName);
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (string operationId in operationIds)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(operationId, parameterName);
            if (!SlidePermissions.IsName(operationId) && !registry.IsKnownOperation(operationId))
                throw new ArgumentException($"Unknown operation ID '{operationId}'.", parameterName);
            SlidePermissions.Set(result, operationId, true);
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
        PuzzleRuleSet rules, IReadOnlyCollection<string>? runtimeUiEnabled = null)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(rules);
        string? error = ValidateIntent(intent);
        if (error != null) return OperationEvaluation.Reject(error);
        var inputs = runtimeUiEnabled ?? rules.UIEnabled;
        if (intent is SlideOperationIntent slide)
        {
            if (!Enum.IsDefined(slide.Axis)) return OperationEvaluation.Reject("Slide axis is invalid.");
            error = rules.GetSlideInputRejection(slide.Axis, inputs);
            if (error != null) return OperationEvaluation.Reject(error);
        }
        else if (!inputs.Contains(intent!.OperationId, StringComparer.Ordinal))
            return OperationEvaluation.Reject($"Operation '{intent.OperationId}' is not enabled in the UI.");
        error = Resolve(board, intent, out var resolution);
        if (error != null) return OperationEvaluation.Reject(error);

        if (intent is SlideOperationIntent || rules.AllowsSolve(intent!.OperationId))
            return OperationEvaluation.Accept(new OperationPlan(intent!.OperationId, intent, board,
                OperationPlanPurpose.Solve, resolution!.Expansion, OperationMoveAccounting.LogicalOperation, 1));

        if (!resolution!.Expansion.All(move => rules.AllowsSolveSlide(move.Axis)))
            return OperationEvaluation.Reject(
                "The requested operation is not solve-allowed and its expansion uses a forbidden Slide axis.");

        return OperationEvaluation.Accept(new OperationPlan(intent!.OperationId, intent, board,
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
        if (!(intent is SlideOperationIntent slide
                ? rules.AllowsScrambleSlide(slide.Axis)
                : rules.AllowsScramble(intent!.OperationId)))
            return OperationEvaluation.Reject($"Operation '{intent!.OperationId}' is not allowed for scrambling.");
        error = Resolve(board, intent, out var resolution);
        if (error != null) return OperationEvaluation.Reject(error);

        return OperationEvaluation.Accept(new OperationPlan(intent!.OperationId, intent, board,
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