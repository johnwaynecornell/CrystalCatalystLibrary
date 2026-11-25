namespace SlideScramble;

public enum RecordingControllerMode { Idle, Recording, Playback }

/// <summary>Controls recording and caller-driven playback against an existing grid.</summary>
public sealed class SessionRecordingController
{
    private readonly Grid grid;
    private readonly List<RecordedMove> moves = new();
    private int[,]? recordingStart;
    private SessionRecording? playback;

    public RecordingControllerMode Mode { get; private set; }
    public bool IsAutoplaying { get; private set; }
    public int Position { get; private set; }
    public int MoveCount => playback?.Moves.Count ?? moves.Count;
    public SessionRecording? Playback => playback;

    public SessionRecordingController(Grid grid) => this.grid = grid ?? throw new ArgumentNullException(nameof(grid));

    public void StartRecording()
    {
        if (Mode != RecordingControllerMode.Idle) throw new InvalidOperationException("Controller must be idle.");
        recordingStart = Capture();
        moves.Clear();
        playback = null;
        Position = 0;
        Mode = RecordingControllerMode.Recording;
    }

    public SessionRecording StopRecording()
    {
        if (Mode != RecordingControllerMode.Recording) throw new InvalidOperationException("Not recording.");
        var result = new SessionRecording(recordingStart!, moves);
        playback = result;
        Position = result.Moves.Count;
        Mode = RecordingControllerMode.Idle;
        IsAutoplaying = false;
        return result;
    }

    public bool MoveRow(int row, int delta) => MoveRow(row, delta, 0, row);
    public bool MoveColumn(int column, int delta) => MoveColumn(column, delta, column, 0);
    public bool MoveRow(int startX, int startY, int delta) => MoveRow(startY, delta, startX, startY);
    public bool MoveColumn(int startX, int startY, int delta) => MoveColumn(startX, delta, startX, startY);
    public bool MoveRow(int row, int delta, int startX, int startY) => Move(RecordedMoveAxis.Row, row, delta, startX, startY);
    public bool MoveColumn(int column, int delta, int startX, int startY) => Move(RecordedMoveAxis.Column, column, delta, startX, startY);

    private bool Move(RecordedMoveAxis axis, int index, int delta, int startX, int startY)
    {
        if (Mode == RecordingControllerMode.Playback) throw new InvalidOperationException("Cannot make manual moves during playback.");
        int lines = axis == RecordedMoveAxis.Row ? grid.Height : grid.Width;
        int length = axis == RecordedMoveAxis.Row ? grid.Width : grid.Height;
        if (index < 0 || index >= lines) throw new ArgumentOutOfRangeException(nameof(index));
        if (startX < 0 || startX >= grid.Width || startY < 0 || startY >= grid.Height ||
            (axis == RecordedMoveAxis.Row ? startY : startX) != index)
            throw new ArgumentException("Drag start must be on the moved line and within the grid.");
        if (delta % length == 0) return false;
        var move = new RecordedMove(axis, startX, startY, delta);
        Apply(move);
        if (Mode == RecordingControllerMode.Recording)
        {
            moves.Add(move);
            Position = moves.Count;
        }
        return true;
    }

    public void LoadPlayback(SessionRecording recording)
    {
        ArgumentNullException.ThrowIfNull(recording);
        if (Mode == RecordingControllerMode.Recording) throw new InvalidOperationException("Stop recording first.");
        if (recording.Width != grid.Width || recording.Height != grid.Height) throw new ArgumentException("Recording dimensions must match the grid.", nameof(recording));
        Restore(recording.InitialCells);
        playback = recording;
        Position = 0;
        IsAutoplaying = false;
        Mode = RecordingControllerMode.Playback;
    }

    public bool Step()
    {
        if (Mode != RecordingControllerMode.Playback) throw new InvalidOperationException("Not in playback.");
        if (Position == playback!.Moves.Count) { IsAutoplaying = false; return false; }
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

    /// <summary>
    /// Continue recording from the currently displayed playback position.
    /// The existing source recording is immutable; only this controller's working
    /// history is replaced by the prefix [0, Position).
    /// </summary>
    public int ContinueRecordingFromHere()
    {
        if (Mode != RecordingControllerMode.Playback || playback == null)
            throw new InvalidOperationException("Inspect a recording before continuing from an index.");

        // A position of N means exactly N moves have already been applied to grid.
        // Capture the original initial arrangement, not the current board, so a
        // new recording can replay the preserved prefix and any appended moves.
        int prefixCount = Position;
        recordingStart = playback.InitialCells;
        moves.Clear();
        for (int i = 0; i < prefixCount; i++) moves.Add(playback.Moves[i]);
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
        if (Mode != RecordingControllerMode.Idle || playback == null) throw new InvalidOperationException("No stopped playback to resume.");
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
}