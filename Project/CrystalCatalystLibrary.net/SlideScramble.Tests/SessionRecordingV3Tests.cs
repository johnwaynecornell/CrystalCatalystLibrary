using System.Text.Json;
using SlideScramble;
using Xunit;

namespace SlideScramble.Tests;

public class SessionRecordingV3Tests
{
    [Fact]
    public void Version2RecordingLoadsWithClassicSlideRulesAndOneLogicalMovePerSlide()
    {
        const string version2 = "{\"format\":\"SlideScramble\",\"version\":2,\"width\":3,\"height\":1,\"initialCells\":[[[1,1],[2,1],[3,1]]],\"moves\":[{\"axis\":\"Row\",\"index\":1,\"dragStart\":[2,1],\"delta\":-1}]}";

        var recording = SessionRecording.FromJson(version2);

        Assert.Equal(new[] { PuzzleOperationIds.Slide }, recording.RuleSet.ScrambleAllowed);
        Assert.Equal(new[] { PuzzleOperationIds.Slide }, recording.RuleSet.SolveAllowed);
        Assert.Equal(new[] { PuzzleOperationIds.Slide }, recording.RuleSet.UIEnabled);
        var operation = Assert.Single(recording.Operations);
        Assert.Equal(PuzzleOperationIds.Slide, operation.OperationId);
        Assert.Equal(OperationMoveAccounting.LogicalOperation, operation.Accounting);
        Assert.Equal(1, operation.MoveCost);
        Assert.Equal(new RecordedMove(RecordedMoveAxis.Row, 1, 0, -1), Assert.Single(recording.Moves));
    }

    [Fact]
    public void Version3RoundTripPreservesRuleSnapshotOperationIntentAndPrimitiveMoves()
    {
        var registry = OperationRegistry.CreateDefault();
        var rules = new PuzzleRuleSet("Lens snapshot", new[] { PuzzleOperationIds.Slide },
            new[] { PuzzleOperationIds.Shuffle }, new[] { PuzzleOperationIds.Slide, PuzzleOperationIds.Shuffle }, registry);
        var operation = ShuffleOperation(new[]
        {
            new RecordedMove(RecordedMoveAxis.Row, 0, 0, 1),
            new RecordedMove(RecordedMoveAxis.Column, 1, 1, -1)
        }, OperationMoveAccounting.LogicalOperation, 1);
        var recording = new SessionRecording(new[,] { { 0, 1 }, { 2, 3 } }, new[] { operation }, rules);

        string json = recording.ToJson();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.Equal(3, root.GetProperty("version").GetInt32());
        Assert.False(root.TryGetProperty("moves", out _));
        Assert.Equal("Lens snapshot", root.GetProperty("ruleSet").GetProperty("name").GetString());
        Assert.Equal(new[] { "Shuffle" }, root.GetProperty("ruleSet").GetProperty("solveAllowed").EnumerateArray().Select(x => x.GetString()));
        Assert.Equal(2, root.GetProperty("operations")[0].GetProperty("moves").GetArrayLength());

        var roundTrip = SessionRecording.FromJson(json);
        Assert.Equal(json, roundTrip.ToJson());
        Assert.Equal(recording.Moves, roundTrip.Moves);
        Assert.Equal(recording.RuleSet.SolveAllowed, roundTrip.RuleSet.SolveAllowed);
        Assert.Equal(operation.Parameters.GetRawText(), Assert.Single(roundTrip.Operations).Parameters.GetRawText());
    }

    [Fact]
    public void KnownOperationWithoutResolverCanReplayItsSavedPrimitiveExpansion()
    {
        var registry = new OperationRegistry();
        Assert.True(registry.IsKnownOperation(PuzzleOperationIds.Shuffle));
        Assert.False(registry.TryGetOperation(PuzzleOperationIds.Shuffle, out _));
        var rules = new PuzzleRuleSet("Replay only", new[] { PuzzleOperationIds.Slide },
            new[] { PuzzleOperationIds.Slide }, new[] { PuzzleOperationIds.Slide, PuzzleOperationIds.Shuffle }, registry);
        var recording = new SessionRecording(new[,] { { 0, 1 }, { 2, 3 } }, new[]
        {
            ShuffleOperation(new[]
            {
                new RecordedMove(RecordedMoveAxis.Row, 0, 0, 1),
                new RecordedMove(RecordedMoveAxis.Column, 1, 1, -1)
            }, OperationMoveAccounting.PrimitiveMoves, 2)
        }, rules);
        var grid = new Grid(2, 2, 32);
        var controller = new SessionRecordingController(grid);

        controller.LoadPlayback(SessionRecording.FromJson(recording.ToJson(), registry));
        Assert.True(controller.Step());
        Assert.Equal(1, controller.Position);
        Assert.True(controller.Step());
        Assert.Equal(2, controller.Position);
        Assert.Equal(new[] { 1, 3, 2, 0 }, Capture(grid));
    }

    [Fact]
    public void LogicalAndPrimitiveAccountingRemainDistinctAndScrambleHasNoSolveCost()
    {
        var registry = OperationRegistry.CreateDefault();
        var directRules = new PuzzleRuleSet("Direct", new[] { PuzzleOperationIds.Slide },
            new[] { PuzzleOperationIds.Shuffle }, new[] { PuzzleOperationIds.Shuffle }, registry);
        var expandedRules = new PuzzleRuleSet("Expanded", new[] { PuzzleOperationIds.Slide },
            new[] { PuzzleOperationIds.Slide }, new[] { PuzzleOperationIds.Shuffle }, registry);
        var direct = new SessionRecording(new[,] { { 0, 1 }, { 2, 3 } }, new[]
        {
            ShuffleOperation(new[]
            {
                new RecordedMove(RecordedMoveAxis.Row, 0, 0, 1),
                new RecordedMove(RecordedMoveAxis.Column, 1, 1, -1)
            }, OperationMoveAccounting.LogicalOperation, 1)
        }, directRules);
        var expanded = new SessionRecording(new[,] { { 0, 1 }, { 2, 3 } }, new[]
        {
            ShuffleOperation(new[]
            {
                new RecordedMove(RecordedMoveAxis.Row, 0, 0, 1),
                new RecordedMove(RecordedMoveAxis.Column, 1, 1, -1)
            }, OperationMoveAccounting.PrimitiveMoves, 2)
        }, expandedRules);

        Assert.Equal(1, Assert.Single(direct.Operations).MoveCost);
        Assert.Equal(2, Assert.Single(expanded.Operations).MoveCost);
        var scramble = new RecordedOperation(PuzzleOperationIds.Shuffle, ShuffleParameters(),
            new[] { new RecordedMove(RecordedMoveAxis.Row, 0, 0, 1) }, OperationPlanPurpose.Scramble,
            OperationMoveAccounting.None, null);
        var scrambleRules = new PuzzleRuleSet("Scramble", new[] { PuzzleOperationIds.Shuffle },
            new[] { PuzzleOperationIds.Slide }, new[] { PuzzleOperationIds.Slide }, registry);
        var automatic = new SessionRecording(new[,] { { 0, 1, 2 } }, new[] { scramble }, scrambleRules);
        Assert.Equal(OperationMoveAccounting.None, Assert.Single(automatic.Operations).Accounting);
        Assert.Null(Assert.Single(automatic.Operations).MoveCost);
    }

    [Fact]
    public void ReverseAndDoubleReversePreserveOperationBoundariesAndAccounting()
    {
        var rules = new PuzzleRuleSet("Direct", new[] { PuzzleOperationIds.Slide },
            new[] { PuzzleOperationIds.Shuffle }, new[] { PuzzleOperationIds.Shuffle }, OperationRegistry.CreateDefault());
        var recording = new SessionRecording(new[,] { { 0, 1, 2 } }, new[]
        {
            ShuffleOperation(new[]
            {
                new RecordedMove(RecordedMoveAxis.Row, 0, 0, 1),
                new RecordedMove(RecordedMoveAxis.Row, 0, 0, -1)
            }, OperationMoveAccounting.LogicalOperation, 1)
        }, rules);

        var twice = recording.Reverse().Reverse();

        Assert.Equal(recording.ToJson(), twice.ToJson());
        Assert.Equal(recording.Operations.Count, twice.Operations.Count);
        Assert.Equal(0, recording.GetVerifiedGoal());
    }

    [Fact]
    public void GoalPositionCountsPrimitiveStepsInsideOneLogicalOperation()
    {
        var operation = ShuffleOperation(new[]
        {
            new RecordedMove(RecordedMoveAxis.Row, 0, 0, 1),
            new RecordedMove(RecordedMoveAxis.Row, 0, 0, 1)
        }, OperationMoveAccounting.LogicalOperation, 1);
        var recording = new SessionRecording(new[,] { { 1, 2, 0 } }, new[] { operation },
            new PuzzleRuleSet("Direct", new[] { PuzzleOperationIds.Slide },
                new[] { PuzzleOperationIds.Shuffle }, new[] { PuzzleOperationIds.Shuffle }, OperationRegistry.CreateDefault()));

        Assert.Equal(2, recording.GetVerifiedGoal());
        Assert.Equal(1, Assert.Single(recording.Operations).MoveCost);
    }

    [Fact]
    public void ZeroOperationRecordingRoundTripsWithItsRules()
    {
        var rules = new PuzzleRuleSet("Empty", new[] { PuzzleOperationIds.Slide },
            new[] { PuzzleOperationIds.Slide }, new[] { PuzzleOperationIds.Slide }, OperationRegistry.CreateDefault());
        var recording = new SessionRecording(new[,] { { 0, 1 } }, Array.Empty<RecordedOperation>(), rules);
        var roundTrip = SessionRecording.FromJson(recording.ToJson());

        Assert.Empty(roundTrip.Operations);
        Assert.Empty(roundTrip.Moves);
        Assert.Equal(0, roundTrip.GetVerifiedGoal());
        Assert.Equal("Empty", roundTrip.RuleSet.Name);
    }

    [Fact]
    public void UnknownOperationsAndInconsistentAccountingAreRejected()
    {
        var recording = new SessionRecording(new[,] { { 0, 1, 2 } }, new[]
        {
            SlideOperation(new RecordedMove(RecordedMoveAxis.Row, 0, 0, 1))
        });
        string unknown = recording.ToJson().Replace("\"operationId\":\"Slide\"", "\"operationId\":\"Unknown\"");
        string badCost = recording.ToJson().Replace("\"moveCost\":1", "\"moveCost\":2");

        Assert.Throws<JsonException>(() => SessionRecording.FromJson(unknown));
        Assert.Throws<JsonException>(() => SessionRecording.FromJson(badCost));
    }

    [Fact]
    public void ContinueRecordingRejectsPrimitivePositionInsideAnOperationWithoutMutation()
    {
        var recording = new SessionRecording(new[,] { { 0, 1 }, { 2, 3 } }, new[]
        {
            ShuffleOperation(new[]
            {
                new RecordedMove(RecordedMoveAxis.Row, 0, 0, 1),
                new RecordedMove(RecordedMoveAxis.Column, 1, 1, -1)
            }, OperationMoveAccounting.LogicalOperation, 1)
        }, new PuzzleRuleSet("Continue", new[] { PuzzleOperationIds.Slide },
            new[] { PuzzleOperationIds.Shuffle }, new[] { PuzzleOperationIds.Slide, PuzzleOperationIds.Shuffle }, OperationRegistry.CreateDefault()));
        var grid = new Grid(2, 2, 32);
        var controller = new SessionRecordingController(grid);
        controller.LoadPlayback(recording);
        Assert.True(controller.Step());
        var before = Capture(grid);

        Assert.False(controller.IsOperationBoundary(controller.Position));
        Assert.Throws<InvalidOperationException>(() => controller.ContinueRecordingFromHere());

        Assert.Equal(RecordingControllerMode.Playback, controller.Mode);
        Assert.Equal(1, controller.Position);
        Assert.Equal(before, Capture(grid));
    }

    [Fact]
    public void ContinueAndAppendAtBoundaryRetainRulesAndCompleteOperationPrefix()
    {
        var rules = new PuzzleRuleSet("Continue snapshot", new[] { PuzzleOperationIds.Slide },
            new[] { PuzzleOperationIds.Slide, PuzzleOperationIds.Shuffle },
            new[] { PuzzleOperationIds.Slide, PuzzleOperationIds.Shuffle }, OperationRegistry.CreateDefault());
        var original = new SessionRecording(new[,] { { 0, 1 }, { 2, 3 } }, new[]
        {
            ShuffleOperation(new[]
            {
                new RecordedMove(RecordedMoveAxis.Row, 0, 0, 1),
                new RecordedMove(RecordedMoveAxis.Column, 1, 1, -1)
            }, OperationMoveAccounting.LogicalOperation, 1),
            ShuffleOperation(new[]
            {
                new RecordedMove(RecordedMoveAxis.Row, 1, 0, 1),
                new RecordedMove(RecordedMoveAxis.Column, 0, 0, 1)
            }, OperationMoveAccounting.LogicalOperation, 1)
        }, rules);
        var grid = new Grid(2, 2, 32);
        var controller = new SessionRecordingController(grid);
        controller.LoadPlayback(original);
        Assert.True(controller.Step());
        Assert.True(controller.Step());
        Assert.True(controller.IsOperationBoundary(controller.Position));
        Assert.Equal(2, controller.ContinueRecordingFromHere());
        Assert.True(controller.MoveRow(0, 1));
        var continued = controller.StopRecording();

        Assert.Equal("Continue snapshot", continued.RuleSet.Name);
        Assert.Equal(original.InitialCells, continued.InitialCells);
        Assert.Equal(2, continued.Operations.Count);
        Assert.Equal(3, continued.Moves.Count);
        Assert.Equal(PuzzleOperationIds.Shuffle, continued.Operations[0].OperationId);
        Assert.Equal(PuzzleOperationIds.Slide, continued.Operations[1].OperationId);
        Assert.Equal(OperationMoveAccounting.LogicalOperation, continued.Operations[0].Accounting);
    }

    [Fact]
    public void ApplyOperationRejectsInvalidParametersBeforeMutationAndReplaysValidExpansion()
    {
        var registry = new OperationRegistry(new IPuzzleOperation[] { new FixedShuffleOperation() });
        var rules = new PuzzleRuleSet("Fixed shuffle", new[] { PuzzleOperationIds.Slide },
            new[] { PuzzleOperationIds.Shuffle }, new[] { PuzzleOperationIds.Shuffle }, registry);
        var grid = new Grid(2, 2, 32);
        var controller = new SessionRecordingController(grid, rules);
        controller.StartRecording();
        var evaluator = new OperationRuleEvaluator(registry);
        var evaluation = evaluator.EvaluateUiRequest(new OperationBoardSnapshot(new[,] { { 0, 1 }, { 2, 3 } }),
            new FixedShuffleIntent(), rules);
        var plan = Assert.IsType<OperationPlan>(evaluation.Plan);
        int[] before = Capture(grid);

        var invalidParameters = JsonSerializer.SerializeToElement(new
        {
            lensAnchor = new[] { 2, 0 },
            startCell = "Circle",
            targetCell = "Star"
        });
        Assert.Throws<ArgumentException>(() => controller.ApplyOperation(plan, invalidParameters));
        Assert.Equal(before, Capture(grid));
        Assert.Equal(0, controller.MoveCount);

        var parameters = JsonSerializer.SerializeToElement(new
        {
            lensAnchor = new[] { 0, 0 },
            startCell = "Circle",
            targetCell = "Star"
        });
        controller.ApplyOperation(plan, parameters);
        var recording = controller.StopRecording();
        Assert.Equal(OperationMoveAccounting.LogicalOperation, Assert.Single(recording.Operations).Accounting);
        Assert.Equal(1, recording.Operations[0].MoveCost);

        var replayGrid = new Grid(2, 2, 32);
        var replay = new SessionRecordingController(replayGrid);
        replay.LoadPlayback(SessionRecording.FromJson(recording.ToJson()));
        Assert.True(replay.Step());
        Assert.True(replay.Step());
        Assert.Equal(Capture(grid), Capture(replayGrid));
    }

    private static RecordedOperation SlideOperation(RecordedMove move) => RecordedOperation.CreateSlide(move);

    private static RecordedOperation ShuffleOperation(IEnumerable<RecordedMove> moves,
        OperationMoveAccounting accounting, int? cost) => new(PuzzleOperationIds.Shuffle, ShuffleParameters(), moves,
        OperationPlanPurpose.Solve, accounting, cost);

    private static JsonElement ShuffleParameters() => JsonSerializer.SerializeToElement(new
    {
        lensAnchor = new[] { 0, 0 },
        startCell = "Circle",
        targetCell = "Star"
    });

    private static int[] Capture(Grid grid) => grid.Tiles.Cast<Tile>()
        .Select(tile => tile.OriginY * grid.Width + tile.OriginX).ToArray();

    private sealed record FixedShuffleIntent : IOperationIntent
    {
        public string OperationId => PuzzleOperationIds.Shuffle;
    }

    private sealed class FixedShuffleOperation : IPuzzleOperation
    {
        public string Id => PuzzleOperationIds.Shuffle;

        public OperationResolution Resolve(OperationBoardSnapshot board, IOperationIntent intent) =>
            OperationResolution.Success(Id, new[]
            {
                new RecordedMove(RecordedMoveAxis.Row, 0, 0, 1),
                new RecordedMove(RecordedMoveAxis.Column, 1, 1, -1)
            });
    }
}
