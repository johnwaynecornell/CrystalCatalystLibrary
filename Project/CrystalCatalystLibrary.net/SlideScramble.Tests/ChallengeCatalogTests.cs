using SlideScramble;
using Xunit;

namespace SlideScramble.Tests;

public class ChallengeCatalogTests
{
    private const string Legacy = "{\"format\":\"SlideScramble\",\"version\":1,\"width\":2,\"height\":1,\"initialCells\":[[[1,0],[0,0]]],\"moves\":[]}";
    private const string Current = "{\"format\":\"SlideScramble\",\"version\":2,\"width\":2,\"height\":1,\"initialCells\":[[[2,1],[1,1]]],\"moves\":[]}";

    [Fact]
    public void LoadsBothRecordingVersionsAndSkipsInvalidEntries()
    {
        var catalog = ChallengeCatalog.FromJson($"{{\"old\":{Legacy},\"bad\":{{}},\"new\":{Current},\"null\":null,\" \":{Current},\"old\":{Current}}}");
        Assert.Equal(new[] { "old", "new" }, catalog.Names);
        Assert.True(catalog.TryGet("old", out var old));
        Assert.True(catalog.TryGet("new", out var current));
        Assert.Empty(old!.Moves);
        Assert.Equal(old.InitialCells, current!.InitialCells);
        Assert.False(catalog.TryGet("bad", out _));
        Assert.False(catalog.TryGet("Old", out _));
    }

    [Fact]
    public void OmittedMovesAreOptionalOnlyForChallenges()
    {
        string withoutMoves = Current.Replace(",\"moves\":[]", "");
        var catalog = ChallengeCatalog.FromJson($"{{\"optional\":{withoutMoves},\"invalid\":{Current.Replace("\"moves\":[]", "\"moves\":null")}}}");
        Assert.True(catalog.TryGet("optional", out var recording));
        Assert.Empty(recording!.Moves);
        Assert.False(catalog.TryGet("invalid", out _));
        Assert.Throws<System.Text.Json.JsonException>(() => SessionRecording.FromJson(withoutMoves));
    }

    [Theory]
    [InlineData("{")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("[]")]
    public void MalformedCatalogIsEmpty(string json) => Assert.Empty(ChallengeCatalog.FromJson(json).Names);

    [Fact]
    public void MissingFileIsEmpty() => Assert.Empty(ChallengeCatalog.Load(Path.Combine(AppContext.BaseDirectory, Guid.NewGuid().ToString(), "challenges.json")).Names);

    [Fact]
    public void BundledSwapIsAvailableWithoutMoves()
    {
        var catalog = ChallengeCatalog.LoadBundled();
        Assert.True(catalog.TryGet("Two-tile swap", out var recording));
        Assert.Empty(recording!.Moves);
        Assert.Equal(new[,] { { 0, 1, 2, 3 }, { 4, 5, 6, 7 }, { 8, 9, 10, 15 }, { 12, 13, 14, 11 } }, recording.InitialCells);
    }
}