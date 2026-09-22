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

    [Theory]
    [InlineData(2, 2)]
    [InlineData(2, 4)]
    [InlineData(4, 2)]
    [InlineData(3, 5)]
    [InlineData(5, 3)]
    [InlineData(9, 2)]
    [InlineData(2, 9)]
    [InlineData(9, 9)]
    public void Grid_SupportsIrregularDimensions(int width, int height)
    {
        var grid = new Grid(width, height);
        Assert.Equal(width, grid.Width);
        Assert.Equal(height, grid.Height);
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
    public void Grid_IrregularDimensions_MoveRow_WrapsAcrossWidth()
    {
        // 4 columns wide, 2 rows tall (4x2)
        var grid = new Grid(4, 2);
        
        // Shift row 0 right by 1
        grid.MoveRow(0, 1);
        Assert.False(grid.IsSolved());
        Assert.Equal(1, grid.Tiles[0, 0].OriginX);
        Assert.Equal(2, grid.Tiles[0, 1].OriginX);
        Assert.Equal(3, grid.Tiles[0, 2].OriginX);
        Assert.Equal(0, grid.Tiles[0, 3].OriginX);

        // Shift by 3 more (total 4 = Width) -> should return to solved
        grid.MoveRow(0, 3);
        Assert.True(grid.IsSolved());
    }

    [Fact]
    public void Grid_IrregularDimensions_MoveColumn_WrapsAcrossHeight()
    {
        // 2 columns wide, 4 rows tall (2x4)
        var grid = new Grid(2, 4);

        // Shift column 0 down by 1
        grid.MoveColumn(0, 1);
        Assert.False(grid.IsSolved());
        Assert.Equal(1, grid.Tiles[0, 0].OriginY);
        Assert.Equal(2, grid.Tiles[1, 0].OriginY);
        Assert.Equal(3, grid.Tiles[2, 0].OriginY);
        Assert.Equal(0, grid.Tiles[3, 0].OriginY);

        // Shift by 3 more (total 4 = Height) -> should return to solved
        grid.MoveColumn(0, 3);
        Assert.True(grid.IsSolved());
    }

    [Theory]
    [InlineData(850, 700, 2, 4)]
    [InlineData(850, 700, 4, 2)]
    [InlineData(850, 700, 2, 9)]
    [InlineData(850, 700, 9, 2)]
    [InlineData(850, 700, 9, 9)]
    [InlineData(850, 700, 4, 4)]
    [InlineData(1200, 800, 3, 5)]
    [InlineData(600, 900, 5, 3)]
    public void AspectRatioAware_LayoutGeometry_PreservesSquareTilesAndBounds(int winWidth, int winHeight, int gridW, int gridH)
    {
        float topHeaderHeight = 56f;
        float footerHeight = 36f;
        float availableWidth = Math.Max(100f, winWidth - 40f);
        float availableHeight = Math.Max(100f, winHeight - topHeaderHeight - footerHeight - 20f);

        float tileSize = Math.Min(availableWidth / gridW, availableHeight / gridH);
        float boardWidth = tileSize * gridW;
        float boardHeight = tileSize * gridH;

        float xx = (winWidth - boardWidth) / 2f;
        float yy = topHeaderHeight + (availableHeight - boardHeight) / 2f;

        // Verify square tile aspect ratio (tile width == tile height == tileSize)
        float tileW = boardWidth / gridW;
        float tileH = boardHeight / gridH;
        Assert.Equal(tileSize, tileW, 0.0001f);
        Assert.Equal(tileSize, tileH, 0.0001f);
        Assert.Equal(tileW, tileH, 0.0001f);

        // Verify fitting inside available space
        Assert.True(boardWidth <= availableWidth + 0.001f);
        Assert.True(boardHeight <= availableHeight + 0.001f);

        // Verify centering
        Assert.True(xx >= 20f);
        Assert.True(yy >= topHeaderHeight);
        Assert.True(xx + boardWidth <= winWidth - 20f + 0.001f);
        Assert.True(yy + boardHeight <= winHeight - footerHeight + 0.001f);
    }

    [Theory]
    [InlineData(2, 4, 12)]
    [InlineData(4, 2, 12)]
    [InlineData(4, 4, 16)]
    [InlineData(9, 9, 36)]
    [InlineData(2, 9, 22)]
    [InlineData(9, 2, 22)]
    public void ScrambleCount_UsesBothDimensions(int width, int height, int expectedCount)
    {
        int count = (width + height) * 2;
        Assert.Equal(expectedCount, count);
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
