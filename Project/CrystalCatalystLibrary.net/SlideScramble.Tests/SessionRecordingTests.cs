using System.Text.Json;
using SlideScramble;
using Xunit;

namespace SlideScramble.Tests;

public class SessionRecordingTests
{
    [Fact]
    public void StoppedRecordingCanBeNavigatedWithoutLoading()
    {
        var grid = new Grid(3, 2, 32);
        var initial = Capture(grid);
        var controller = new SessionRecordingController(grid);
        controller.StartRecording();
        controller.MoveRow(0, 1);
        var end = Capture(grid);
        var recording = controller.StopRecording();
        Assert.Same(recording, controller.Playback);
        Assert.Equal(1, controller.Position);
        Assert.Equal(end, Capture(grid));
        controller.ResumePlayback();
        Assert.True(controller.StepBack());
        Assert.Equal(initial, Capture(grid));
        Assert.True(controller.Step());
        Assert.Equal(end, Capture(grid));
    }

    [Fact]
    public void JsonContainsVersionedCoordinatesAndDragStart()
    {
        var controller = new SessionRecordingController(new Grid(3, 2, 32));
        controller.StartRecording();
        controller.MoveRow(2, 1, 1);
        var recording = controller.StopRecording();
        using var json = JsonDocument.Parse(recording.ToJson());
        var root = json.RootElement;
        Assert.Equal("SlideScramble", root.GetProperty("format").GetString());
        Assert.Equal(5, root.GetProperty("version").GetInt32());
        Assert.Equal(3, root.GetProperty("width").GetInt32());
        Assert.Equal(2, root.GetProperty("height").GetInt32());
        Assert.Equal(new[] { 3, 2 }, root.GetProperty("initialCells")[1][2].EnumerateArray().Select(e => e.GetInt32()));
        Assert.Equal(new[] { 3, 2 },
            root.GetProperty("operations")[0].GetProperty("moves")[0].GetProperty("dragStart").EnumerateArray()
                .Select(e => e.GetInt32()));
        Assert.Equal(2, root.GetProperty("operations")[0].GetProperty("moves")[0].GetProperty("index").GetInt32());
        var roundTrip = SessionRecording.FromJson(recording.ToJson());
        Assert.Equal(2, roundTrip.Moves[0].StartX);
        Assert.Equal(1, roundTrip.Moves[0].StartY);
    }

    [Fact]
    public void LegacyRecordingLoadsAndResavesInVersion5()
    {
        const string legacy =
            "{\"format\":\"SlideScramble\",\"version\":1,\"width\":2,\"height\":2,\"initialCells\":[[[1,1],[0,0]],[[1,0],[0,1]]],\"moves\":[{\"axis\":\"Column\",\"index\":1,\"dragStart\":[1,0],\"delta\":-1}]}";
        var recording = SessionRecording.FromJson(legacy);
        Assert.Equal(3, recording.InitialCells[0, 0]);
        Assert.Equal(new RecordedMove(RecordedMoveAxis.Column, 1, 0, -1), recording.Moves[0]);
        using var json = JsonDocument.Parse(recording.ToJson());
        Assert.Equal(5, json.RootElement.GetProperty("version").GetInt32());
        Assert.Equal(new[] { 2, 2 },
            json.RootElement.GetProperty("initialCells")[0][0].EnumerateArray().Select(e => e.GetInt32()));
        var move = json.RootElement.GetProperty("operations")[0].GetProperty("moves")[0];
        Assert.Equal(2, move.GetProperty("index").GetInt32());
        Assert.Equal(new[] { 2, 1 }, move.GetProperty("dragStart").EnumerateArray().Select(e => e.GetInt32()));
        Assert.Equal(-1, move.GetProperty("delta").GetInt32());
        Assert.Equal(recording.Moves[0], SessionRecording.FromJson(recording.ToJson()).Moves[0]);
    }

    [Theory]
    [InlineData("[[[0,1],[2,1]]]", "[]")]
    [InlineData("[[[1,1],[3,1]]]", "[]")]
    [InlineData("[[[1,1],[2,1]]]", "[{\"axis\":\"Row\",\"index\":1,\"dragStart\":[0,1],\"delta\":1}]")]
    [InlineData("[[[1,1],[2,1]]]", "[{\"axis\":\"Row\",\"index\":0,\"dragStart\":[1,1],\"delta\":1}]")]
    [InlineData("[[[1,1],[2,1]]]", "[{\"axis\":\"Column\",\"index\":2,\"dragStart\":[2,2],\"delta\":1}]")]
    public void RejectsInvalidVersion2Coordinates(string cells, string moves)
    {
        string json =
            $"{{\"format\":\"SlideScramble\",\"version\":2,\"width\":2,\"height\":1,\"initialCells\":{cells},\"moves\":{moves}}}";
        Assert.Throws<JsonException>(() => SessionRecording.FromJson(json));
    }

    [Theory]
    [InlineData("Other", 1, 2, 1, "[[[0,0],[1,0]]]", "[]")]
    [InlineData("SlideScramble", 3, 2, 1, "[[[0,0],[1,0]]]", "[]")]
    [InlineData("SlideScramble", 1, 3, 1, "[[[0,0],[1,0]]]", "[]")]
    [InlineData("SlideScramble", 1, 2, 1, "[[[0,0],[0,0]]]", "[]")]
    [InlineData("SlideScramble", 1, 2, 1, "[[[0,0],[2,0]]]", "[]")]
    [InlineData("SlideScramble", 1, 2, 1, "[[[0,0],[1,0]]]",
        "[{\"axis\":\"Row\",\"index\":0,\"dragStart\":[2,0],\"delta\":1}]")]
    [InlineData("SlideScramble", 1, 2, 1, "[[[0,0],[1,0]]]",
        "[{\"axis\":\"Row\",\"index\":0,\"dragStart\":[0],\"delta\":1}]")]
    [InlineData("SlideScramble", 1, 2, 1, "[[[0,0],[1,0]]]",
        "[{\"axis\":\"Row\",\"index\":0,\"dragStart\":[0,0],\"delta\":2}]")]
    [InlineData("SlideScramble", 1, 2, 1, "[[[0,0],[1,0]]]",
        "[{\"axis\":3,\"index\":0,\"dragStart\":[0,0],\"delta\":1}]")]
    [InlineData("SlideScramble", 1, 2, 1, "[[[0,0],[1,0]]]", "[{\"axis\":\"Row\",\"index\":0,\"dragStart\":[0,0]}]")]
    [InlineData("SlideScramble", 1, 2, 1, "[[[0,0],[1,0]]]",
        "[{\"axis\":\"Row\",\"index\":1,\"dragStart\":[0,0],\"delta\":1}]")]
    public void RejectsInvalidVersionedData(string format, int version, int width, int height, string cells,
        string moves)
    {
        string json =
            $"{{\"format\":\"{format}\",\"version\":{version},\"width\":{width},\"height\":{height},\"initialCells\":{cells},\"moves\":{moves}}}";
        Assert.Throws<JsonException>(() => SessionRecording.FromJson(json));
    }

    [Fact]
    public void ReverseAndStoppedPlaybackRetainPositionAndSession()
    {
        var grid = new Grid(3, 2, 32);
        var controller = new SessionRecordingController(grid);
        controller.StartRecording();
        controller.MoveRow(2, 1, int.MinValue);
        controller.MoveColumn(1, 1, 1);
        var end = Capture(grid);
        var recording = controller.StopRecording();
        controller.LoadPlayback(recording);
        var start = Capture(grid);
        Assert.False(controller.StepBack());
        Assert.True(controller.Step());
        Assert.True(controller.Step());
        Assert.Equal(end, Capture(grid));
        Assert.True(controller.StepBack());
        controller.StopPlayback();
        Assert.Same(recording, controller.Playback);
        Assert.Equal(1, controller.Position);
        controller.MoveRow(0, 1);
        controller.ResumePlayback();
        Assert.True(controller.StepBack());
        Assert.Equal(start, Capture(grid));
    }

    [Fact]
    public void RecordingRoundTripsAndPlaybackRestoresGrid()
    {
        var grid = new Grid(3, 2, 32);
        grid.MoveColumn(2, 1);
        var controller = new SessionRecordingController(grid);
        controller.StartRecording();
        Assert.False(controller.MoveRow(0, 3));
        controller.MoveRow(0, 1);
        controller.MoveColumn(1, -1);
        var expected = Capture(grid);
        var recording = SessionRecording.FromJson(controller.StopRecording().ToJson());
        Assert.Equal(2, recording.Moves.Count);
        recording.InitialCells[0, 0] = 100;
        controller.LoadPlayback(recording);
        Assert.Equal(5, grid.Tiles[0, 2].OriginY * 3 + grid.Tiles[0, 2].OriginX);
        Assert.True(controller.Step());
        controller.SetAutoplay(true);
        Assert.True(controller.AutoplayTick());
        Assert.False(controller.IsAutoplaying);
        Assert.False(controller.Step());
        Assert.Equal(expected, Capture(grid));
        controller.ResetPlayback();
        Assert.Equal(0, controller.Position);
        controller.SetAutoplay(true);
        Assert.True(controller.AutoplayTick());
        controller.SetAutoplay(false);
        Assert.False(controller.AutoplayTick());
        controller.StopPlayback();
        Assert.Equal(RecordingControllerMode.Idle, controller.Mode);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"initialCells\":[[0,1],[2]],\"moves\":[]}")]
    [InlineData("{\"initialCells\":[[0,0]],\"moves\":[]}")]
    [InlineData("{\"initialCells\":[[0,1]],\"moves\":[{\"axis\":\"Row\",\"delta\":1}]}")]
    [InlineData("{\"initialCells\":[[0,1]],\"moves\":[{\"axis\":\"Row\",\"index\":1,\"delta\":1}]}")]
    [InlineData("{\"initialCells\":[[0,1]],\"moves\":[{\"axis\":\"Row\",\"index\":0,\"delta\":2}]}")]
    [InlineData("{\"initialCells\":[[0,1]],\"moves\":[{\"axis\":3,\"index\":0,\"delta\":1}]}")]
    [InlineData("{\"initialCells\":[[0,1]],\"moves\":[],\"extra\":true}")]
    public void InvalidJsonIsRejected(string json) =>
        Assert.Throws<JsonException>(() => SessionRecording.FromJson(json));

    [Fact]
    public void InvalidTransitionsAndDimensionsAreRejected()
    {
        var grid = new Grid(2, 2, 32);
        var controller = new SessionRecordingController(grid);
        Assert.Throws<InvalidOperationException>(() => controller.Step());
        controller.StartRecording();
        Assert.Throws<InvalidOperationException>(() => controller.StartRecording());
        Assert.Throws<ArgumentOutOfRangeException>(() => controller.MoveColumn(2, 1));
        Assert.Throws<ArgumentException>(() => controller.MoveRow(0, 1, 0, 1));
        Assert.Throws<ArgumentException>(() => controller.MoveColumn(0, 1, 2, 0));
        var recording = controller.StopRecording();
        Assert.Throws<ArgumentException>(() =>
            new SessionRecordingController(new Grid(3, 2, 32)).LoadPlayback(recording));
        controller.LoadPlayback(recording);
        controller.SetAutoplay(true);
        Assert.False(controller.IsAutoplaying);
        Assert.Throws<InvalidOperationException>(() => controller.MoveRow(0, 1));
    }

    private static int[] Capture(Grid grid)
    {
        var result = new List<int>();
        foreach (var tile in grid.Tiles) result.Add(tile.OriginY * grid.Width + tile.OriginX);
        return result.ToArray();
    }
}