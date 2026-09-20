using SlideScramble;
using Xunit;

namespace SlideScramble.Tests;

public class GridAndGameStateTests
{
    [Theory]
    [InlineData(0, 4, 0)]
    [InlineData(1, 4, 1)]
    [InlineData(4, 4, 0)]
    [InlineData(5, 4, 1)]
    [InlineData(-1, 4, 3)]
    [InlineData(-4, 4, 0)]
    [InlineData(-5, 4, 3)]
    public void WrapIndex_WrapsCorrectly(int index, int length, int expected)
    {
        int actual = Grid.WrapIndex(index, length);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Grid_Initialization_IsInitiallySolved()
    {
        var grid = new Grid(4, 4);
        Assert.Equal(4, grid.Width);
        Assert.Equal(4, grid.Height);
        Assert.True(grid.IsSolved());

        for (int y = 0; y < grid.Height; y++)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                Assert.Equal(x, grid.Tiles[y, x].OriginX);
                Assert.Equal(y, grid.Tiles[y, x].OriginY);
                Assert.NotNull(grid.Tiles[y, x].Image);
            }
        }
    }

    [Fact]
    public void Grid_MoveRow_ShiftsAndWraps()
    {
        var grid = new Grid(4, 4);
        // Shift row 0 right by 1
        grid.MoveRow(0, 1);

        Assert.False(grid.IsSolved());
        // In row 0, tile at x=0 should now have origin (1, 0)
        Assert.Equal(1, grid.Tiles[0, 0].OriginX);
        Assert.Equal(2, grid.Tiles[0, 1].OriginX);
        Assert.Equal(3, grid.Tiles[0, 2].OriginX);
        Assert.Equal(0, grid.Tiles[0, 3].OriginX);

        // Shift row 0 back by 3 (or -1)
        grid.MoveRow(0, -1);
        Assert.True(grid.IsSolved());
    }

    [Fact]
    public void Grid_MoveColumn_ShiftsAndWraps()
    {
        var grid = new Grid(4, 4);
        // Shift column 1 down by 1
        grid.MoveColumn(1, 1);

        Assert.False(grid.IsSolved());
        Assert.Equal(1, grid.Tiles[0, 1].OriginY);
        Assert.Equal(2, grid.Tiles[1, 1].OriginY);
        Assert.Equal(3, grid.Tiles[2, 1].OriginY);
        Assert.Equal(0, grid.Tiles[3, 1].OriginY);

        // Shift column 1 by -1 to restore
        grid.MoveColumn(1, -1);
        Assert.True(grid.IsSolved());
    }

    [Fact]
    public void Grid_Reset_RestoresSolvedState()
    {
        var grid = new Grid(4, 4);
        grid.MoveRow(0, 2);
        grid.MoveColumn(2, 3);
        Assert.False(grid.IsSolved());

        grid.Reset();
        Assert.True(grid.IsSolved());
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(8)]
    public void Grid_SupportsDifferentDimensions(int size)
    {
        var grid = new Grid(size, size);
        Assert.Equal(size, grid.Width);
        Assert.Equal(size, grid.Height);
        Assert.True(grid.IsSolved());
    }

    [Fact]
    public void GameState_LifecycleAndTransitions()
    {
        var state = new GameState();
        var grid = new Grid(4, 4);

        Assert.Equal(GameMode.Ready, state.Mode);
        Assert.Equal(0, state.MoveCount);

        // Scrambling
        state.StartScrambling();
        Assert.Equal(GameMode.Scrambling, state.Mode);

        // Finish Scramble
        grid.MoveRow(0, 1); // Scramble move
        state.FinishScrambling(10.0);
        Assert.Equal(GameMode.InGame, state.Mode);
        Assert.Equal(0, state.MoveCount);

        // Player move
        state.RecordMove();
        Assert.Equal(1, state.MoveCount);
        Assert.False(state.CheckSolved(grid, 15.0));
        Assert.Equal(GameMode.InGame, state.Mode);

        // Solved move
        grid.MoveRow(0, -1); // Solves board
        state.RecordMove();
        Assert.Equal(2, state.MoveCount);
        Assert.True(state.CheckSolved(grid, 25.0));
        Assert.Equal(GameMode.Solved, state.Mode);
        Assert.Equal(15.0, state.FinalTime, 0.001);

        // Reset
        state.Reset();
        Assert.Equal(GameMode.Ready, state.Mode);
        Assert.Equal(0, state.MoveCount);
    }

    [Fact]
    public void GameState_TakeBackSolved_OnGridManipulation()
    {
        var state = new GameState();
        var grid = new Grid(4, 4);

        state.StartScrambling();
        grid.MoveRow(0, 1);
        state.FinishScrambling(10.0);

        grid.MoveRow(0, -1); // Solves board
        state.RecordMove(20.0);
        Assert.True(state.CheckSolved(grid, 25.0));
        Assert.Equal(GameMode.Solved, state.Mode);
        Assert.Equal(15.0, state.FinalTime, 0.001);

        // Manipulate grid while in Solved state
        grid.MoveRow(0, 1); // Board is no longer solved
        state.RecordMove(35.0);
        Assert.Equal(GameMode.InGame, state.Mode);
        Assert.False(state.CheckSolved(grid, 35.0));
        Assert.Equal(2, state.MoveCount);
        // Elapsed time resumed from FinalTime (15.0s elapsed at time 35.0s)
        Assert.Equal(15.0, state.GetElapsedTime(35.0), 0.001);
        Assert.Equal(20.0, state.GetElapsedTime(40.0), 0.001);
    }
}
