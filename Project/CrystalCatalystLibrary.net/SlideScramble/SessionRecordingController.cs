using System.Text.Json;

namespace SlideScramble;

public enum RecordingControllerMode
{
    Idle,
    Recording,
    Playback
}

/// <summary>A saved operation's primitive slice that can be replayed as one playback action.</summary>
public sealed class RecordedOperationPlaybackStep
{
    internal SessionRecording Recording { get; }
    internal int OperationStartPosition { get; }
    internal int OperationEndPosition { get; }
    internal OperationBoardSnapshot SourceBoard { get; }
    internal bool IsCommitted { get; set; }

    public RecordedOperation Operation { get; }
    public int StartPosition { get; }
    public int TargetPosition { get; }
    public bool IsForward { get; }
    public bool IsCompleteOperation => IsForward
        ? StartPosition == OperationStartPosition && TargetPosition == OperationEndPosition
        : StartPosition == OperationEndPosition && TargetPosition == OperationStartPosition;
    public IReadOnlyList<RecordedMove> Moves { get; }

    internal RecordedOperationPlaybackStep(SessionRecording recording, RecordedOperation operation,
        int operationStartPosition, int operationEndPosition, int startPosition, int targetPosition, bool isForward,
        IEnumerable<RecordedMove> moves, OperationBoardSnapshot sourceBoard)
    {
        Recording = recording;
        Operation = operation;
        OperationStartPosition = operationStartPosition;
        OperationEndPosition = operationEndPosition;
        StartPosition = startPosition;
        TargetPosition = targetPosition;
        IsForward = isForward;
        Moves = Array.AsReadOnly(moves.ToArray());
        SourceBoard = sourceBoard.Copy();
    }

    /// <summary>Creates visual snapshots from saved primitives, without invoking an operation resolver.</summary>
    public OperationAnimationPlan CreateAnimationPlan(OperationBoardSnapshot board)
    {
        ArgumentNullException.ThrowIfNull(board);
        if (board.Width != SourceBoard.Width || board.Height != SourceBoard.Height ||
            !BoardsEqual(board.Cells, SourceBoard.Cells))
            throw new InvalidOperationException("Playback board changed before the operation could be animated.");
        IOperationIntent intent = new PlaybackIntent(Operation.OperationId);
        var parameters = Operation.Parameters;
        (int X, int Y)? lensAnchor = ReadPlaybackLensAnchor(board);
        var (x, y) = lensAnchor.GetValueOrDefault();
        if (Operation.OperationId == PuzzleOperationIds.Shuffle && lensAnchor.HasValue &&
            parameters.TryGetProperty("startCell", out var start) && start.ValueKind == JsonValueKind.String &&
            parameters.TryGetProperty("targetCell", out var target) && target.ValueKind == JsonValueKind.String &&
            Enum.TryParse(start.GetString(), false, out PortableLensCell startCell) && Enum.IsDefined(startCell) &&
            Enum.TryParse(target.GetString(), false, out PortableLensCell targetCell) && Enum.IsDefined(targetCell))
            intent = new ShuffleOperationIntent(x, y, startCell, targetCell);

        var plan = new OperationPlan(Operation.OperationId, intent, board, Operation.Purpose, Moves,
            Operation.Accounting, Operation.MoveCost);
        bool lensMetadata = lensAnchor.HasValue && (Operation.OperationId != PuzzleOperationIds.Shuffle ||
                                                    intent is ShuffleOperationIntent);
        OperationAnimationPlan BuildVisual(bool allowLogical) => new(board, plan, allowLogical)
        {
            PlaybackLensAnchor = lensAnchor
        };

        var visual = BuildVisual(IsCompleteOperation && lensMetadata);
        if (!visual.SupportsLogicalAnimation) return visual;

        var lensCells = Enum.GetValues<PortableLensCell>()
            .Select(cell => LensGestureOperationMapper.BoardPosition(cell, (x, y), board.Width, board.Height))
            .ToHashSet();
        bool local = visual.LogicalMotions.All(motion => lensCells.Contains((motion.SourceX, motion.SourceY)) &&
                                                          lensCells.Contains((motion.DestinationX, motion.DestinationY)));
        bool paired = Operation.OperationId is not (PuzzleOperationIds.ColumnSwap or PuzzleOperationIds.RowSwap) ||
                      visual.LogicalMotions.All(motion => visual.LogicalMotions.Any(other =>
                          other.SourceX == motion.DestinationX && other.SourceY == motion.DestinationY &&
                          other.DestinationX == motion.SourceX && other.DestinationY == motion.SourceY));
        return local && paired ? visual : BuildVisual(false);
    }

    private (int X, int Y)? ReadPlaybackLensAnchor(OperationBoardSnapshot board)
    {
        if (Operation.OperationId is not (PuzzleOperationIds.Shuffle or PuzzleOperationIds.ColumnSwap or
                PuzzleOperationIds.RowSwap) ||
            board.Width < 2 || board.Height < 2 ||
            !Operation.Parameters.TryGetProperty("lensAnchor", out var anchor))
            return null;

        // SessionRecording validates the shape and bounds of canonical lens anchors on load.
        return (anchor[0].GetInt32(), anchor[1].GetInt32());
    }

    private sealed record PlaybackIntent(string OperationId) : IOperationIntent;

    internal static bool BoardsEqual(int[,] left, int[,] right)
    {
        if (left.GetLength(0) != right.GetLength(0) || left.GetLength(1) != right.GetLength(1)) return false;
        for (int y = 0; y < left.GetLength(0); y++)
        for (int x = 0; x < left.GetLength(1); x++)
            if (left[y, x] != right[y, x]) return false;
        return true;
    }
}

/// <summary>Controls recording and caller-driven primitive playback against an existing grid.</summary>
public sealed class SessionRecordingController
{
    private readonly Grid grid;
    private readonly List<RecordedOperation> operations = new();
    private readonly HashSet<string> runtimeUiEnabled = new(StringComparer.Ordinal);
    private PuzzleRuleSet configuredRules;
    private PuzzleRuleSet? recordingRules;
    private int[,]? recordingStart;
    private SessionRecording? playback;

    public RecordingControllerMode Mode { get; private set; }
    public bool IsAutoplaying { get; private set; }

    /// <summary>Primitive playback position, which may be inside a logical operation.</summary>
    public int Position { get; private set; }

    public int MoveCount => playback?.Moves.Count ?? operations.Sum(operation => operation.PrimitiveMoves.Count);
    public int OperationCount => CurrentOperations.Count;
    public int CompletedOperationCount
    {
        get
        {
            int position = Math.Clamp(Position, 0, MoveCount);
            int boundary = 0;
            int completed = 0;
            foreach (RecordedOperation operation in CurrentOperations)
            {
                boundary += operation.PrimitiveMoves.Count;
                if (boundary > position) break;
                completed++;
            }

            return completed;
        }
    }

    /// <summary>One-based operation index when the primitive position is inside an operation.</summary>
    public int? CurrentOperationIndex
    {
        get
        {
            int start = 0;
            for (int i = 0; i < CurrentOperations.Count; i++)
            {
                int end = start + CurrentOperations[i].PrimitiveMoves.Count;
                if (Position > start && Position < end) return i + 1;
                start = end;
            }

            return null;
        }
    }

    public SessionRecording? Playback => playback;
    public PuzzleRuleSet RuleSet => (recordingRules ?? playback?.RuleSet ?? configuredRules).Snapshot();
    public IReadOnlyCollection<string> RuntimeUIEnabled => runtimeUiEnabled.Order(StringComparer.Ordinal).ToArray();

    private IReadOnlyList<RecordedOperation> CurrentOperations => playback?.Operations ?? operations;

    public SessionRecordingController(Grid grid, PuzzleRuleSet? rules = null)
    {
        this.grid = grid ?? throw new ArgumentNullException(nameof(grid));
        configuredRules = (rules ?? new PuzzleRuleSet()).Snapshot();
        runtimeUiEnabled.UnionWith(configuredRules.UIEnabled);
    }

    /// <summary>Starts a new idle rules context without changing the board.</summary>
    public void ConfigureRules(PuzzleRuleSet rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        if (Mode != RecordingControllerMode.Idle)
            throw new InvalidOperationException("Rules can only change while idle.");
        configuredRules = rules.Snapshot();
        playback = null;
        recordingRules = null;
        operations.Clear();
        Position = 0;
        runtimeUiEnabled.Clear();
        runtimeUiEnabled.UnionWith(configuredRules.UIEnabled);
    }

    /// <summary>Changes live input availability without changing the saved rule snapshot or recording history.</summary>
    public bool SetRuntimeUIEnabled(string operationId, bool enabled)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        var rules = recordingRules ?? playback?.RuleSet ?? configuredRules;
        if (!rules.Registry.TryGetOperation(operationId, out _)) return false;
        return enabled ? runtimeUiEnabled.Add(operationId) : runtimeUiEnabled.Remove(operationId);
    }

    public void StartRecording()
    {
        if (Mode != RecordingControllerMode.Idle) throw new InvalidOperationException("Controller must be idle.");
        recordingStart = Capture();
        operations.Clear();
        playback = null;
        recordingRules = configuredRules.WithUIEnabled(runtimeUiEnabled).Snapshot();
        Position = 0;
        Mode = RecordingControllerMode.Recording;
    }

    public SessionRecording StopRecording()
    {
        if (Mode != RecordingControllerMode.Recording) throw new InvalidOperationException("Not recording.");
        var result = new SessionRecording(recordingStart!, operations, recordingRules);
        playback = result;
        Position = result.Moves.Count;
        recordingRules = null;
        Mode = RecordingControllerMode.Idle;
        IsAutoplaying = false;
        return result;
    }

    public bool MoveRow(int row, int delta) => MoveRow(row, delta, 0, row);
    public bool MoveColumn(int column, int delta) => MoveColumn(column, delta, column, 0);
    public bool MoveRow(int startX, int startY, int delta) => MoveRow(startY, delta, startX, startY);
    public bool MoveColumn(int startX, int startY, int delta) => MoveColumn(startX, delta, startX, startY);

    public bool MoveRow(int row, int delta, int startX, int startY) =>
        Move(RecordedMoveAxis.Row, row, delta, startX, startY);

    public bool MoveColumn(int column, int delta, int startX, int startY) =>
        Move(RecordedMoveAxis.Column, column, delta, startX, startY);

    /// <summary>Atomically applies a previously authorized plan and records its logical boundary when recording.</summary>
    public void ApplyOperation(OperationPlan plan, JsonElement parameters)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (Mode == RecordingControllerMode.Playback)
            throw new InvalidOperationException("Cannot make manual moves during playback.");
        var rules = recordingRules ?? configuredRules;
        var before = Capture();
        if (!BoardsEqual(before, plan.SourceBoard.Cells))
            throw new InvalidOperationException("Operation plan was resolved for a different board state.");
        if (plan.Intent == null || !string.Equals(plan.Intent.OperationId, plan.OperationId, StringComparison.Ordinal))
            throw new ArgumentException("Operation plan identity does not match its intent.", nameof(plan));

        var evaluator = new OperationRuleEvaluator(rules.Registry);
        var evaluation = plan.Purpose == OperationPlanPurpose.Solve
            ? evaluator.EvaluateUiRequest(new OperationBoardSnapshot(before), plan.Intent, rules, RuntimeUIEnabled)
            : evaluator.EvaluateScrambleRequest(new OperationBoardSnapshot(before), plan.Intent, rules);
        var currentPlan = evaluation.Plan;
        if (currentPlan == null || currentPlan.Accounting != plan.Accounting || currentPlan.MoveCost != plan.MoveCost ||
            !currentPlan.Expansion.SequenceEqual(plan.Expansion))
            throw new InvalidOperationException(evaluation.RejectionReason ?? "Operation plan is no longer authorized.");
        JsonElement? expectedParameters = plan.Intent switch
        {
            ShuffleOperationIntent shuffle => shuffle.ToRecordingParameters(),
            PortableLensSwapIntent swap => swap.ToRecordingParameters(),
            _ => null
        };
        if (expectedParameters.HasValue && !JsonElement.DeepEquals(expectedParameters.Value, parameters))
            throw new ArgumentException("Recording parameters do not match the operation intent.", nameof(parameters));

        var operation = new RecordedOperation(plan.OperationId, parameters, plan.Expansion, plan.Purpose,
            plan.Accounting, plan.MoveCost);
        _ = new SessionRecording(before, new[] { operation }, rules);

        try
        {
            foreach (var move in operation.PrimitiveMoves) Apply(move);
        }
        catch
        {
            Restore(before);
            throw;
        }

        if (Mode == RecordingControllerMode.Recording)
        {
            operations.Add(operation);
            Position = MoveCount;
        }
    }

    private bool Move(RecordedMoveAxis axis, int index, int delta, int startX, int startY)
    {
        if (Mode == RecordingControllerMode.Playback)
            throw new InvalidOperationException("Cannot make manual moves during playback.");
        int lines = axis == RecordedMoveAxis.Row ? grid.Height : grid.Width;
        int length = axis == RecordedMoveAxis.Row ? grid.Width : grid.Height;
        if (index < 0 || index >= lines) throw new ArgumentOutOfRangeException(nameof(index));
        if (startX < 0 || startX >= grid.Width || startY < 0 || startY >= grid.Height ||
            (axis == RecordedMoveAxis.Row ? startY : startX) != index)
            throw new ArgumentException("Drag start must be on the moved line and within the grid.");
        if (delta % length == 0) return false;
        if (Mode == RecordingControllerMode.Recording &&
            (!runtimeUiEnabled.Contains(PuzzleOperationIds.Slide) ||
             !recordingRules!.AllowsSolve(PuzzleOperationIds.Slide)))
            throw new InvalidOperationException("Slide is not enabled and solve-allowed by the recording rules.");

        var move = new RecordedMove(axis, startX, startY, delta);
        Apply(move);
        if (Mode == RecordingControllerMode.Recording)
        {
            operations.Add(RecordedOperation.CreateSlide(move));
            Position = MoveCount;
        }

        return true;
    }

    public void LoadPlayback(SessionRecording recording)
    {
        ArgumentNullException.ThrowIfNull(recording);
        if (Mode == RecordingControllerMode.Recording) throw new InvalidOperationException("Stop recording first.");
        if (recording.Width != grid.Width || recording.Height != grid.Height)
            throw new ArgumentException("Recording dimensions must match the grid.", nameof(recording));
        Restore(recording.InitialCells);
        playback = recording;
        configuredRules = recording.RuleSet.Snapshot();
        runtimeUiEnabled.Clear();
        runtimeUiEnabled.UnionWith(recording.RuleSet.UIEnabled);
        recordingRules = null;
        Position = 0;
        IsAutoplaying = false;
        Mode = RecordingControllerMode.Playback;
    }

    public bool Step()
    {
        if (Mode != RecordingControllerMode.Playback) throw new InvalidOperationException("Not in playback.");
        if (Position == playback!.Moves.Count)
        {
            IsAutoplaying = false;
            return false;
        }

        Apply(playback.Moves[Position++]);
        if (Position == playback.Moves.Count) IsAutoplaying = false;
        return true;
    }

    public void ResetPlayback()
    {
        if (Mode != RecordingControllerMode.Playback) throw new InvalidOperationException("Not in playback.");
        Restore(playback!.InitialCells);
        Position = 0;
        IsAutoplaying = false;
    }

    public bool StepBack()
    {
        if (Mode != RecordingControllerMode.Playback) throw new InvalidOperationException("Not in playback.");
        IsAutoplaying = false;
        if (Position == 0) return false;
        var move = playback!.Moves[--Position];
        int length = move.Axis == RecordedMoveAxis.Row ? grid.Width : grid.Height;
        Apply(move with { Delta = -(move.Delta % length) });
        return true;
    }

    /// <summary>Gets the remaining part of the next operation in the requested playback direction.</summary>
    public RecordedOperationPlaybackStep? GetPlaybackOperationStep(bool forward)
    {
        if (Mode != RecordingControllerMode.Playback || playback == null)
            throw new InvalidOperationException("Not in playback.");

        int operationStart = 0;
        foreach (RecordedOperation operation in playback.Operations)
        {
            int operationEnd = operationStart + operation.PrimitiveMoves.Count;
            if (forward && Position >= operationStart && Position < operationEnd)
            {
                RecordedMove[] moves = operation.PrimitiveMoves.Skip(Position - operationStart).ToArray();
                return new RecordedOperationPlaybackStep(playback, operation, operationStart, operationEnd,
                    Position, operationEnd, true, moves, new OperationBoardSnapshot(Capture()));
            }

            if (!forward && Position > operationStart && Position <= operationEnd)
            {
                RecordedMove[] moves = operation.PrimitiveMoves.Take(Position - operationStart)
                    .Reverse().Select(InvertPlaybackMove).ToArray();
                return new RecordedOperationPlaybackStep(playback, operation, operationStart, operationEnd,
                    Position, operationStart, false, moves, new OperationBoardSnapshot(Capture()));
            }

            operationStart = operationEnd;
        }

        return null;
    }

    /// <summary>Commits an operation playback slice once, restoring the board if applying any primitive fails.</summary>
    public void CommitPlaybackOperationStep(RecordedOperationPlaybackStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        if (step.IsCommitted || Mode != RecordingControllerMode.Playback || !ReferenceEquals(playback, step.Recording) ||
            Position != step.StartPosition)
            throw new InvalidOperationException("Playback changed before the operation could be committed.");

        int[,] before = Capture();
        if (!RecordedOperationPlaybackStep.BoardsEqual(before, step.SourceBoard.Cells))
            throw new InvalidOperationException("Playback board changed before the operation could be committed.");
        try
        {
            foreach (RecordedMove move in step.Moves) Apply(move);
            Position = step.TargetPosition;
            step.IsCommitted = true;
            if (!step.IsForward || Position == playback!.Moves.Count) IsAutoplaying = false;
        }
        catch
        {
            Restore(before);
            throw;
        }
    }

    public void SetAutoplay(bool enabled)
    {
        if (Mode != RecordingControllerMode.Playback) throw new InvalidOperationException("Not in playback.");
        IsAutoplaying = enabled && Position < playback!.Moves.Count;
    }

    public bool AutoplayTick() => IsAutoplaying && Step();

    public bool IsOperationBoundary(int primitivePosition)
    {
        if (playback != null) return playback.IsOperationBoundary(primitivePosition);
        if (primitivePosition < 0 || primitivePosition > MoveCount) return false;
        if (primitivePosition == 0) return true;
        int boundary = 0;
        foreach (var operation in operations)
        {
            boundary += operation.PrimitiveMoves.Count;
            if (boundary == primitivePosition) return true;
            if (boundary > primitivePosition) return false;
        }

        return false;
    }

    /// <summary>
    /// Continues only at a complete logical-operation boundary. The original source recording stays immutable;
    /// the working history keeps the original initial board, rule snapshot, and complete prefix.
    /// </summary>
    public int ContinueRecordingFromHere()
    {
        if (Mode != RecordingControllerMode.Playback || playback == null)
            throw new InvalidOperationException("Inspect a recording before continuing from an index.");
        if (!playback.IsOperationBoundary(Position))
            throw new InvalidOperationException("Continue recording only at a complete logical-operation boundary.");

        int prefixCount = Position;
        int operationCount = playback.GetOperationCountAtBoundary(prefixCount);
        recordingStart = playback.InitialCells;
        operations.Clear();
        operations.AddRange(playback.Operations.Take(operationCount));
        recordingRules = playback.RuleSet.Snapshot();
        playback = null;
        Position = prefixCount;
        IsAutoplaying = false;
        Mode = RecordingControllerMode.Recording;
        return prefixCount;
    }

    public void StopPlayback()
    {
        if (Mode != RecordingControllerMode.Playback) throw new InvalidOperationException("Not in playback.");
        Mode = RecordingControllerMode.Idle;
        IsAutoplaying = false;
    }

    public void ResumePlayback()
    {
        if (Mode != RecordingControllerMode.Idle || playback == null)
            throw new InvalidOperationException("No stopped playback to resume.");
        Restore(playback.InitialCells);
        for (int i = 0; i < Position; i++) Apply(playback.Moves[i]);
        Mode = RecordingControllerMode.Playback;
    }

    private void Apply(RecordedMove move)
    {
        if (move.Axis == RecordedMoveAxis.Row) grid.MoveRow(move.Index, move.Delta);
        else grid.MoveColumn(move.Index, move.Delta);
    }

    private RecordedMove InvertPlaybackMove(RecordedMove move)
    {
        int length = move.Axis == RecordedMoveAxis.Row ? grid.Width : grid.Height;
        int delta = -(move.Delta % length);
        int startX = move.StartX;
        int startY = move.StartY;
        if (move.Axis == RecordedMoveAxis.Row)
            startX = (startX + delta + length) % length;
        else
            startY = (startY + delta + length) % length;
        return move with { StartX = startX, StartY = startY, Delta = delta };
    }

    private int[,] Capture()
    {
        var cells = new int[grid.Height, grid.Width];
        for (int y = 0; y < grid.Height; y++)
        for (int x = 0; x < grid.Width; x++)
            cells[y, x] = grid.Tiles[y, x].OriginY * grid.Width + grid.Tiles[y, x].OriginX;
        return cells;
    }

    private void Restore(int[,] cells)
    {
        var tiles = new Tile[grid.Height * grid.Width];
        foreach (var tile in grid.Tiles) tiles[tile.OriginY * grid.Width + tile.OriginX] = tile;
        for (int y = 0; y < grid.Height; y++)
        for (int x = 0; x < grid.Width; x++)
            grid.Tiles[y, x] = tiles[cells[y, x]];
    }

    private static bool BoardsEqual(int[,] left, int[,] right)
    {
        if (left.GetLength(0) != right.GetLength(0) || left.GetLength(1) != right.GetLength(1)) return false;
        for (int y = 0; y < left.GetLength(0); y++)
        for (int x = 0; x < left.GetLength(1); x++)
            if (left[y, x] != right[y, x])
                return false;
        return true;
    }
}