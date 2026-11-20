using SlideScramble;
using Xunit;

namespace SlideScramble.Tests;

public class SessionRecordingGoalTests
{
    [Theory]
    [InlineData(4, null, "Moves: 4")]
    [InlineData(4, 0, "Moves: 4  Goal: 0")]
    [InlineData(4, 15, "Moves: 4  Goal: 15")]
    public void HudOmitsOnlyUnknownGoals(int moves, int? goal, string expected)
    {
        Assert.Equal(expected, Window.FormatMovesText(moves, goal));
    }

    [Fact]
    public void InitiallySolvedReturnsZeroEvenWhenLaterUnsolved()
    {
        var recording = new SessionRecording(new[,] { { 0, 1, 2 } },
            new[] { new RecordedMove(RecordedMoveAxis.Row, 0, 1) });
        Assert.Equal(0, recording.GetVerifiedGoal());
    }

    [Fact]
    public void ReturnsFirstSolvedPositionWithoutMutatingRecording()
    {
        var recording = new SessionRecording(new[,] { { 1, 2, 0 }, { 3, 4, 5 } }, new[]
        {
            new RecordedMove(RecordedMoveAxis.Row, 0, -1),
            new RecordedMove(RecordedMoveAxis.Column, 1, 1),
            new RecordedMove(RecordedMoveAxis.Column, 1, -1)
        });
        string before = recording.ToJson();
        Assert.Equal(1, recording.GetVerifiedGoal());
        Assert.Equal(1, recording.GetVerifiedGoal());
        Assert.Equal(before, recording.ToJson());
    }

    [Fact]
    public void NeverSolvedReturnsNull()
    {
        var recording = new SessionRecording(new[,] { { 1, 0 }, { 2, 3 } },
            new[] { new RecordedMove(RecordedMoveAxis.Row, 1, 1) });
        Assert.Null(recording.GetVerifiedGoal());
        Assert.Null(new SessionRecording(new[,] { { 1, 0 } }, Array.Empty<RecordedMove>()).GetVerifiedGoal());
    }

    [Theory]
    [InlineData(int.MinValue, 1)]
    [InlineData(int.MaxValue, 1)]
    [InlineData(-7, 2)]
    [InlineData(7, 1)]
    public void RectangularColumnMovesHandleExtremeAndWrappedDeltas(int delta, int shift)
    {
        var solved = new[,] { { 0, 1 }, { 2, 3 }, { 4, 5 } };
        var scrambled = (int[,])solved.Clone();
        for (int y = 0; y < 3; y++) scrambled[y, 1] = solved[(y - shift + 3) % 3, 1];
        var recording = new SessionRecording(scrambled, new[] { new RecordedMove(RecordedMoveAxis.Column, 1, delta) });
        Assert.Equal(1, recording.GetVerifiedGoal());
    }
}