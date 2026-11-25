using SkiaSharp;

namespace SlideScramble;

public static class SelectionPreviewRules
{
    public static IReadOnlyList<string> Format(PuzzleRuleSet? rules)
    {
        if (rules == null) return Array.Empty<string>();

        return new[]
        {
            $"Rules: {rules.Name}",
            $"Scramble: {FormatSet(rules.ScrambleAllowed)}",
            $"Solve: {FormatSet(rules.SolveAllowed)}",
            $"Input (initial): {FormatSet(rules.UIEnabled)}"
        };
    }

    private static string FormatSet(IEnumerable<string> operationIds)
    {
        var operations = operationIds.ToArray();
        return operations.Length == 0 ? "None" : string.Join(", ", operations);
    }
}

public readonly record struct SelectionPreviewLayout(SKRect RulesBounds, SKRect BoardBounds)
{
    public static SelectionPreviewLayout Calculate(SKRect previewArea, int boardWidth, int boardHeight,
        int ruleLineCount)
    {
        if (boardWidth <= 0) throw new ArgumentOutOfRangeException(nameof(boardWidth));
        if (boardHeight <= 0) throw new ArgumentOutOfRangeException(nameof(boardHeight));

        float headerHeight = ruleLineCount > 0 ? Math.Min(60, Math.Max(0, previewArea.Height) * 0.4f) : 0;
        float availableTop = previewArea.Top + headerHeight;
        float availableHeight = Math.Max(0, previewArea.Bottom - availableTop);
        float availableWidth = Math.Max(0, previewArea.Width);
        float tileSize = Math.Min(availableWidth / boardWidth, availableHeight / boardHeight);
        float gridWidth = tileSize * boardWidth;
        float gridHeight = tileSize * boardHeight;
        float gridLeft = previewArea.MidX - gridWidth / 2;
        float gridTop = availableTop + (availableHeight - gridHeight) / 2;

        var boardBounds = new SKRect(gridLeft, gridTop, gridLeft + gridWidth, gridTop + gridHeight);
        var rulesBounds = headerHeight > 0
            ? new SKRect(gridLeft, gridTop - headerHeight, gridLeft + gridWidth, gridTop)
            : new SKRect(gridLeft, gridTop, gridLeft, gridTop);
        return new SelectionPreviewLayout(rulesBounds, boardBounds);
    }
}