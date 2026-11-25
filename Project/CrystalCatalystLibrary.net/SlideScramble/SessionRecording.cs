using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SlideScramble;

public enum RecordedMoveAxis
{
    Row,
    Column
}

public sealed record RecordedMove(RecordedMoveAxis Axis, int StartX, int StartY, int Delta)
{
    public RecordedMove(RecordedMoveAxis axis, int index, int delta)
        : this(axis, axis == RecordedMoveAxis.Column ? index : 0, axis == RecordedMoveAxis.Row ? index : 0, delta)
    {
    }

    public int Index => Axis == RecordedMoveAxis.Row ? StartY : StartX;
}

/// <summary>One logical operation and the authoritative primitive moves that execute it.</summary>
public sealed class RecordedOperation
{
    private static readonly JsonSerializerOptions ParameterOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter<RecordedMoveAxis>(null, false) }
    };

    private readonly ReadOnlyCollection<RecordedMove> primitiveMoves;

    public string OperationId { get; }
    public JsonElement Parameters { get; }
    public IReadOnlyList<RecordedMove> PrimitiveMoves => primitiveMoves;
    public OperationPlanPurpose Purpose { get; }
    public OperationMoveAccounting Accounting { get; }
    public int? MoveCost { get; }
    public bool IsInverse { get; }

    public RecordedOperation(string operationId, JsonElement parameters, IEnumerable<RecordedMove> primitiveMoves,
        OperationPlanPurpose purpose = OperationPlanPurpose.Solve,
        OperationMoveAccounting accounting = OperationMoveAccounting.LogicalOperation, int? moveCost = 1,
        bool isInverse = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentNullException.ThrowIfNull(primitiveMoves);
        if (parameters.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Operation parameters must be a JSON object.", nameof(parameters));
        if (!Enum.IsDefined(purpose)) throw new ArgumentOutOfRangeException(nameof(purpose));
        if (!Enum.IsDefined(accounting)) throw new ArgumentOutOfRangeException(nameof(accounting));

        OperationId = operationId;
        Parameters = parameters.Clone();
        var moves = primitiveMoves.ToArray();
        if (moves.Length == 0 || moves.Any(move => move == null))
            throw new ArgumentException("An operation must contain primitive moves.", nameof(primitiveMoves));
        this.primitiveMoves = Array.AsReadOnly(moves);
        Purpose = purpose;
        Accounting = accounting;
        MoveCost = moveCost;
        IsInverse = isInverse;
    }

    public static RecordedOperation CreateSlide(RecordedMove move,
        OperationPlanPurpose purpose = OperationPlanPurpose.Solve)
    {
        ArgumentNullException.ThrowIfNull(move);
        var parameters = JsonSerializer.SerializeToElement(new SlideParametersData
        {
            Axis = move.Axis,
            Index = move.Index,
            Delta = move.Delta
        }, ParameterOptions);
        bool isSolve = purpose == OperationPlanPurpose.Solve;
        return new RecordedOperation(PuzzleOperationIds.Slide, parameters, new[] { move }, purpose,
            isSolve ? OperationMoveAccounting.LogicalOperation : OperationMoveAccounting.None,
            isSolve ? 1 : null);
    }

    internal RecordedOperation WithInverseMoves(IEnumerable<RecordedMove> moves) =>
        new(OperationId, Parameters, moves, Purpose, Accounting, MoveCost, !IsInverse);

    private sealed class SlideParametersData
    {
        public RecordedMoveAxis Axis { get; set; }
        public int Index { get; set; }
        public int Delta { get; set; }
    }
}

/// <summary>A portable initial board and ordered logical operations with exact primitive history.</summary>
public sealed class SessionRecording
{
    public const string Format = "SlideScramble";
    public const int Version = 3;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters =
        {
            new JsonStringEnumConverter<RecordedMoveAxis>(null, false),
            new JsonStringEnumConverter<OperationPlanPurpose>(null, false),
            new JsonStringEnumConverter<OperationMoveAccounting>(null, false)
        }
    };

    private readonly int[,] initialCells;
    private readonly RecordedOperation[] operations;
    private readonly RecordedMove[] moves;
    private readonly ReadOnlyCollection<RecordedOperation> operationView;
    private readonly ReadOnlyCollection<RecordedMove> moveView;
    private readonly PuzzleRuleSet ruleSet;
    private readonly OperationRegistry registry;

    public int[,] InitialCells => (int[,])initialCells.Clone();
    public IReadOnlyList<RecordedOperation> Operations => operationView;

    /// <summary>Flattened primitive playback history, derived from <see cref="Operations"/>.</summary>
    public IReadOnlyList<RecordedMove> Moves => moveView;

    public PuzzleRuleSet RuleSet => ruleSet.Snapshot();
    public int Width => initialCells.GetLength(1);
    public int Height => initialCells.GetLength(0);

    /// <summary>Creates a classic recording from the established primitive move representation.</summary>
    public SessionRecording(int[,] initialCells, IEnumerable<RecordedMove> moves)
        : this(initialCells, ConvertClassicMoves(moves), new PuzzleRuleSet())
    {
    }

    public SessionRecording(int[,] initialCells, IEnumerable<RecordedOperation> operations,
        PuzzleRuleSet? ruleSet = null, OperationRegistry? registry = null)
    {
        ArgumentNullException.ThrowIfNull(initialCells);
        ArgumentNullException.ThrowIfNull(operations);
        _ = new SlideScrambleSession(initialCells);
        this.initialCells = (int[,])initialCells.Clone();

        var suppliedRules = ruleSet ?? new PuzzleRuleSet();
        this.registry = registry ?? suppliedRules.Registry;
        this.ruleSet = new PuzzleRuleSet(suppliedRules.Name, suppliedRules.ScrambleAllowed,
            suppliedRules.SolveAllowed, suppliedRules.UIEnabled, this.registry);
        this.operations = operations.ToArray();
        if (this.operations.Any(operation => operation == null))
            throw new ArgumentException("Recording contains a null operation.", nameof(operations));
        operationView = Array.AsReadOnly(this.operations);
        moves = this.operations.SelectMany(operation => operation.PrimitiveMoves).ToArray();
        moveView = Array.AsReadOnly(moves);

        foreach (var operation in this.operations) ValidateOperation(operation);
    }

    /// <summary>Returns the primitive playback position at the first solved board, or null if never solved.</summary>
    public int? GetVerifiedGoal()
    {
        var session = new SlideScrambleSession(initialCells);
        if (session.IsSolved) return 0;
        for (int i = 0; i < moves.Length; i++)
        {
            var move = moves[i];
            if (move.Axis == RecordedMoveAxis.Row) session.MoveRow(move.Index, move.Delta, 0);
            else session.MoveColumn(move.Index, move.Delta, 0);
            if (session.IsSolved) return i + 1;
        }

        return null;
    }

    /// <summary>Returns the move cost at the first solved logical-operation boundary.</summary>
    public int? GetVerifiedGoalMoveCost()
    {
        var session = new SlideScrambleSession(initialCells);
        if (session.IsSolved) return 0;
        int moveCost = 0;
        foreach (var operation in operations)
        {
            foreach (var move in operation.PrimitiveMoves)
            {
                if (move.Axis == RecordedMoveAxis.Row) session.MoveRow(move.Index, move.Delta, 0);
                else session.MoveColumn(move.Index, move.Delta, 0);
            }

            if (operation.Purpose == OperationPlanPurpose.Solve)
                moveCost = checked(moveCost + (operation.MoveCost ?? 0));
            if (session.IsSolved) return moveCost;
        }

        return null;
    }

    /// <summary>Returns whether a primitive playback position lies between complete logical operations.</summary>
    public bool IsOperationBoundary(int primitivePosition)
    {
        if (primitivePosition < 0 || primitivePosition > moves.Length) return false;
        int boundary = 0;
        if (primitivePosition == 0) return true;
        foreach (var operation in operations)
        {
            boundary += operation.PrimitiveMoves.Count;
            if (boundary == primitivePosition) return true;
            if (boundary > primitivePosition) return false;
        }

        return false;
    }

    /// <summary>Returns the operation count retained at a valid primitive boundary.</summary>
    public int GetOperationCountAtBoundary(int primitivePosition)
    {
        if (!IsOperationBoundary(primitivePosition))
            throw new ArgumentOutOfRangeException(nameof(primitivePosition), "Position must be an operation boundary.");
        int operationCount = 0;
        int boundary = 0;
        while (boundary < primitivePosition)
            boundary += operations[operationCount++].PrimitiveMoves.Count;
        return operationCount;
    }

    /// <summary>Returns a recording that retraces this recording from its final board.</summary>
    public SessionRecording Reverse()
    {
        var cells = (int[,])initialCells.Clone();
        foreach (var operation in operations)
            foreach (var move in operation.PrimitiveMoves)
            {
                if (move.Axis == RecordedMoveAxis.Row) LogicalMoves.MoveRow(cells, move.Index, move.Delta);
                else LogicalMoves.MoveColumn(cells, move.Index, move.Delta);
            }

        var reversed = new List<RecordedOperation>(operations.Length);
        for (int operationIndex = operations.Length - 1; operationIndex >= 0; operationIndex--)
        {
            var operation = operations[operationIndex];
            var inverseMoves = new List<RecordedMove>(operation.PrimitiveMoves.Count);
            for (int moveIndex = operation.PrimitiveMoves.Count - 1; moveIndex >= 0; moveIndex--)
            {
                var move = operation.PrimitiveMoves[moveIndex];
                int length = move.Axis == RecordedMoveAxis.Row ? Width : Height;
                int shift = move.Delta % length;
                int start = move.Axis == RecordedMoveAxis.Row ? move.StartX : move.StartY;
                int destination = (int)(((long)start - shift + length) % length);
                int inverse = move.Delta == int.MinValue ? -shift : -move.Delta;
                var inverseMove = move.Axis == RecordedMoveAxis.Row
                    ? new RecordedMove(move.Axis, destination, move.StartY, inverse)
                    : new RecordedMove(move.Axis, move.StartX, destination, inverse);
                inverseMoves.Add(inverseMove);
            }

            reversed.Add(operation.WithInverseMoves(inverseMoves));
        }

        return new SessionRecording(cells, reversed, ruleSet, registry);
    }

    public string ToJson()
    {
        var rows = new int[Height][][];
        for (int y = 0; y < Height; y++)
        {
            rows[y] = new int[Width][];
            for (int x = 0; x < Width; x++)
                rows[y][x] = new[] { initialCells[y, x] % Width + 1, initialCells[y, x] / Width + 1 };
        }

        return JsonSerializer.Serialize(new RecordingData
        {
            Format = Format,
            Version = Version,
            Width = Width,
            Height = Height,
            InitialCells = rows,
            RuleSet = new RuleSetData
            {
                Name = ruleSet.Name,
                ScrambleAllowed = ruleSet.ScrambleAllowed.ToArray(),
                SolveAllowed = ruleSet.SolveAllowed.ToArray(),
                UIEnabled = ruleSet.UIEnabled.ToArray()
            },
            Operations = operations.Select(operation => new OperationData
            {
                OperationId = operation.OperationId,
                Parameters = operation.Parameters,
                Purpose = operation.Purpose,
                Accounting = operation.Accounting,
                MoveCost = operation.MoveCost,
                IsInverse = operation.IsInverse,
                Moves = operation.PrimitiveMoves.Select(ToMoveData).ToArray()
            }).ToArray()
        }, JsonOptions);
    }

    public static SessionRecording FromJson(string json, OperationRegistry? registry = null)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            var data = JsonSerializer.Deserialize<RecordingData>(json, JsonOptions) ??
                       throw new JsonException("Recording is null.");
            if (data.Format != Format || data.Version < 1 || data.Version > Version || data.Width <= 0 ||
                data.Height <= 0 ||
                data.Width > 256 || data.Height > 256 || data.InitialCells == null ||
                data.InitialCells.Length != data.Height)
                throw new JsonException("Recording must contain a supported format and nonempty board.");

            bool isVersion3 = data.Version == 3;
            if (isVersion3
                    ? data.Operations == null || data.Moves != null || data.RuleSet == null
                    : data.Moves == null || data.Operations != null)
                throw new JsonException("Recording history does not match its format version.");

            int width = data.Width;
            int offset = data.Version == 1 ? 0 : 1;
            var cells = new int[data.Height, width];
            for (int y = 0; y < cells.GetLength(0); y++)
            {
                if (data.InitialCells[y] == null || data.InitialCells[y].Length != width)
                    throw new JsonException("Board must be rectangular.");
                for (int x = 0; x < width; x++)
                {
                    var pair = data.InitialCells[y][x];
                    if (pair == null || pair.Length != 2 || pair[0] < offset || pair[0] >= width + offset ||
                        pair[1] < offset || pair[1] >= data.Height + offset)
                        throw new JsonException("Invalid tile coordinates.");
                    cells[y, x] = (pair[1] - offset) * width + pair[0] - offset;
                }
            }

            var selectedRegistry = registry ?? OperationRegistry.CreateDefault();
            PuzzleRuleSet rules;
            RecordedOperation[] recordedOperations;
            if (isVersion3)
            {
                rules = ReadRuleSet(data.RuleSet!, selectedRegistry);
                recordedOperations = data.Operations!
                    .Select(operation => ReadOperation(operation, width, data.Height, selectedRegistry)).ToArray();
            }
            else
            {
                rules = new PuzzleRuleSet();
                var legacyMoves = data.Moves!.Select(move => ReadMove(move, width, data.Height, offset)).ToArray();
                recordedOperations = ConvertClassicMoves(legacyMoves);
            }

            return new SessionRecording(cells, recordedOperations, rules, selectedRegistry);
        }
        catch (ArgumentException ex)
        {
            throw new JsonException("Invalid recording.", ex);
        }
        catch (OverflowException ex)
        {
            throw new JsonException("Invalid recording.", ex);
        }
    }

    private void ValidateOperation(RecordedOperation operation)
    {
        if (!registry.IsKnownOperation(operation.OperationId))
            throw new ArgumentException($"Unknown operation ID '{operation.OperationId}'.", nameof(operations));
        if (HasDuplicateProperties(operation.Parameters))
            throw new ArgumentException("Operation parameters contain duplicate property names.", nameof(operations));

        foreach (var move in operation.PrimitiveMoves)
        {
            if (!Enum.IsDefined(move.Axis) || move.StartX < 0 || move.StartX >= Width || move.StartY < 0 ||
                move.StartY >= Height ||
                move.Index < 0 || move.Index >= (move.Axis == RecordedMoveAxis.Row ? Height : Width) ||
                move.Delta % (move.Axis == RecordedMoveAxis.Row ? Width : Height) == 0)
                throw new ArgumentException("Recording contains an invalid or ineffective primitive move.",
                    nameof(operations));
        }

        ValidateOperationParameters(operation);
        ValidateAccounting(operation);
    }

    private void ValidateOperationParameters(RecordedOperation operation)
    {
        foreach (var parameter in operation.Parameters.EnumerateObject())
        {
            if (parameter.NameEquals("lensAnchor"))
            {
                if (parameter.Value.ValueKind != JsonValueKind.Array || parameter.Value.GetArrayLength() != 2)
                    throw new ArgumentException("Lens anchor must contain a column and row.", nameof(operations));
                var anchor = parameter.Value.EnumerateArray().ToArray();
                if (anchor[0].ValueKind != JsonValueKind.Number || !anchor[0].TryGetInt32(out int column) ||
                    anchor[1].ValueKind != JsonValueKind.Number || !anchor[1].TryGetInt32(out int row) ||
                    column < 0 || column >= Width || row < 0 || row >= Height)
                    throw new ArgumentException("Lens anchor is outside the board.", nameof(operations));
            }
            else if ((parameter.NameEquals("startCell") || parameter.NameEquals("targetCell")) &&
                     (parameter.Value.ValueKind != JsonValueKind.String ||
                      string.IsNullOrWhiteSpace(parameter.Value.GetString())))
                throw new ArgumentException("Symbolic lens cells must be nonempty strings.", nameof(operations));
        }

        if (!string.Equals(operation.OperationId, PuzzleOperationIds.Slide, StringComparison.Ordinal)) return;
        if (operation.Parameters.EnumerateObject().Count() != 3 ||
            !operation.Parameters.TryGetProperty("axis", out var axisValue) ||
            axisValue.ValueKind != JsonValueKind.String ||
            !Enum.TryParse(axisValue.GetString(), false, out RecordedMoveAxis axis) ||
            !operation.Parameters.TryGetProperty("index", out var indexValue) ||
            !indexValue.TryGetInt32(out int index) ||
            !operation.Parameters.TryGetProperty("delta", out var deltaValue) || !deltaValue.TryGetInt32(out int delta))
            throw new ArgumentException("Slide parameters require an axis, index, and displacement.",
                nameof(operations));

        var move = operation.PrimitiveMoves[0];
        int length = axis == RecordedMoveAxis.Row ? Width : Height;
        if (operation.PrimitiveMoves.Count != 1 || !Enum.IsDefined(axis) || index != move.Index || axis != move.Axis ||
            (operation.IsInverse ? ((long)delta + move.Delta) % length != 0 : ((long)delta - move.Delta) % length != 0))
            throw new ArgumentException("Slide parameters do not match the recorded primitive move.",
                nameof(operations));
    }

    private void ValidateAccounting(RecordedOperation operation)
    {
        if (operation.Purpose == OperationPlanPurpose.Scramble)
        {
            if (!ruleSet.AllowsScramble(operation.OperationId) ||
                operation.Accounting != OperationMoveAccounting.None || operation.MoveCost != null)
                throw new ArgumentException("Scramble operations must be allowed and have no solve move cost.",
                    nameof(operations));
            return;
        }

        if (!ruleSet.AllowsUi(operation.OperationId))
            throw new ArgumentException("Recorded solve operation is not enabled in the UI rule snapshot.",
                nameof(operations));
        if (ruleSet.AllowsSolve(operation.OperationId))
        {
            if (operation.Accounting != OperationMoveAccounting.LogicalOperation || operation.MoveCost != 1)
                throw new ArgumentException("A directly permitted operation must cost one logical move.",
                    nameof(operations));
            return;
        }

        if (!ruleSet.AllowsSolve(PuzzleOperationIds.Slide) ||
            operation.Accounting != OperationMoveAccounting.PrimitiveMoves ||
            operation.MoveCost != operation.PrimitiveMoves.Count)
            throw new ArgumentException("Expanded operations must be authorized and cost their primitive move count.",
                nameof(operations));
    }

    private static bool HasDuplicateProperties(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
                if (!names.Add(property.Name) || HasDuplicateProperties(property.Value))
                    return true;
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
                if (HasDuplicateProperties(item))
                    return true;
        }

        return false;
    }

    private static PuzzleRuleSet ReadRuleSet(RuleSetData data, OperationRegistry registry)
    {
        if (string.IsNullOrWhiteSpace(data.Name)) throw new JsonException("Rule-set name is required.");
        return new PuzzleRuleSet(data.Name, ReadOperationIds(data.ScrambleAllowed, "scrambleAllowed"),
            ReadOperationIds(data.SolveAllowed, "solveAllowed"), ReadOperationIds(data.UIEnabled, "uiEnabled"),
            registry);
    }

    private static string[] ReadOperationIds(string[]? ids, string propertyName)
    {
        if (ids == null || ids.Any(string.IsNullOrWhiteSpace) ||
            ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
            throw new JsonException($"Rule-set property '{propertyName}' must contain unique operation IDs.");
        return ids;
    }

    private static RecordedOperation ReadOperation(OperationData? data, int width, int height,
        OperationRegistry registry)
    {
        if (data == null || string.IsNullOrWhiteSpace(data.OperationId) ||
            data.Parameters.ValueKind != JsonValueKind.Object || data.Moves == null)
            throw new JsonException("Invalid operation record.");
        if (!registry.IsKnownOperation(data.OperationId))
            throw new JsonException($"Unknown operation ID '{data.OperationId}'.");
        var primitiveMoves = data.Moves.Select(move => ReadMove(move, width, height, 1)).ToArray();
        return new RecordedOperation(data.OperationId, data.Parameters, primitiveMoves, data.Purpose,
            data.Accounting, data.MoveCost, data.IsInverse);
    }

    private static RecordedMove ReadMove(MoveData? data, int width, int height, int offset)
    {
        if (data?.DragStart == null || data.DragStart.Length != 2) throw new JsonException("Invalid drag start.");
        if (data.DragStart[0] < offset || data.DragStart[0] >= width + offset ||
            data.DragStart[1] < offset || data.DragStart[1] >= height + offset)
            throw new JsonException("Invalid drag start.");
        var move = new RecordedMove(data.Axis, data.DragStart[0] - offset, data.DragStart[1] - offset, data.Delta);
        if (data.Index != move.Index + offset) throw new JsonException("Move index does not match drag start.");
        return move;
    }

    private static MoveData ToMoveData(RecordedMove move) => new()
    {
        Axis = move.Axis,
        Index = move.Index + 1,
        DragStart = new[] { move.StartX + 1, move.StartY + 1 },
        Delta = move.Delta
    };

    private static RecordedOperation[] ConvertClassicMoves(IEnumerable<RecordedMove> moves)
    {
        ArgumentNullException.ThrowIfNull(moves);
        return moves.Select(move => RecordedOperation.CreateSlide(move)).ToArray();
    }

    private sealed class RecordingData
    {
        [JsonRequired] public string? Format { get; set; }
        [JsonRequired] public int Version { get; set; }
        [JsonRequired] public int Width { get; set; }
        [JsonRequired] public int Height { get; set; }
        [JsonRequired] public int[][][]? InitialCells { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public MoveData[]? Moves { get; set; }

        public RuleSetData? RuleSet { get; set; }
        public OperationData[]? Operations { get; set; }
    }

    private sealed class RuleSetData
    {
        [JsonRequired] public string? Name { get; set; }
        [JsonRequired] public string[]? ScrambleAllowed { get; set; }
        [JsonRequired] public string[]? SolveAllowed { get; set; }
        [JsonRequired] public string[]? UIEnabled { get; set; }
    }

    private sealed class OperationData
    {
        [JsonRequired] public string? OperationId { get; set; }
        [JsonRequired] public JsonElement Parameters { get; set; }
        [JsonRequired] public OperationPlanPurpose Purpose { get; set; }
        [JsonRequired] public OperationMoveAccounting Accounting { get; set; }
        [JsonRequired] public int? MoveCost { get; set; }
        [JsonRequired] public bool IsInverse { get; set; }
        [JsonRequired] public MoveData[]? Moves { get; set; }
    }

    private sealed class MoveData
    {
        [JsonRequired] public RecordedMoveAxis Axis { get; set; }
        [JsonRequired] public int Index { get; set; }
        [JsonRequired] public int[]? DragStart { get; set; }
        [JsonRequired] public int Delta { get; set; }
    }
}