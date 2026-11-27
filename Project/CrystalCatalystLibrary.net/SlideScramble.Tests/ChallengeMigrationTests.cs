using System.Text.Json;
using SlideScramble;
using Xunit;

namespace SlideScramble.Tests;

public class ChallengeMigrationTests
{
    [Fact]
    public void AllLegacyChallengesMigrateToVersion4WithoutChangingTheirSemantics()
    {
        string baselineJson = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData",
            "challenges-pre-v4-baseline.json"));
        using var baselineDocument = JsonDocument.Parse(baselineJson);
        var originalEntries = baselineDocument.RootElement.EnumerateObject().ToArray();
        Assert.Equal(7, originalEntries.Count(entry => entry.Value.GetProperty("version").GetInt32() == 2));
        Assert.Equal(4, originalEntries.Count(entry => entry.Value.GetProperty("version").GetInt32() == 3));

        var baseline = ChallengeCatalog.FromJson(baselineJson);
        string migratedJson = File.ReadAllText(FindCatalogPath());
        using var migratedDocument = JsonDocument.Parse(migratedJson);
        var migratedEntries = migratedDocument.RootElement.EnumerateObject().ToArray();
        Assert.Equal(11, migratedEntries.Length);
        Assert.All(migratedEntries,
            entry => Assert.Equal(4, entry.Value.GetProperty("version").GetInt32()));
        var migrated = ChallengeCatalog.FromJson(migratedJson);
        Assert.Equal(11, baseline.Names.Count);
        Assert.Equal(baseline.Names.Order(StringComparer.Ordinal), migrated.Names.Order(StringComparer.Ordinal));
        var bundled = ChallengeCatalog.LoadBundled();
        Assert.Equal(migrated.Names.Order(StringComparer.Ordinal), bundled.Names.Order(StringComparer.Ordinal));

        foreach (string name in baseline.Names)
        {
            Assert.True(baseline.TryGet(name, out var original));
            Assert.True(migrated.TryGet(name, out var current));
            Assert.NotNull(original);
            Assert.NotNull(current);

            using var currentJson = JsonDocument.Parse(current!.ToJson());
            Assert.Equal(4, currentJson.RootElement.GetProperty("version").GetInt32());
            Assert.Equal(original!.ToJson(), current.ToJson());
            AssertEquivalent(original, current);
            AssertPlaybackAndReversal(original, current);
        }
    }

    private static string FindCatalogPath()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            string path = Path.Combine(directory.FullName, "SlideScramble", "challenges.json");
            if (File.Exists(path)) return path;
        }

        throw new FileNotFoundException(
            "Could not locate the SlideScramble challenge catalog from the test directory.");
    }

    private static void AssertEquivalent(SessionRecording expected, SessionRecording actual)
    {
        Assert.Equal(expected.Width, actual.Width);
        Assert.Equal(expected.Height, actual.Height);
        Assert.Equal(expected.InitialCells.Cast<int>(), actual.InitialCells.Cast<int>());
        Assert.Equal(expected.RuleSet.Name, actual.RuleSet.Name);
        Assert.Equal(expected.RuleSet.ScrambleAllowed, actual.RuleSet.ScrambleAllowed);
        Assert.Equal(expected.RuleSet.SolveAllowed, actual.RuleSet.SolveAllowed);
        Assert.Equal(expected.RuleSet.UIEnabled, actual.RuleSet.UIEnabled);
        Assert.Equal(expected.Operations.Count, actual.Operations.Count);
        Assert.Equal(expected.Moves, actual.Moves);
        Assert.Equal(expected.GetVerifiedGoal(), actual.GetVerifiedGoal());
        Assert.Equal(expected.GetVerifiedGoalMoveCost(), actual.GetVerifiedGoalMoveCost());

        for (int i = 0; i < expected.Operations.Count; i++)
        {
            var left = expected.Operations[i];
            var right = actual.Operations[i];
            Assert.Equal(left.OperationId, right.OperationId);
            Assert.Equal(left.Parameters.GetRawText(), right.Parameters.GetRawText());
            Assert.Equal(left.PrimitiveMoves, right.PrimitiveMoves);
            Assert.Equal(left.Purpose, right.Purpose);
            Assert.Equal(left.Accounting, right.Accounting);
            Assert.Equal(left.MoveCost, right.MoveCost);
            Assert.Equal(left.IsInverse, right.IsInverse);
        }

        for (int position = 0; position <= expected.Moves.Count; position++)
        {
            Assert.Equal(expected.IsOperationBoundary(position), actual.IsOperationBoundary(position));
            if (expected.IsOperationBoundary(position))
                Assert.Equal(expected.GetOperationCountAtBoundary(position),
                    actual.GetOperationCountAtBoundary(position));
        }
    }

    private static void AssertPlaybackAndReversal(SessionRecording expected, SessionRecording actual)
    {
        var expectedGrid = new Grid(expected.Width, expected.Height, 32);
        var expectedController = new SessionRecordingController(expectedGrid);
        expectedController.LoadPlayback(expected);
        while (expectedController.Step())
        {
        }

        var actualGrid = new Grid(actual.Width, actual.Height, 32);
        var actualController = new SessionRecordingController(actualGrid);
        actualController.LoadPlayback(actual);
        while (actualController.Step())
        {
        }

        int[] expectedFinal = Capture(expectedGrid);
        int[] actualFinal = Capture(actualGrid);
        Assert.Equal(expectedFinal, actualFinal);
        Assert.Equal(expectedGrid.IsSolved(), actualGrid.IsSolved());

        while (expectedController.StepBack())
        {
        }

        while (actualController.StepBack())
        {
        }

        Assert.Equal(expected.InitialCells.Cast<int>(), Capture(expectedGrid));
        Assert.Equal(expected.InitialCells.Cast<int>(), Capture(actualGrid));

        var reverseGrid = new Grid(actual.Width, actual.Height, 32);
        var reverseController = new SessionRecordingController(reverseGrid);
        reverseController.LoadPlayback(actual.Reverse());
        while (reverseController.Step())
        {
        }

        Assert.Equal(actual.InitialCells.Cast<int>(), Capture(reverseGrid));
    }

    private static int[] Capture(Grid grid) => grid.Tiles.Cast<Tile>()
        .Select(tile => tile.OriginY * grid.Width + tile.OriginX).ToArray();
}