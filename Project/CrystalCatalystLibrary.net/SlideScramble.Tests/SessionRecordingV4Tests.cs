using System.Text.Json;
using System.Text.Json.Nodes;
using SlideScramble;
using Xunit;

namespace SlideScramble.Tests;

public class SessionRecordingV4Tests
{
    private static string Version3 => JsonSerializer.Serialize(new
    {
        format = SessionRecording.Format,
        version = 3,
        width = 3,
        height = 3,
        initialCells = new[]
        {
            new[] { new[] { 1, 1 }, new[] { 2, 1 }, new[] { 3, 1 } },
            new[] { new[] { 1, 2 }, new[] { 2, 2 }, new[] { 3, 2 } },
            new[] { new[] { 1, 3 }, new[] { 2, 3 }, new[] { 3, 3 } }
        },
        ruleSet = new
        {
            name = "Historical",
            scrambleAllowed = new[] { PuzzleOperationIds.Slide, PuzzleOperationIds.Shuffle },
            solveAllowed = new[] { PuzzleOperationIds.Slide, PuzzleOperationIds.Shuffle },
            uiEnabled = new[] { PuzzleOperationIds.Slide, PuzzleOperationIds.Shuffle }
        },
        operations = new object[]
        {
            new
            {
                operationId = PuzzleOperationIds.Shuffle,
                parameters = new { lensAnchor = new[] { 2, 1 }, startCell = "Circle", targetCell = "Star" },
                purpose = "Solve",
                accounting = "LogicalOperation",
                moveCost = 1,
                isInverse = false,
                moves = new[] { new { axis = "Row", index = 1, dragStart = new[] { 1, 1 }, delta = -1 } }
            },
            new
            {
                operationId = PuzzleOperationIds.Slide,
                parameters = new { axis = "Column", index = 2, delta = -1 },
                purpose = "Solve",
                accounting = "LogicalOperation",
                moveCost = 1,
                isInverse = false,
                moves = new[] { new { axis = "Column", index = 3, dragStart = new[] { 3, 1 }, delta = -1 } }
            }
        }
    });

    [Fact]
    public void Version3OperationCoordinatesNormalizeAndExportAsVersion4()
    {
        var recording = SessionRecording.FromJson(Version3);

        Assert.Equal(new[] { 2, 1 }, recording.Operations[0].Parameters.GetProperty("lensAnchor").EnumerateArray()
            .Select(value => value.GetInt32()));
        Assert.Equal(2, recording.Operations[1].Parameters.GetProperty("index").GetInt32());
        Assert.Equal(-1, recording.Operations[1].Parameters.GetProperty("delta").GetInt32());

        string json = recording.ToJson();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.Equal(4, root.GetProperty("version").GetInt32());
        Assert.Equal(new[] { 3, 2 }, root.GetProperty("operations")[0].GetProperty("parameters")
            .GetProperty("lensAnchor").EnumerateArray().Select(value => value.GetInt32()));
        Assert.Equal("Circle", root.GetProperty("operations")[0].GetProperty("parameters")
            .GetProperty("startCell").GetString());
        Assert.Equal(3, root.GetProperty("operations")[1].GetProperty("parameters").GetProperty("index").GetInt32());
        Assert.Equal(-1, root.GetProperty("operations")[1].GetProperty("parameters").GetProperty("delta").GetInt32());

        var reloaded = SessionRecording.FromJson(json);
        Assert.Equal(json, reloaded.ToJson());
        AssertEquivalent(recording, reloaded);
    }

    [Theory]
    [InlineData(0, 0, 1, 1)]
    [InlineData(2, 2, 3, 3)]
    public void LensAnchorExportsOneBasedAtFirstAndWrappingEdges(int x, int y, int externalX, int externalY)
    {
        var recording = CreateLensRecording(x, y);
        string json = recording.ToJson();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var operation = Assert.Single(root.GetProperty("operations").EnumerateArray());
        var parameters = operation.GetProperty("parameters");

        Assert.Equal(new[] { externalX, externalY }, parameters.GetProperty("lensAnchor").EnumerateArray()
            .Select(value => value.GetInt32()));
        Assert.Equal(new[] { 1, 1 }, root.GetProperty("initialCells")[0][0].EnumerateArray()
            .Select(value => value.GetInt32()));
        Assert.Equal(new[] { x + 1, y + 1 }, operation.GetProperty("moves")[0].GetProperty("dragStart")
            .EnumerateArray().Select(value => value.GetInt32()));
        Assert.Equal(y + 1, operation.GetProperty("moves")[0].GetProperty("index").GetInt32());

        var reloaded = SessionRecording.FromJson(json);
        Assert.Equal(new[] { x, y }, reloaded.Operations[0].Parameters.GetProperty("lensAnchor")
            .EnumerateArray().Select(value => value.GetInt32()));
        Assert.Equal(json, reloaded.ToJson());
        if (x == 2 && y == 2)
        {
            var anchor = (x, y);
            Assert.Equal((0, 2), LensGestureOperationMapper.BoardPosition(PortableLensCell.Star, anchor, 3, 3));
            Assert.Equal((2, 0), LensGestureOperationMapper.BoardPosition(PortableLensCell.Triangle, anchor, 3, 3));
            Assert.Equal((0, 0), LensGestureOperationMapper.BoardPosition(PortableLensCell.Square, anchor, 3, 3));
        }
    }

    [Theory]
    [InlineData(RecordedMoveAxis.Row, 0, 1)]
    [InlineData(RecordedMoveAxis.Row, 2, 3)]
    [InlineData(RecordedMoveAxis.Column, 0, 1)]
    [InlineData(RecordedMoveAxis.Column, 3, 4)]
    public void RestrictedSlideAxesExportAndNormalizeFirstAndLastIndices(RecordedMoveAxis axis, int index,
        int externalIndex)
    {
        var recording = CreateSlideRecording(axis, index, 4, 3);
        string json = recording.ToJson();
        using var document = JsonDocument.Parse(json);
        var operation = Assert.Single(document.RootElement.GetProperty("operations").EnumerateArray());
        Assert.Equal(externalIndex, operation.GetProperty("parameters").GetProperty("index").GetInt32());
        Assert.Equal(externalIndex, operation.GetProperty("moves")[0].GetProperty("index").GetInt32());
        Assert.Equal(-1, operation.GetProperty("parameters").GetProperty("delta").GetInt32());

        var reloaded = SessionRecording.FromJson(json);
        Assert.Equal(index, reloaded.Operations[0].Parameters.GetProperty("index").GetInt32());
        Assert.Equal(recording.Moves, reloaded.Moves);
        Assert.Equal(recording.RuleSet.SolveAllowed, reloaded.RuleSet.SolveAllowed);
        Assert.Equal(json, reloaded.ToJson());
    }

    [Fact]
    public void RejectsZeroBasedSlideParametersInVersion4WithoutChangingPrimitiveCoordinates()
    {
        string json = CreateSlideRecording(RecordedMoveAxis.Row, 1, 4, 3).ToJson();

        Assert.Throws<JsonException>(() =>
            SessionRecording.FromJson(WithParameter(json, "index", JsonValue.Create(0))));
        Assert.Throws<JsonException>(() =>
            SessionRecording.FromJson(WithParameter(json, "index", JsonValue.Create(1))));
    }

    [Fact]
    public void RejectsZeroBasedLensAnchorInVersion4()
    {
        string json = CreateLensRecording(1, 1).ToJson();
        var invalidAnchor = new JsonArray(JsonValue.Create(0), JsonValue.Create(0));

        Assert.Throws<JsonException>(() => SessionRecording.FromJson(WithParameter(json, "lensAnchor", invalidAnchor)));
    }

    [Theory]
    [InlineData(2, 4, 41, "TwoWideSlide")]
    [InlineData(3, 3, 92, "ShuffleLogic")]
    [InlineData(4, 3, 100, "Hybrid")]
    public void SeededRuleAwareSimulationSurvivesVersion4RoundTrip(int width, int height, int seed,
        string ruleSetName)
    {
        var rules = ruleSetName switch
        {
            "TwoWideSlide" => PuzzleRuleSet.CreateTwoWideSlide(),
            "ShuffleLogic" => PuzzleRuleSet.CreateShuffleLogic(),
            "Hybrid" => PuzzleRuleSet.CreateHybrid(),
            _ => throw new ArgumentOutOfRangeException(nameof(ruleSetName), ruleSetName, "Unknown test preset")
        };
        var evaluator = new OperationRuleEvaluator(rules.Registry);
        var challenge = ShuffleChallengeFactory.GenerateRuleAware(width, height, 6, rules, evaluator, new Random(seed));
        var expected = challenge.Solution;

        string json = expected.ToJson();
        using var document = JsonDocument.Parse(json);
        Assert.Equal(4, document.RootElement.GetProperty("version").GetInt32());
        var restored = SessionRecording.FromJson(json);
        Assert.Equal(json, restored.ToJson());
        AssertEquivalent(expected, restored);
        Assert.NotNull(restored.GetVerifiedGoal());

        var grid = new Grid(width, height, 32);
        var controller = new SessionRecordingController(grid);
        controller.LoadPlayback(restored);
        while (controller.Step())
        {
        }

        Assert.True(grid.IsSolved());
        Assert.Equal(expected.Moves.Count, controller.MoveCount);

        while (controller.StepBack())
        {
        }

        Assert.Equal(restored.InitialCells.Cast<int>(), OperationBoardSnapshot.FromGrid(grid).Cells.Cast<int>());
    }

    private static SessionRecording CreateLensRecording(int x, int y)
    {
        var registry = OperationRegistry.CreateDefault();
        var rules = new PuzzleRuleSet("Lens", new[] { PuzzleOperationIds.Shuffle },
            new[] { PuzzleOperationIds.Shuffle }, new[] { PuzzleOperationIds.Shuffle }, registry);
        var parameters = JsonSerializer.SerializeToElement(new
        {
            lensAnchor = new[] { x, y },
            startCell = "Circle",
            targetCell = "Star"
        });
        var move = new RecordedMove(RecordedMoveAxis.Row, x, y, -1);
        var operation = new RecordedOperation(PuzzleOperationIds.Shuffle, parameters, new[] { move },
            OperationPlanPurpose.Solve, OperationMoveAccounting.LogicalOperation, 1);
        return new SessionRecording(Identity(3, 3), new[] { operation }, rules);
    }

    private static SessionRecording CreateSlideRecording(RecordedMoveAxis axis, int index, int width, int height)
    {
        var registry = OperationRegistry.CreateDefault();
        string allowed = axis == RecordedMoveAxis.Row ? PuzzleOperationIds.RowSlide : PuzzleOperationIds.ColumnSlide;
        var rules = new PuzzleRuleSet("Restricted", new[] { allowed }, new[] { allowed }, new[] { allowed }, registry);
        var move = axis == RecordedMoveAxis.Row
            ? new RecordedMove(axis, 0, index, -1)
            : new RecordedMove(axis, index, 0, -1);
        return new SessionRecording(Identity(width, height), new[] { RecordedOperation.CreateSlide(move) }, rules);
    }

    private static int[,] Identity(int width, int height)
    {
        var cells = new int[height, width];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
            cells[y, x] = y * width + x;
        return cells;
    }

    private static string WithParameter(string json, string name, JsonNode? value)
    {
        var root = JsonNode.Parse(json)!.AsObject();
        root["operations"]!.AsArray()[0]!["parameters"]!.AsObject()[name] = value;
        return root.ToJsonString();
    }

    private static void AssertEquivalent(SessionRecording expected, SessionRecording actual)
    {
        Assert.Equal(expected.InitialCells.Cast<int>(), actual.InitialCells.Cast<int>());
        Assert.Equal(expected.RuleSet.Name, actual.RuleSet.Name);
        Assert.Equal(expected.RuleSet.ScrambleAllowed, actual.RuleSet.ScrambleAllowed);
        Assert.Equal(expected.RuleSet.SolveAllowed, actual.RuleSet.SolveAllowed);
        Assert.Equal(expected.RuleSet.UIEnabled, actual.RuleSet.UIEnabled);
        Assert.Equal(expected.Operations.Count, actual.Operations.Count);
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
    }
}