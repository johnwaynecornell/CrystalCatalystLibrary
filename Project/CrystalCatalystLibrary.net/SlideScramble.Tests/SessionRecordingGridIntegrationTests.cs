using SlideScramble;
using Xunit;

namespace SlideScramble.Tests;

public class SessionRecordingGridIntegrationTests
{
    [Fact]
    public void ArbitraryLoadedBoardCanBecomeTheStartOfANewRecording()
    {
        var cells = new[,] { { 4, 0, 5 }, { 2, 1, 3 } };
        var source = new SessionRecording(cells, Array.Empty<RecordedMove>());
        var grid = new Grid(3, 2, 32);
        var controller = new SessionRecordingController(grid);

        controller.LoadPlayback(source);
        Assert.Equal(new[] { 4, 0, 5, 2, 1, 3 }, Capture(grid));
        Assert.False(grid.IsSolved());
        controller.StopPlayback();
        Assert.Equal(RecordingControllerMode.Idle, controller.Mode);
        controller.StartRecording();
        Assert.Equal(RecordingControllerMode.Recording, controller.Mode);
        Assert.True(controller.MoveRow(0, 1));
        var end = Capture(grid);
        var recorded = controller.StopRecording();

        Assert.Equal(new[] { 4, 0, 5, 2, 1, 3 }, Flatten(recorded.InitialCells));
        Assert.Equal(new RecordedMove(RecordedMoveAxis.Row, 0, 0, 1), Assert.Single(recorded.Moves));
        controller.LoadPlayback(recorded);
        Assert.Equal(new[] { 4, 0, 5, 2, 1, 3 }, Capture(grid));
        Assert.True(controller.Step());
        Assert.Equal(end, Capture(grid));
    }

    [Fact]
    public void ZeroMovePlaybackLoadsBoardAndCannotStepInEitherDirection()
    {
        var recording = new SessionRecording(new[,] { { 3, 1 }, { 0, 2 } }, Array.Empty<RecordedMove>());
        var grid = new Grid(2, 2, 32);
        var controller = new SessionRecordingController(grid);

        controller.LoadPlayback(recording);
        Assert.Equal(new[] { 3, 1, 0, 2 }, Capture(grid));
        Assert.Equal(RecordingControllerMode.Playback, controller.Mode);
        Assert.Equal(0, controller.Position);
        Assert.Equal(0, controller.MoveCount);
        Assert.False(controller.Step());
        Assert.False(controller.StepBack());
        controller.SetAutoplay(true);
        Assert.False(controller.IsAutoplaying);
        Assert.False(controller.AutoplayTick());
        Assert.Equal(new[] { 3, 1, 0, 2 }, Capture(grid));
    }

    [Fact]
    public void UnsolvedPlaybackNavigatesBothWaysWithoutChangingRecordingOrPreviewSource()
    {
        var recording = new SessionRecording(new[,] { { 4, 0, 5 }, { 2, 1, 3 } }, new[]
        {
            new RecordedMove(RecordedMoveAxis.Row, 1, 0, 1),
            new RecordedMove(RecordedMoveAxis.Column, 2, 1, -1)
        });
        var originalJson = recording.ToJson();
        var liveGrid = new Grid(3, 2, 32);
        liveGrid.MoveColumn(0, 1);
        var liveBefore = Capture(liveGrid);
        var previewGrid = new Grid(3, 2, 32);
        var preview = new SessionRecordingController(previewGrid);

        preview.LoadPlayback(recording);
        var initial = Capture(previewGrid);
        Assert.Equal(new[] { 4, 0, 5, 2, 1, 3 }, initial);
        Assert.False(previewGrid.IsSolved());
        Assert.NotSame(liveGrid.Tiles[0, 0], previewGrid.Tiles[0, 0]);
        Assert.False(preview.StepBack());
        Assert.True(preview.Step());
        var intermediate = Capture(previewGrid);
        Assert.Equal(new[] { 0, 5, 4, 2, 1, 3 }, intermediate);
        Assert.Equal(liveBefore, Capture(liveGrid));
        Assert.Equal(1, preview.Position);
        Assert.True(preview.Step());
        var end = Capture(previewGrid);
        Assert.Equal(new[] { 0, 5, 3, 2, 1, 4 }, end);
        Assert.Equal(2, preview.Position);
        Assert.False(preview.Step());
        Assert.True(preview.StepBack());
        Assert.Equal(intermediate, Capture(previewGrid));
        Assert.True(preview.StepBack());
        Assert.Equal(initial, Capture(previewGrid));
        Assert.Equal(0, preview.Position);
        Assert.False(preview.StepBack());
        Assert.Equal(originalJson, recording.ToJson());
        Assert.Equal(liveBefore, Capture(liveGrid));
    }

    private static int[] Capture(Grid grid)
    {
        var result = new List<int>();
        foreach (var tile in grid.Tiles) result.Add(tile.OriginY * grid.Width + tile.OriginX);
        return result.ToArray();
    }

    private static int[] Flatten(int[,] cells)
    {
        var result = new List<int>();
        foreach (var cell in cells) result.Add(cell);
        return result.ToArray();
    }
}