using System.Text.Json;

namespace SlideScramble;

internal static class ClipboardJson
{
    private static string Pair(int column, int row) => JsonSerializer.Serialize(new[] { column, row });

    private static string Identity(Tile tile) => Pair(tile.OriginX + 1, tile.OriginY + 1);

    public static string Board(Tile[,] tiles)
    {
        var rows = new string[tiles.GetLength(0)];
        for (int y = 0; y < rows.Length; y++)
        {
            var cells = new string[tiles.GetLength(1)];
            for (int x = 0; x < cells.Length; x++)
                cells[x] = Identity(tiles[y, x]);
            rows[y] = $"  [{string.Join(", ", cells)}]";
        }
        return "[\n" + string.Join(",\n", rows) + "\n]";
    }

    public static string? Lens(Tile[,] tiles, PortableLensInteractionController lens)
    {
        if (lens.Phase == PortableLensInteractionPhase.Inactive) return null;

        var anchor = (lens.AnchorX, lens.AnchorY);
        string Cell(PortableLensCell symbol)
        {
            var (x, y) = LensGestureOperationMapper.BoardPosition(symbol, anchor,
                tiles.GetLength(1), tiles.GetLength(0));
            return Identity(tiles[y, x]);
        }

        return $$"""
            {
              "at": {{Pair(lens.AnchorX + 1, lens.AnchorY + 1)}},
              "cells": [
                [{{Cell(PortableLensCell.Circle)}}, {{Cell(PortableLensCell.Star)}}],
                [{{Cell(PortableLensCell.Triangle)}}, {{Cell(PortableLensCell.Square)}}]
              ]
            }
            """;
    }

    public static string Copy(string subject, string? json, Func<string, string?> writeText)
    {
        if (json == null) return "Position the Portable Lens before copying it.";

        try
        {
            if (writeText(json) is { } error)
                return $"Could not copy {subject.ToLowerInvariant()}: {error}";
            return $"{subject} copied to clipboard.";
        }
        catch (Exception ex)
        {
            return $"Could not copy {subject.ToLowerInvariant()}: {ex.Message}";
        }
    }
}
