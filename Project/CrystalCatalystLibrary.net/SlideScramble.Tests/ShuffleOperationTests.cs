using Xunit;

namespace SlideScramble.Tests;

public class ShuffleOperationTests
{
    [Theory]
    [InlineData(PortableLensCell.Circle, PortableLensCell.Star, 3, 0, 2, 1)]
    [InlineData(PortableLensCell.Star, PortableLensCell.Circle, 1, 2, 0, 3)]
    [InlineData(PortableLensCell.Star, PortableLensCell.Square, 0, 2, 3, 1)]
    [InlineData(PortableLensCell.Square, PortableLensCell.Star, 1, 3, 2, 0)]
    [InlineData(PortableLensCell.Circle, PortableLensCell.Triangle, 3, 1, 0, 2)]
    [InlineData(PortableLensCell.Triangle, PortableLensCell.Circle, 2, 0, 1, 3)]
    [InlineData(PortableLensCell.Triangle, PortableLensCell.Square, 0, 3, 1, 2)]
    [InlineData(PortableLensCell.Square, PortableLensCell.Triangle, 2, 1, 3, 0)]
    public void DirectedGestureMatchesItsVerifiedAtlasTransformationAtEveryLensPlacement(
        PortableLensCell start, PortableLensCell target, int expectedCircle, int expectedStar,
        int expectedTriangle, int expectedSquare)
    {
        var operation = new ShuffleOperation();
        foreach (var (width, height) in new[] { (3, 3), (4, 4), (5, 5), (5, 3), (7, 6) })
        for (int anchorY = 0; anchorY < height; anchorY++)
        for (int anchorX = 0; anchorX < width; anchorX++)
        {
            int[,] initial = Solved(width, height);
            var snapshot = new OperationBoardSnapshot(initial);
            int[,] snapshotBefore = snapshot.Cells;
            var intent = new ShuffleOperationIntent(anchorX, anchorY, start, target);
            var resolution = operation.Resolve(snapshot, intent);

            Assert.True(resolution.IsValid, resolution.Error);
            Assert.Equal(4, resolution.Expansion.Count);
            Apply(initial, resolution.Expansion);
            Assert.Equal(snapshotBefore, snapshot.Cells);

            int[] expected = { expectedCircle, expectedStar, expectedTriangle, expectedSquare };
            for (int cell = 0; cell < expected.Length; cell++)
            {
                var (offsetX, offsetY) = LensGestureOperationMapper.Coordinates((PortableLensCell)cell);
                int x = (anchorX + offsetX) % width;
                int y = (anchorY + offsetY) % height;
                int sourceX = (anchorX + (expected[cell] is 1 or 3 ? 1 : 0)) % width;
                int sourceY = (anchorY + (expected[cell] is 2 or 3 ? 1 : 0)) % height;
                Assert.Equal(snapshot.TileAt(sourceX, sourceY), initial[y, x]);
            }

            var lensCells = new HashSet<(int X, int Y)>();
            foreach (PortableLensCell cell in Enum.GetValues<PortableLensCell>())
            {
                var (offsetX, offsetY) = LensGestureOperationMapper.Coordinates(cell);
                lensCells.Add(((anchorX + offsetX) % width, (anchorY + offsetY) % height));
            }

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (!lensCells.Contains((x, y)))
                    Assert.Equal(snapshot.TileAt(x, y), initial[y, x]);
        }
    }

    [Theory]
    [InlineData(PortableLensCell.Circle, PortableLensCell.Star)]
    [InlineData(PortableLensCell.Star, PortableLensCell.Circle)]
    [InlineData(PortableLensCell.Star, PortableLensCell.Square)]
    [InlineData(PortableLensCell.Square, PortableLensCell.Star)]
    [InlineData(PortableLensCell.Circle, PortableLensCell.Triangle)]
    [InlineData(PortableLensCell.Triangle, PortableLensCell.Circle)]
    [InlineData(PortableLensCell.Triangle, PortableLensCell.Square)]
    [InlineData(PortableLensCell.Square, PortableLensCell.Triangle)]
    public void ShuffleHasPeriodThreeAndMappedGestureIsItsInverse(PortableLensCell start,
        PortableLensCell target)
    {
        const int width = 5;
        const int height = 3;
        const int anchorX = 4;
        const int anchorY = 2;
        int[,] original = Solved(width, height);
        int[,] repeated = (int[,])original.Clone();
        var operation = new ShuffleOperation();

        for (int i = 0; i < 3; i++)
            Apply(repeated, Resolve(operation, repeated, anchorX, anchorY, start, target));

        Assert.Equal(original, repeated);

        int[,] reversed = (int[,])original.Clone();
        Apply(reversed, Resolve(operation, reversed, anchorX, anchorY, start, target));
        var intent = new ShuffleOperationIntent(anchorX, anchorY, start, target);
        var inverse = ShuffleOperation.InvertGesture(intent);
        Assert.Equal(target, inverse.TargetCell);
        Assert.Equal(intent, ShuffleOperation.InvertGesture(inverse));
        Apply(reversed, Resolve(operation, reversed, inverse.AnchorX, inverse.AnchorY,
            inverse.StartCell, inverse.TargetCell));
        Assert.Equal(original, reversed);
    }

    [Theory]
    [InlineData(PortableLensCell.Circle, PortableLensCell.Star, false, LensOperationFamily.Shuffle)]
    [InlineData(PortableLensCell.Circle, PortableLensCell.Star, true, LensOperationFamily.ColumnSwap)]
    [InlineData(PortableLensCell.Star, PortableLensCell.Square, true, LensOperationFamily.RowSwap)]
    [InlineData(PortableLensCell.Circle, PortableLensCell.Triangle, true, LensOperationFamily.RowSwap)]
    public void ModifierSelectsFamilyWithoutChangingGestureGeometry(PortableLensCell start,
        PortableLensCell target, bool control, LensOperationFamily expected)
    {
        Assert.True(LensGestureOperationMapper.TrySelectFamily(start, target, control, out var actual));
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void InvalidGesturesAndUnsupportedBoardDimensionsAreRejected()
    {
        var operation = new ShuffleOperation();
        var board = new OperationBoardSnapshot(Solved(3, 3));

        Assert.False(operation.Resolve(board,
            new ShuffleOperationIntent(0, 0, PortableLensCell.Circle, PortableLensCell.Square)).IsValid);
        Assert.False(operation.Resolve(board,
            new ShuffleOperationIntent(3, 0, PortableLensCell.Circle, PortableLensCell.Star)).IsValid);
        Assert.False(operation.Resolve(board,
            new ShuffleOperationIntent(0, 0, (PortableLensCell)99, PortableLensCell.Star)).IsValid);
        Assert.False(LensGestureOperationMapper.TrySelectFamily(PortableLensCell.Circle,
            PortableLensCell.Square, false, out _));
        Assert.False(operation.Resolve(new OperationBoardSnapshot(Solved(2, 3)),
            new ShuffleOperationIntent(0, 0, PortableLensCell.Circle, PortableLensCell.Star)).IsValid);
    }

    private static IReadOnlyList<RecordedMove> Resolve(ShuffleOperation operation, int[,] cells,
        int anchorX, int anchorY, PortableLensCell start, PortableLensCell target)
    {
        var result = operation.Resolve(new OperationBoardSnapshot(cells),
            new ShuffleOperationIntent(anchorX, anchorY, start, target));
        Assert.True(result.IsValid, result.Error);
        Assert.Equal(4, result.Expansion.Count);
        return result.Expansion;
    }

    private static int[,] Solved(int width, int height)
    {
        var cells = new int[height, width];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
            cells[y, x] = y * width + x;
        return cells;
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