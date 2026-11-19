using SlideScramble;
using Xunit;

namespace SlideScramble.Tests;

public class SlideScrambleSessionTests
{
    [Fact]
    public void InitialCellsAreValidatedAndCopied()
    {
        Assert.Throws<ArgumentNullException>(() => new SlideScrambleSession(null!));
        Assert.Throws<ArgumentException>(() => new SlideScrambleSession(new int[0, 2]));
        Assert.Throws<ArgumentException>(() => new SlideScrambleSession(new[,] { { 0, 0 } }));
        Assert.Throws<ArgumentException>(() => new SlideScrambleSession(new[,] { { 0, 2 } }));
        Assert.Throws<ArgumentException>(() => new SlideScrambleSession(new[,] { { -1, 0 } }));
        int[,] initial = { { 1, 0, 2 }, { 3, 4, 5 } };
        var session = new SlideScrambleSession(initial);
        initial[0, 0] = 0;
        session.Cells[0, 0] = 0;
        Assert.Equal(1, session.Cells[0, 0]);
        Assert.Equal(3, session.Width);
        Assert.Equal(2, session.Height);
        Assert.False(session.IsSolved);
    }

    [Fact]
    public void MovesMatchGridForRectangularBoardAndExtremeDeltas()
    {
        var grid = new Grid(3, 2, 32);
        var session = new SlideScrambleSession(new[,] { { 0, 1, 2 }, { 3, 4, 5 } });
        grid.MoveRow(0, int.MaxValue);
        session.MoveRow(0, int.MaxValue, 1);
        grid.MoveColumn(1, int.MinValue + 1);
        session.MoveColumn(1, int.MinValue + 1, 2);
        int[,] cells = session.Cells;
        for (int y = 0; y < 2; y++)
            for (int x = 0; x < 3; x++)
                Assert.Equal(grid.Tiles[y, x].OriginY * 3 + grid.Tiles[y, x].OriginX, cells[y, x]);
    }

    [Fact]
    public void ScrambleSolveResumeAndResetMaintainState()
    {
        var session = new SlideScrambleSession(new[,] { { 0, 1, 2 }, { 3, 4, 5 } });
        session.StartScrambling();
        Assert.Equal(GameMode.Scrambling, session.Mode);
        session.MoveRow(0, 1, 1);
        Assert.Equal(0, session.MoveCount);
        session.FinishScrambling(10);
        Assert.Equal(GameMode.InGame, session.Mode);
        session.MoveRow(0, -1, 25);
        Assert.True(session.IsSolved);
        Assert.Equal(GameMode.Solved, session.Mode);
        Assert.Equal(15, session.GetElapsedTime(100));
        session.MoveColumn(0, 1, 35);
        Assert.Equal(GameMode.InGame, session.Mode);
        Assert.Equal(2, session.MoveCount);
        Assert.Equal(20, session.GetElapsedTime(40));
        session.Reset();
        Assert.True(session.IsSolved);
        Assert.Equal(GameMode.Ready, session.Mode);
        Assert.Equal(0, session.MoveCount);
        Assert.Equal(0, session.GetElapsedTime(40));
    }

    [Fact]
    public void InvalidAndNoOpMovesDoNotChangeSession()
    {
        var session = new SlideScrambleSession(new[,] { { 1, 0 }, { 2, 3 } });
        Assert.Throws<InvalidOperationException>(() => session.FinishScrambling(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => session.MoveRow(2, 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => session.MoveColumn(-1, 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => session.MoveRow(0, 1, double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => session.GetElapsedTime(-1));
        Assert.False(session.MoveRow(0, 2, 1));
        Assert.Equal(GameMode.Ready, session.Mode);
        Assert.Equal(0, session.MoveCount);
        session.MoveRow(0, 1, 2);
        Assert.Equal(GameMode.Solved, session.Mode);
        session.Reset();
        Assert.Equal(1, session.Cells[0, 0]);
        Assert.False(session.IsSolved);
    }
}