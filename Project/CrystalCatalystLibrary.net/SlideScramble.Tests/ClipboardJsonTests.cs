using System.Text.Json;
using Xunit;

namespace SlideScramble.Tests;

public class ClipboardJsonTests
{
    [Fact]
    public void BoardCopiesTileIdentitiesInCurrentRowOrder()
    {
        Tile[,] tiles =
        {
            { new(2, 1, null!), new(0, 0, null!), new(1, 0, null!) },
            { new(0, 1, null!), new(1, 1, null!), new(2, 0, null!) }
        };

        string json = ClipboardJson.Board(tiles);

        Assert.Equal("""
            [
              [[3,2], [1,1], [2,1]],
              [[1,2], [2,2], [3,1]]
            ]
            """, json);
        using var document = JsonDocument.Parse(json);
        Assert.Equal(2, document.RootElement.GetArrayLength());
        Assert.Equal(3, document.RootElement[0].GetArrayLength());
    }

    [Theory]
    [InlineData(0, 0, "[1,1]", "[[4,3], [3,3]]", "[[4,2], [3,2]]")]
    [InlineData(3, 0, "[4,1]", "[[1,3], [4,3]]", "[[1,2], [4,2]]")]
    [InlineData(0, 2, "[1,3]", "[[4,1], [3,1]]", "[[4,3], [3,3]]")]
    [InlineData(3, 2, "[4,3]", "[[1,1], [4,1]]", "[[1,3], [4,3]]")]
    public void LensCopiesCircleStarAndTriangleSquareRows(
        int x, int y, string at, string top, string bottom)
    {
        var tiles = new Tile[3, 4];
        for (int row = 0; row < 3; row++)
        for (int column = 0; column < 4; column++)
            tiles[row, column] = new Tile(3 - column, 2 - row, null!);
        var lens = new PortableLensInteractionController();
        Assert.True(lens.BeginPlacement(4, 3, x, y));
        Assert.True(lens.AcceptPosition());

        string? json = ClipboardJson.Lens(tiles, lens);

        Assert.Equal($$"""
            {
              "at": {{at}},
              "cells": [
                {{top}},
                {{bottom}}
              ]
            }
            """, json);
        using var document = JsonDocument.Parse(json!);
        Assert.Equal(x + 1, document.RootElement.GetProperty("at")[0].GetInt32());
        Assert.Equal(y + 1, document.RootElement.GetProperty("at")[1].GetInt32());
    }

    [Fact]
    public void LensWithoutPlacementExplainsHowToCopyAndSkipsClipboard()
    {
        string? json = ClipboardJson.Lens(new Tile[3, 3], new PortableLensInteractionController());

        Assert.Null(json);
        bool wrote = false;
        string message = ClipboardJson.Copy("Lens", json, _ =>
        {
            wrote = true;
            return null;
        });

        Assert.False(wrote);
        Assert.Equal("Position the Portable Lens before copying it.", message);
    }

    [Fact]
    public void CopyWritesTheJsonAndReportsSuccess()
    {
        var tiles = new Tile[,] { { new(0, 0, null!) } };
        string json = ClipboardJson.Board(tiles);
        string? copied = null;

        string message = ClipboardJson.Copy("Board", json, text =>
        {
            copied = text;
            return null;
        });

        Assert.Equal(json, copied);
        Assert.Equal("Board copied to clipboard.", message);
    }

    [Fact]
    public void CopyReportsBothReportedBackendErrorsAndThrownFailures()
    {
        Assert.Equal("Could not copy board: Clipboard unavailable.",
            ClipboardJson.Copy("Board", "[]", _ => "Clipboard unavailable."));
        Assert.Equal("Could not copy lens: Clipboard unavailable.",
            ClipboardJson.Copy("Lens", "{}", _ => throw new InvalidOperationException("Clipboard unavailable.")));
    }
}
