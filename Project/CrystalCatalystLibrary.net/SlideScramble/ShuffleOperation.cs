using System.Text.Json;

namespace SlideScramble;

public enum PortableLensCell
{
    Circle,
    Star,
    Triangle,
    Square
}

public sealed record ShuffleOperationIntent(int AnchorX, int AnchorY, PortableLensCell StartCell,
    PortableLensCell TargetCell) : IOperationIntent
{
    public string OperationId => PuzzleOperationIds.Shuffle;

    public JsonElement ToRecordingParameters() => JsonSerializer.SerializeToElement(new
    {
        lensAnchor = new[] { AnchorX, AnchorY },
        startCell = StartCell.ToString(),
        targetCell = TargetCell.ToString()
    });
}

public enum LensOperationFamily
{
    Shuffle,
    ColumnSwap,
    RowSwap
}

/// <summary>Maps a completed neighboring-cell drag and its captured modifier to a logical family.</summary>
public static class LensGestureOperationMapper
{
    public static bool TrySelectFamily(PortableLensCell start, PortableLensCell target, bool control,
        out LensOperationFamily family)
    {
        family = default;
        if (!Enum.IsDefined(start) || !Enum.IsDefined(target) || start == target) return false;

        var (startX, startY) = Coordinates(start);
        var (targetX, targetY) = Coordinates(target);
        bool horizontal = startY == targetY && Math.Abs(startX - targetX) == 1;
        bool vertical = startX == targetX && Math.Abs(startY - targetY) == 1;
        if (!horizontal && !vertical) return false;

        family = !control
            ? LensOperationFamily.Shuffle
            : horizontal ? LensOperationFamily.ColumnSwap : LensOperationFamily.RowSwap;
        return true;
    }

    public static (int X, int Y) Coordinates(PortableLensCell cell) => cell switch
    {
        PortableLensCell.Circle => (0, 0),
        PortableLensCell.Star => (1, 0),
        PortableLensCell.Triangle => (0, 1),
        PortableLensCell.Square => (1, 1),
        _ => throw new ArgumentOutOfRangeException(nameof(cell))
    };
}

/// <summary>Resolves each directed lens edge through its verified four-rotation Shuffle Atlas entry.</summary>
public sealed class ShuffleOperation : IPuzzleOperation
{
    private static readonly IReadOnlyDictionary<(PortableLensCell Start, PortableLensCell Target), AtlasStep[]>
        GestureFormulas = new Dictionary<(PortableLensCell, PortableLensCell), AtlasStep[]>
        {
            [(PortableLensCell.Circle, PortableLensCell.Star)] = Formula(
                (PortableLensCell.Triangle, 'U'), (PortableLensCell.Star, 'L'),
                (PortableLensCell.Star, 'D'), (PortableLensCell.Triangle, 'R')),
            [(PortableLensCell.Star, PortableLensCell.Circle)] = Formula(
                (PortableLensCell.Star, 'L'), (PortableLensCell.Triangle, 'U'),
                (PortableLensCell.Triangle, 'R'), (PortableLensCell.Star, 'D')),
            [(PortableLensCell.Star, PortableLensCell.Square)] = Formula(
                (PortableLensCell.Circle, 'R'), (PortableLensCell.Square, 'U'),
                (PortableLensCell.Square, 'L'), (PortableLensCell.Circle, 'D')),
            [(PortableLensCell.Square, PortableLensCell.Star)] = Formula(
                (PortableLensCell.Square, 'U'), (PortableLensCell.Circle, 'R'),
                (PortableLensCell.Circle, 'D'), (PortableLensCell.Square, 'L')),
            [(PortableLensCell.Circle, PortableLensCell.Triangle)] = Formula(
                (PortableLensCell.Circle, 'D'), (PortableLensCell.Square, 'L'),
                (PortableLensCell.Square, 'U'), (PortableLensCell.Circle, 'R')),
            [(PortableLensCell.Triangle, PortableLensCell.Circle)] = Formula(
                (PortableLensCell.Square, 'L'), (PortableLensCell.Circle, 'D'),
                (PortableLensCell.Circle, 'R'), (PortableLensCell.Square, 'U')),
            [(PortableLensCell.Triangle, PortableLensCell.Square)] = Formula(
                (PortableLensCell.Triangle, 'R'), (PortableLensCell.Star, 'D'),
                (PortableLensCell.Star, 'L'), (PortableLensCell.Triangle, 'U')),
            [(PortableLensCell.Square, PortableLensCell.Triangle)] = Formula(
                (PortableLensCell.Star, 'D'), (PortableLensCell.Triangle, 'R'),
                (PortableLensCell.Triangle, 'U'), (PortableLensCell.Star, 'L'))
        };

    public string Id => PuzzleOperationIds.Shuffle;

    public OperationResolution Resolve(OperationBoardSnapshot board, IOperationIntent intent)
    {
        if (intent is not ShuffleOperationIntent shuffle)
            return OperationResolution.Rejected(Id, "Shuffle requires a lens anchor and directed neighboring cells.");
        if (board.Width < 3 || board.Height < 3)
            return OperationResolution.Rejected(Id, "Shuffle Atlas formulas require board dimensions of at least 3×3.");
        if (shuffle.AnchorX < 0 || shuffle.AnchorX >= board.Width || shuffle.AnchorY < 0 ||
            shuffle.AnchorY >= board.Height)
            return OperationResolution.Rejected(Id, "Lens anchor is outside the board.");
        if (!LensGestureOperationMapper.TrySelectFamily(shuffle.StartCell, shuffle.TargetCell, false, out _) ||
            !GestureFormulas.TryGetValue((shuffle.StartCell, shuffle.TargetCell), out var formula))
            return OperationResolution.Rejected(Id, "Shuffle gesture must connect neighboring lens cells.");

        int[,] cells = board.Cells;
        var trackedTiles = new int[4];
        foreach (PortableLensCell cell in Enum.GetValues<PortableLensCell>())
        {
            var (offsetX, offsetY) = LensGestureOperationMapper.Coordinates(cell);
            int x = (shuffle.AnchorX + offsetX) % board.Width;
            int y = (shuffle.AnchorY + offsetY) % board.Height;
            trackedTiles[(int)cell] = cells[y, x];
        }

        var expansion = new List<RecordedMove>(formula.Length);
        foreach (var step in formula)
        {
            (int x, int y) = FindTile(cells, trackedTiles[(int)step.Cell]);
            var move = ToRecordedMove(step.Direction, x, y);
            expansion.Add(move);
            if (move.Axis == RecordedMoveAxis.Row) LogicalMoves.MoveRow(cells, move.Index, move.Delta);
            else LogicalMoves.MoveColumn(cells, move.Index, move.Delta);
        }

        return OperationResolution.Success(Id, expansion);
    }

    private static AtlasStep[] Formula(params (PortableLensCell Cell, char Direction)[] steps) =>
        steps.Select(step => new AtlasStep(step.Cell, step.Direction)).ToArray();

    private static (int X, int Y) FindTile(int[,] cells, int tileId)
    {
        for (int y = 0; y < cells.GetLength(0); y++)
        for (int x = 0; x < cells.GetLength(1); x++)
            if (cells[y, x] == tileId)
                return (x, y);

        throw new InvalidOperationException("A tracked lens tile is missing from the board.");
    }

    private static RecordedMove ToRecordedMove(char direction, int x, int y) => direction switch
    {
        'L' => new RecordedMove(RecordedMoveAxis.Row, x, y, 1),
        'R' => new RecordedMove(RecordedMoveAxis.Row, x, y, -1),
        'U' => new RecordedMove(RecordedMoveAxis.Column, x, y, 1),
        'D' => new RecordedMove(RecordedMoveAxis.Column, x, y, -1),
        _ => throw new ArgumentOutOfRangeException(nameof(direction))
    };

    private sealed record AtlasStep(PortableLensCell Cell, char Direction);
}