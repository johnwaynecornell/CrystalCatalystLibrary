using System.Text.Json;
using System.Text.Json.Serialization;

namespace SlideScramble;

public enum RecordedMoveAxis { Row, Column }

public sealed record RecordedMove(RecordedMoveAxis Axis, int StartX, int StartY, int Delta)
{
    public RecordedMove(RecordedMoveAxis axis, int index, int delta)
        : this(axis, axis == RecordedMoveAxis.Column ? index : 0, axis == RecordedMoveAxis.Row ? index : 0, delta) { }

    public int Index => Axis == RecordedMoveAxis.Row ? StartY : StartX;
}

/// <summary>A portable initial board and ordered sequence of grid moves.</summary>
public sealed class SessionRecording
{
    public const string Format = "SlideScramble";
    public const int Version = 2;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter<RecordedMoveAxis>(null, false) }
    };
    private readonly int[,] initialCells;
    private readonly RecordedMove[] moves;

    public int[,] InitialCells => (int[,])initialCells.Clone();
    public IReadOnlyList<RecordedMove> Moves => Array.AsReadOnly(moves);
    public int Width => initialCells.GetLength(1);
    public int Height => initialCells.GetLength(0);

    public SessionRecording(int[,] initialCells, IEnumerable<RecordedMove> moves)
    {
        ArgumentNullException.ThrowIfNull(initialCells);
        ArgumentNullException.ThrowIfNull(moves);
        _ = new SlideScrambleSession(initialCells);
        this.initialCells = (int[,])initialCells.Clone();
        this.moves = moves.ToArray();
        foreach (var move in this.moves)
        {
            if (move == null || !Enum.IsDefined(move.Axis) || move.StartX < 0 || move.StartX >= Width ||
                move.StartY < 0 || move.StartY >= Height ||
                move.Delta % (move.Axis == RecordedMoveAxis.Row ? Width : Height) == 0)
                throw new ArgumentException("Recording contains an invalid or ineffective move.", nameof(moves));
        }
    }

    /// <summary>Returns the number of moves at the first solved position, or null if never solved.</summary>
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

    /// <summary>Returns a recording that retraces this recording from its final board.</summary>
    public SessionRecording Reverse()
    {
        var cells = (int[,])initialCells.Clone();
        var reversed = new RecordedMove[moves.Length];
        for (int i = 0; i < moves.Length; i++)
        {
            var move = moves[i];
            int length = move.Axis == RecordedMoveAxis.Row ? Width : Height;
            int shift = move.Delta % length;
            int start = move.Axis == RecordedMoveAxis.Row ? move.StartX : move.StartY;
            int destination = (int)(((long)start - shift + length) % length);
            int inverse = move.Delta == int.MinValue ? -shift : -move.Delta;
            reversed[moves.Length - 1 - i] = move.Axis == RecordedMoveAxis.Row
                ? new RecordedMove(move.Axis, destination, move.StartY, inverse)
                : new RecordedMove(move.Axis, move.StartX, destination, inverse);
            if (move.Axis == RecordedMoveAxis.Row) LogicalMoves.MoveRow(cells, move.Index, move.Delta);
            else LogicalMoves.MoveColumn(cells, move.Index, move.Delta);
        }
        return new SessionRecording(cells, reversed);
    }

    public string ToJson()
    {
        var rows = new int[Height][][];
        for (int y = 0; y < Height; y++)
        {
            rows[y] = new int[Width][];
            for (int x = 0; x < Width; x++) rows[y][x] = new[] { initialCells[y, x] % Width + 1, initialCells[y, x] / Width + 1 };
        }
        return JsonSerializer.Serialize(new RecordingData
        {
            Format = Format, Version = Version, Width = Width, Height = Height, InitialCells = rows,
            Moves = moves.Select(m => new MoveData { Axis = m.Axis, Index = m.Index + 1, DragStart = new[] { m.StartX + 1, m.StartY + 1 }, Delta = m.Delta }).ToArray()
        }, JsonOptions);
    }

    public static SessionRecording FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            var data = JsonSerializer.Deserialize<RecordingData>(json, JsonOptions) ?? throw new JsonException("Recording is null.");
            if (data.Format != Format || (data.Version != 1 && data.Version != Version) || data.Width <= 0 || data.Height <= 0 || data.Width > 256 || data.Height > 256 ||
                data.InitialCells == null || data.InitialCells.Length != data.Height || data.Moves == null)
                throw new JsonException("Recording must contain a nonempty board and moves.");
            int width = data.Width;
            int offset = data.Version == 1 ? 0 : 1;
            var cells = new int[data.Height, width];
            for (int y = 0; y < cells.GetLength(0); y++)
            {
                if (data.InitialCells[y] == null || data.InitialCells[y].Length != width) throw new JsonException("Board must be rectangular.");
                for (int x = 0; x < width; x++)
                {
                    var pair = data.InitialCells[y][x];
                    if (pair == null || pair.Length != 2 || pair[0] < offset || pair[0] >= width + offset || pair[1] < offset || pair[1] >= data.Height + offset)
                        throw new JsonException("Invalid tile coordinates.");
                    cells[y, x] = (pair[1] - offset) * width + pair[0] - offset;
                }
            }
            var recordedMoves = data.Moves.Select(m =>
            {
                if (m?.DragStart == null || m.DragStart.Length != 2) throw new JsonException("Invalid drag start.");
                if (m.DragStart[0] < offset || m.DragStart[0] >= width + offset ||
                    m.DragStart[1] < offset || m.DragStart[1] >= data.Height + offset)
                    throw new JsonException("Invalid drag start.");
                var move = new RecordedMove(m.Axis, m.DragStart[0] - offset, m.DragStart[1] - offset, m.Delta);
                if (m.Index != move.Index + offset) throw new JsonException("Move index does not match drag start.");
                return move;
            });
            return new SessionRecording(cells, recordedMoves);
        }
        catch (ArgumentException ex) { throw new JsonException("Invalid recording.", ex); }
        catch (OverflowException ex) { throw new JsonException("Invalid recording.", ex); }
    }

    private sealed class RecordingData
    {
        [JsonRequired] public string? Format { get; set; }
        [JsonRequired] public int Version { get; set; }
        [JsonRequired] public int Width { get; set; }
        [JsonRequired] public int Height { get; set; }
        [JsonRequired] public int[][][]? InitialCells { get; set; }
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