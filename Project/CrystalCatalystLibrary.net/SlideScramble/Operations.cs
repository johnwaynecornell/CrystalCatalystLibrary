using System.Collections.ObjectModel;

namespace SlideScramble;

public static class PuzzleOperationIds
{
    public const string Slide = "Slide";
    public const string Shuffle = "Shuffle";
    public const string ColumnSwap = "ColumnSwap";
    public const string RowSwap = "RowSwap";
}

/// <summary>A read-only view of tile identities, using zero-based row-major solved IDs.</summary>
public sealed class OperationBoardSnapshot
{
    private readonly int[,] cells;

    public int Width { get; }
    public int Height { get; }
    public int[,] Cells => (int[,])cells.Clone();

    public OperationBoardSnapshot(int[,] cells)
    {
        ArgumentNullException.ThrowIfNull(cells);
        Height = cells.GetLength(0);
        Width = cells.GetLength(1);
        if (Width == 0 || Height == 0)
            throw new ArgumentException("Board dimensions must be positive.", nameof(cells));

        int tileCount = checked(Width * Height);
        var seen = new bool[tileCount];
        foreach (int tileId in cells)
        {
            if (tileId < 0 || tileId >= tileCount || seen[tileId])
                throw new ArgumentException("Cells must be a permutation of zero-based row-major IDs.", nameof(cells));
            seen[tileId] = true;
        }

        this.cells = (int[,])cells.Clone();
    }

    public int TileAt(int x, int y)
    {
        if (x < 0 || x >= Width) throw new ArgumentOutOfRangeException(nameof(x));
        if (y < 0 || y >= Height) throw new ArgumentOutOfRangeException(nameof(y));
        return cells[y, x];
    }

    public bool TryFindTile(int tileId, out int x, out int y)
    {
        for (int row = 0; row < Height; row++)
        for (int column = 0; column < Width; column++)
            if (cells[row, column] == tileId)
            {
                x = column;
                y = row;
                return true;
            }

        x = -1;
        y = -1;
        return false;
    }

    public static OperationBoardSnapshot FromSession(SlideScrambleSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return new OperationBoardSnapshot(session.Cells);
    }

    public static OperationBoardSnapshot FromGrid(Grid grid)
    {
        ArgumentNullException.ThrowIfNull(grid);
        var currentCells = new int[grid.Height, grid.Width];
        for (int y = 0; y < grid.Height; y++)
        for (int x = 0; x < grid.Width; x++)
        {
            var tile = grid.Tiles[y, x] ?? throw new InvalidOperationException("Grid contains a missing tile.");
            currentCells[y, x] = checked(tile.OriginY * grid.Width + tile.OriginX);
        }

        return new OperationBoardSnapshot(currentCells);
    }

    internal OperationBoardSnapshot Copy() => new(cells);
}

/// <summary>An immutable, operation-specific request. Implementations should be immutable records or value objects.</summary>
public interface IOperationIntent
{
    string OperationId { get; }
}

public sealed record SlideOperationIntent(RecordedMoveAxis Axis, int Index, int Delta) : IOperationIntent
{
    public string OperationId => PuzzleOperationIds.Slide;
}

/// <summary>Resolves an operation deterministically without applying it to the supplied board.</summary>
public interface IPuzzleOperation
{
    string Id { get; }
    OperationResolution Resolve(OperationBoardSnapshot board, IOperationIntent intent);
}

/// <summary>A deterministic expansion into existing row and column move primitives.</summary>
public sealed class OperationResolution
{
    private readonly ReadOnlyCollection<RecordedMove> expansion;

    public string OperationId { get; }
    public bool IsValid { get; }
    public IReadOnlyList<RecordedMove> Expansion => expansion;
    public string? Error { get; }

    private OperationResolution(string operationId, bool isValid, IEnumerable<RecordedMove> expansion, string? error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        OperationId = operationId;
        IsValid = isValid;
        var moves = expansion.ToArray();
        if (moves.Any(move => move == null))
            throw new ArgumentException("An operation expansion cannot contain a null move.", nameof(expansion));
        this.expansion = Array.AsReadOnly(moves);
        Error = error;
    }

    public static OperationResolution Success(string operationId, IEnumerable<RecordedMove> expansion)
    {
        ArgumentNullException.ThrowIfNull(expansion);
        return new OperationResolution(operationId, true, expansion, null);
    }

    public static OperationResolution Rejected(string operationId, string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        return new OperationResolution(operationId, false, Array.Empty<RecordedMove>(), error);
    }
}

public sealed class SlideOperation : IPuzzleOperation
{
    public string Id => PuzzleOperationIds.Slide;

    public OperationResolution Resolve(OperationBoardSnapshot board, IOperationIntent intent)
    {
        if (intent is not SlideOperationIntent slide)
            return OperationResolution.Rejected(Id, "Slide requires an axis, line index, and displacement.");
        if (!Enum.IsDefined(slide.Axis))
            return OperationResolution.Rejected(Id, "Slide axis is invalid.");

        int lineCount = slide.Axis == RecordedMoveAxis.Row ? board.Height : board.Width;
        int lineLength = slide.Axis == RecordedMoveAxis.Row ? board.Width : board.Height;
        if (slide.Index < 0 || slide.Index >= lineCount)
            return OperationResolution.Rejected(Id, "Slide line index is outside the board.");
        if (slide.Delta % lineLength == 0)
            return OperationResolution.Rejected(Id, "Slide displacement does not move any tiles.");

        return OperationResolution.Success(Id, new[] { new RecordedMove(slide.Axis, slide.Index, slide.Delta) });
    }
}

/// <summary>Stable operation identities and the operation implementations available to the evaluator.</summary>
public sealed class OperationRegistry
{
    private static readonly string[] ReservedIds =
    {
        PuzzleOperationIds.Slide,
        PuzzleOperationIds.Shuffle,
        PuzzleOperationIds.ColumnSwap,
        PuzzleOperationIds.RowSwap
    };

    private readonly IReadOnlyDictionary<string, IPuzzleOperation> operations;
    private readonly HashSet<string> knownOperationIds;
    private readonly ReadOnlyCollection<string> knownIds;

    public IReadOnlyCollection<string> KnownOperationIds => knownIds;

    public IReadOnlyCollection<string> RegisteredOperationIds =>
        Array.AsReadOnly(operations.Keys.Order(StringComparer.Ordinal).ToArray());

    public OperationRegistry(IEnumerable<IPuzzleOperation>? additionalOperations = null,
        IEnumerable<string>? additionalKnownIds = null)
    {
        var registered = new Dictionary<string, IPuzzleOperation>(StringComparer.Ordinal)
        {
            [PuzzleOperationIds.Slide] = new SlideOperation()
        };
        knownOperationIds = new HashSet<string>(ReservedIds, StringComparer.Ordinal);

        if (additionalOperations != null)
            foreach (var operation in additionalOperations)
            {
                ArgumentNullException.ThrowIfNull(operation);
                ArgumentException.ThrowIfNullOrWhiteSpace(operation.Id);
                if (!registered.TryAdd(operation.Id, operation))
                    throw new ArgumentException($"Operation ID '{operation.Id}' is already registered.",
                        nameof(additionalOperations));
                knownOperationIds.Add(operation.Id);
            }

        if (additionalKnownIds != null)
            foreach (string operationId in additionalKnownIds)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
                knownOperationIds.Add(operationId);
            }

        operations = new ReadOnlyDictionary<string, IPuzzleOperation>(registered);
        knownIds = Array.AsReadOnly(knownOperationIds.Order(StringComparer.Ordinal).ToArray());
    }

    public static OperationRegistry CreateDefault() => new();

    public bool IsKnownOperation(string operationId) => knownOperationIds.Contains(operationId);

    public bool TryGetOperation(string operationId, out IPuzzleOperation operation)
    {
        if (operations.TryGetValue(operationId, out var found))
        {
            operation = found;
            return true;
        }

        operation = null!;
        return false;
    }
}