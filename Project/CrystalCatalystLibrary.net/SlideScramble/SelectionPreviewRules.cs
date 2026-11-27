using System.Drawing;
using SkiaSharp;

namespace SlideScramble;

public static class SelectionPreviewRules
{
    public const int PresetPreviewLineCount = 5;

    public static IReadOnlyList<PuzzleRuleSet> CreatePresets(OperationRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        return Array.AsReadOnly(new[]
        {
            PuzzleRuleSet.CreateClassic(registry),
            PuzzleRuleSet.CreateShuffleLogic(registry),
            PuzzleRuleSet.CreateSwapLogic(registry),
            PuzzleRuleSet.CreateTwoWide(registry),
            PuzzleRuleSet.CreateTwoTall(registry),
            PuzzleRuleSet.CreateTwoWideSlide(registry),
            PuzzleRuleSet.CreateTwoTallSlide(registry),
            PuzzleRuleSet.CreateHybrid(registry),
            PuzzleRuleSet.CreateZeroSlide(registry),
            PuzzleRuleSet.CreateFreeStyle(registry)
        });
    }

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

    public static IReadOnlyList<string> Format(PuzzleRuleSet? rules, int boardWidth, int boardHeight)
    {
        var lines = Format(rules).ToList();
        if (rules?.GetBoardCompatibilityMessage(boardWidth, boardHeight) is { } message)
            lines.Add($"Board: {message}");
        return lines;
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

public readonly record struct PresetMenuPreviewLayout(RectangleF MenuBounds, SKRect PreviewBounds, bool IsStacked)
{
    public static PresetMenuPreviewLayout Calculate(SKRect windowBounds, float menuWidth, float menuHeight,
        int previewLineCount)
    {
        if (menuWidth <= 0) throw new ArgumentOutOfRangeException(nameof(menuWidth));
        if (menuHeight <= 0) throw new ArgumentOutOfRangeException(nameof(menuHeight));
        if (previewLineCount <= 0) throw new ArgumentOutOfRangeException(nameof(previewLineCount));

        float width = Math.Max(1, windowBounds.Width);
        float height = Math.Max(1, windowBounds.Height);
        float margin = Math.Min(12, Math.Min(width, height) * 0.04f);
        float gap = Math.Min(16, Math.Max(4, Math.Min(width, height) * 0.04f));
        float usableWidth = Math.Max(1, width - margin * 2);
        float usableHeight = Math.Max(1, height - margin * 2);
        float previewHeight = Math.Min(112, 16 + previewLineCount * 18);
        float previewMinWidth = 220;
        float left = windowBounds.Left + margin;
        float top = windowBounds.Top + margin;
        float right = windowBounds.Right - margin;
        float bottom = windowBounds.Bottom - margin;

        if (usableWidth >= menuWidth + previewMinWidth + gap)
        {
            float menuSlotWidth = Math.Min(menuWidth, usableWidth - previewMinWidth - gap);
            var menuSlot = new RectangleF(left, top, menuSlotWidth, usableHeight);
            var menuBounds = FitMenu(menuSlot, menuWidth, menuHeight);
            float previewWidth = Math.Max(1, right - menuSlot.Right - gap);
            float actualPreviewHeight = Math.Min(previewHeight, usableHeight);
            float previewTop = top + (usableHeight - actualPreviewHeight) / 2;
            var previewBounds = new SKRect(menuSlot.Right + gap, previewTop,
                menuSlot.Right + gap + previewWidth, previewTop + actualPreviewHeight);
            return new PresetMenuPreviewLayout(menuBounds, previewBounds, false);
        }

        float stackedPreviewHeight = Math.Min(previewHeight, usableHeight * 0.3f);
        var stackedPreview = new SKRect(left, top, right, top + stackedPreviewHeight);
        float menuTop = stackedPreview.Bottom + gap;
        var stackedMenuArea = new RectangleF(left, menuTop, usableWidth, Math.Max(1, bottom - menuTop));
        return new PresetMenuPreviewLayout(FitMenu(stackedMenuArea, menuWidth, menuHeight), stackedPreview, true);
    }

    private static RectangleF FitMenu(RectangleF area, float menuWidth, float menuHeight)
    {
        float scale = Math.Min(1, Math.Min(Math.Max(1, area.Width) / menuWidth,
            Math.Max(1, area.Height) / menuHeight));
        float width = menuWidth * scale;
        float height = menuHeight * scale;
        return new RectangleF(area.X + (area.Width - width) / 2, area.Y + (area.Height - height) / 2,
            width, height);
    }
}