using System.Text.Json;

namespace SlideScramble;

public enum RecordingControllerMode
{
    Idle,
    Recording,
    Playback
}

/// <summary>Controls recording and caller-driven primitive playback against an existing grid.</summary>
public sealed class SessionRecordingController
{
    private readonly Grid grid;
    private readonly List<RecordedOperation> operations = new();
    private PuzzleRuleSet configuredRules;
    private PuzzleRuleSet? recordingRules;
    private int[,]? recordingStart;
    private SessionRecording? playback;

    public RecordingControllerMode Mode { get; private set; }
    public bool IsAutoplaying { get; private set; }

    /// <summary>Primitive playback position, which may be inside a logical operation.</summary>
    public int Position { get; private set; }

    public int MoveCount => playback?.Moves.Count ?? operations.Sum(operation => operation.PrimitiveMoves.Count);
    public SessionRecording? Playback => playback;
    public PuzzleRuleSet RuleSet => (recordingRules ?? playback?.RuleSet ?? configuredRules).Snapshot();

    public SessionRecordingController(Grid grid, PuzzleRuleSet? rules = null)
    {
        this.grid = grid ?? throw new ArgumentNullException(nameof(grid));
        configuredRules = (rules ?? new PuzzleRuleSet()).Snapshot();
    }

    /// <summary>Changes rules only for a future recording; an existing playback keeps its saved snapshot.</summary>
    public void ConfigureRules(PuzzleRuleSet rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        if (Mode != RecordingControllerMode.Idle)
            throw new InvalidOperationException("Rules can only change while idle.");
        configuredRules = rules.Snapshot();
    }

    public void StartRecording()
    {
        if (Mode != RecordingControllerMode.Idle) throw new InvalidOperationException("Controller must be idle.");
        recordingStart = Capture();
        operations.Clear();
        playback = null;
        recordingRules = configuredRules.Snapshot();
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
            (!recordingRules!.AllowsUi(PuzzleOperationIds.Slide) ||
             !recordingRules.AllowsSolve(PuzzleOperationIds.Slide)))
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