using Xunit;

namespace SlideScramble.Tests;

public class PortableLensSwapTests
{
    [Theory]
    [InlineData(true, 1, OperationMoveAccounting.LogicalOperation)]
    [InlineData(false, 8, OperationMoveAccounting.PrimitiveMoves)]
    public void SwapRecordingRoundTripsAndContinuesAtOperationBoundary(bool direct, int cost,
        OperationMoveAccounting accounting)
    {
        var registry = OperationRegistry.CreateDefault();
        var rules = new PuzzleRuleSet("Swap", new[] { PuzzleOperationIds.Slide },
            new[] { direct ? PuzzleOperationIds.ColumnSwap : PuzzleOperationIds.Slide },
            new[] { PuzzleOperationIds.ColumnSwap }, registry);
        var grid = new Grid(4, 4);
        var controller = new SessionRecordingController(grid, rules);
        controller.StartRecording();
        var intent = new PortableLensSwapIntent(PuzzleOperationIds.ColumnSwap, 3, 3,
            PortableLensCell.Circle, PortableLensCell.Star, "UpLeft");
        var evaluation = new OperationRuleEvaluator(registry).EvaluateUiRequest(
            OperationBoardSnapshot.FromGrid(grid), intent, rules, controller.RuntimeUIEnabled);
        Assert.True(evaluation.IsAccepted, evaluation.RejectionReason);
        Assert.Equal(cost, evaluation.Plan!.MoveCost);
        Assert.Equal(accounting, evaluation.Plan.Accounting);
        controller.ApplyOperation(evaluation.Plan, intent.ToRecordingParameters());
        int[,] final = OperationBoardSnapshot.FromGrid(grid).Cells;
        var saved = SessionRecording.FromJson(controller.StopRecording().ToJson());
        Assert.Equal(8, saved.Moves.Count);
        Assert.Equal(cost, Assert.Single(saved.Operations).MoveCost);

        var replayGrid = new Grid(4, 4);
        var replay = new SessionRecordingController(replayGrid);
        replay.LoadPlayback(saved);
        Assert.True(replay.Step());
        Assert.Throws<InvalidOperationException>(() => replay.ContinueRecordingFromHere());
        while (replay.Step()) { }
        Assert.Equal(final, OperationBoardSnapshot.FromGrid(replayGrid).Cells);
        Assert.Equal(8, replay.ContinueRecordingFromHere());
        Assert.Equal(saved.ToJson(), replay.StopRecording().ToJson());
        Assert.Equal(saved.ToJson(), saved.Reverse().Reverse().ToJson());
    }
    [Theory]
    [InlineData(PuzzleOperationIds.ColumnSwap, "UpLeft", PortableLensCell.Circle, PortableLensCell.Star)]
    [InlineData(PuzzleOperationIds.ColumnSwap, "UpRight", PortableLensCell.Star, PortableLensCell.Circle)]
    [InlineData(PuzzleOperationIds.ColumnSwap, "DownLeft", PortableLensCell.Triangle, PortableLensCell.Square)]
    [InlineData(PuzzleOperationIds.ColumnSwap, "DownRight", PortableLensCell.Square, PortableLensCell.Triangle)]
    [InlineData(PuzzleOperationIds.RowSwap, "LeftUp", PortableLensCell.Circle, PortableLensCell.Triangle)]
    [InlineData(PuzzleOperationIds.RowSwap, "LeftDown", PortableLensCell.Triangle, PortableLensCell.Circle)]
    [InlineData(PuzzleOperationIds.RowSwap, "RightUp", PortableLensCell.Star, PortableLensCell.Square)]
    [InlineData(PuzzleOperationIds.RowSwap, "RightDown", PortableLensCell.Square, PortableLensCell.Star)]
    public void EveryVerifiedSwapVariantProducesItsLocalTransformationAtEveryPlacement(string operationId,
        string variant, PortableLensCell start, PortableLensCell target)
    {
        var operation = new PortableLensSwapOperation(operationId);
        foreach ((int width, int height) in new[] { (3, 3), (4, 4), (5, 5), (5, 3), (7, 6) })
        for (int anchorY = 0; anchorY < height; anchorY++)
        for (int anchorX = 0; anchorX < width; anchorX++)
        {
            int[,] initial = Solved(width, height);
            var snapshot = new OperationBoardSnapshot(initial);
            var intent = new PortableLensSwapIntent(operationId, anchorX, anchorY, start, target, variant);
            OperationResolution resolution = operation.Resolve(snapshot, intent);

            Assert.True(resolution.IsValid, resolution.Error);
            Assert.Equal(8, resolution.Expansion.Count);
            Apply(initial, resolution.Expansion);

            int[] expected = operationId == PuzzleOperationIds.ColumnSwap
                ? new[] { 1, 0, 3, 2 }
                : new[] { 2, 3, 0, 1 };
            var lensCells = new HashSet<(int X, int Y)>();
            foreach (PortableLensCell cell in Enum.GetValues<PortableLensCell>())
            {
                (int offsetX, int offsetY) = LensGestureOperationMapper.Coordinates(cell);
                int x = (anchorX + offsetX) % width;
                int y = (anchorY + offsetY) % height;
                int sourceX = (anchorX + (expected[(int)cell] is 1 or 3 ? 1 : 0)) % width;
                int sourceY = (anchorY + (expected[(int)cell] is 2 or 3 ? 1 : 0)) % height;
                lensCells.Add((x, y));
                Assert.Equal(snapshot.TileAt(sourceX, sourceY), initial[y, x]);
            }

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (!lensCells.Contains((x, y)))
                    Assert.Equal(snapshot.TileAt(x, y), initial[y, x]);
        }
    }

    [Theory]
    [InlineData(PuzzleOperationIds.ColumnSwap, "UpLeft", PortableLensCell.Circle, PortableLensCell.Star)]
    [InlineData(PuzzleOperationIds.ColumnSwap, "UpRight", PortableLensCell.Star, PortableLensCell.Circle)]
    [InlineData(PuzzleOperationIds.ColumnSwap, "DownLeft", PortableLensCell.Triangle, PortableLensCell.Square)]
    [InlineData(PuzzleOperationIds.ColumnSwap, "DownRight", PortableLensCell.Square, PortableLensCell.Triangle)]
    [InlineData(PuzzleOperationIds.RowSwap, "LeftUp", PortableLensCell.Circle, PortableLensCell.Triangle)]
    [InlineData(PuzzleOperationIds.RowSwap, "LeftDown", PortableLensCell.Triangle, PortableLensCell.Circle)]
    [InlineData(PuzzleOperationIds.RowSwap, "RightUp", PortableLensCell.Star, PortableLensCell.Square)]
    [InlineData(PuzzleOperationIds.RowSwap, "RightDown", PortableLensCell.Square, PortableLensCell.Star)]
    public void EverySwapVariantIsAnInvolution(string operationId, string variant, PortableLensCell start,
        PortableLensCell target)
    {
        int[,] cells = Solved(5, 3);
        int[,] original = (int[,])cells.Clone();
        var operation = new PortableLensSwapOperation(operationId);
        var intent = new PortableLensSwapIntent(operationId, 4, 2, start, target, variant);

        for (int i = 0; i < 2; i++)
        {
            OperationResolution resolution = operation.Resolve(new OperationBoardSnapshot(cells), intent);
            Assert.True(resolution.IsValid, resolution.Error);
            Apply(cells, resolution.Expansion);
        }

        Assert.Equal(original, cells);
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
        foreach (RecordedMove move in moves)
        {
            int length = move.Axis == RecordedMoveAxis.Row ? cells.GetLength(1) : cells.GetLength(0);
            int[] line = new int[length];
            for (int i = 0; i < length; i++)
                line[i] = move.Axis == RecordedMoveAxis.Row ? cells[move.Index, i] : cells[i, move.Index];
            int shift = (move.Delta % length + length) % length;
            for (int i = 0; i < length; i++)
            {
                int value = line[(i + shift) % length];
                if (move.Axis == RecordedMoveAxis.Row) cells[move.Index, i] = value;
                else cells[i, move.Index] = value;
            }
        }
    }
}
