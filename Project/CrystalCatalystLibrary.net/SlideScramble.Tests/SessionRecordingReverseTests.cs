using SlideScramble;
using Xunit;

namespace SlideScramble.Tests;

public class SessionRecordingReverseTests
{
    [Fact]
    public void ReverseReplaysRectangularRecordingAndPreservesDragTile()
    {
        var source = new SessionRecording(new[,] { { 4, 0, 5 }, { 2, 1, 3 } }, new[]
        {
            new RecordedMove(RecordedMoveAxis.Row, 1, 0, 4),
            new RecordedMove(RecordedMoveAxis.Column, 2, 1, -3)
        });
        var original = source.ToJson();
        var reverse = source.Reverse();
        Assert.Equal(new[] { 0, 5, 3, 2, 1, 4 }, Flatten(reverse.InitialCells));
        Assert.Equal(new RecordedMove(RecordedMoveAxis.Column, 2, 0, 3), reverse.Moves[0]);
        Assert.Equal(new RecordedMove(RecordedMoveAxis.Row, 0, 0, -4), reverse.Moves[1]);
        var grid = new Grid(3, 2, 32);
        var controller = new SessionRecordingController(grid);
        controller.LoadPlayback(reverse);
        Assert.True(controller.Step());
        Assert.True(controller.Step());
        Assert.Equal(Flatten(source.InitialCells), Capture(grid));
        Assert.Equal(source.ToJson(), reverse.Reverse().ToJson());
        Assert.Equal(original, source.ToJson());
        Assert.Equal(reverse.ToJson(), SessionRecording.FromJson(reverse.ToJson()).ToJson());
    }

    [Fact]
    public void EmptyRecordingIsCopied()
    {
        var source = new SessionRecording(new[,] { { 1, 0 } }, Array.Empty<RecordedMove>());
        var reverse = source.Reverse();
        Assert.NotSame(source, reverse);
        Assert.Empty(reverse.Moves);
        reverse.InitialCells[0, 0] = 0;
        Assert.Equal(1, source.InitialCells[0, 0]);
        Assert.Equal(1, reverse.InitialCells[0, 0]);
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    [InlineData(-7)]
    [InlineData(7)]
    public void ExtremeAndWrappedDeltasReverseSafely(int delta)
    {
        var source = new SessionRecording(new[,] { { 0, 1, 2 } }, new[] { new RecordedMove(RecordedMoveAxis.Row, 2, 0, delta) });
        var reverse = source.Reverse();
        var grid = new Grid(3, 1, 32);
        var controller = new SessionRecordingController(grid);
        controller.LoadPlayback(reverse);
        int destination = (int)(((long)2 - delta % 3 + 3) % 3);
        Assert.Equal(destination, reverse.Moves[0].StartX);
        Assert.Equal(-(delta % 3), reverse.Moves[0].Delta % 3);
        Assert.True(controller.Step());
        Assert.Equal(Flatten(source.InitialCells), Capture(grid));
        Assert.Equal(Flatten(source.InitialCells), Flatten(reverse.Reverse().InitialCells));
    }

    [Fact]
    public void MinimumColumnDeltaReversesFromDestination()
    {
        var source = new SessionRecording(new[,] { { 0, 1 }, { 2, 3 }, { 4, 5 } },
            new[] { new RecordedMove(RecordedMoveAxis.Column, 1, 2, int.MinValue) });
        var reverse = source.Reverse();
        Assert.Equal(new RecordedMove(RecordedMoveAxis.Column, 1, 1, 2), Assert.Single(reverse.Moves));
        var grid = new Grid(2, 3, 32);
        var controller = new SessionRecordingController(grid);
        controller.LoadPlayback(reverse);
        Assert.True(controller.Step());
        Assert.Equal(Flatten(source.InitialCells), Capture(grid));
    }

    private static int[] Flatten(int[,] cells) => cells.Cast<int>().ToArray();
    private static int[] Capture(Grid grid) => grid.Tiles.Cast<Tile>().Select(t => t.OriginY * grid.Width + t.OriginX).ToArray();
}