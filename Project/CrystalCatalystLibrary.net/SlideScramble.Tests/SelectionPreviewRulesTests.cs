using System.Text.Json.Nodes;
using SkiaSharp;
using Xunit;

namespace SlideScramble.Tests;

public class SelectionPreviewRulesTests
{
    [Fact]
    public void FormatsPresetAndIndependentOperationSets()
    {
        var rules = PuzzleRuleSet.CreateHybrid();

        Assert.Equal(new[]
        {
            "Rules: Hybrid",
            "Scramble: Shuffle, Slide",
            "Solve: Shuffle, Slide",
            "Input (initial): Shuffle, Slide"
        }, SelectionPreviewRules.Format(rules));
    }

    [Fact]
    public void FormatsEmptyCustomRuleSetsAsNone()
    {
        var rules = new PuzzleRuleSet("Custom", Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(),
            OperationRegistry.CreateDefault());

        Assert.Equal(new[]
        {
            "Rules: Custom",
            "Scramble: None",
            "Solve: None",
            "Input (initial): None"
        }, SelectionPreviewRules.Format(rules));
        Assert.Empty(SelectionPreviewRules.Format(null));
    }

    [Fact]
    public void PresetPreviewCandidatesContainAllExpectedRuleSetsInMenuOrder()
    {
        var activeRules = PuzzleRuleSet.CreateClassic();
        var candidates = SelectionPreviewRules.CreatePresets(activeRules.Registry);

        Assert.Equal(new[]
        {
            "Classic", "ShuffleLogic", "SwapLogic", "TwoWide", "TwoTall", "TwoWideSlide", "TwoTallSlide",
            "TwoWideColumnSlide", "TwoTallRowSlide", "Hybrid", "ZeroSlide", "FreeStyle"
        }, candidates.Select(rules => rules.Name));

        var expected = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Classic"] = [PuzzleOperationIds.Slide],
            ["ShuffleLogic"] = [PuzzleOperationIds.Shuffle],
            ["SwapLogic"] = [PuzzleOperationIds.ColumnSwap, PuzzleOperationIds.RowSwap],
            ["TwoWide"] = [PuzzleOperationIds.ColumnSwap, PuzzleOperationIds.Slide],
            ["TwoTall"] = [PuzzleOperationIds.RowSwap, PuzzleOperationIds.Slide],
            ["TwoWideSlide"] = [PuzzleOperationIds.ColumnSwap, PuzzleOperationIds.RowSlide],
            ["TwoTallSlide"] = [PuzzleOperationIds.ColumnSlide, PuzzleOperationIds.RowSwap],
            ["TwoWideColumnSlide"] = [PuzzleOperationIds.ColumnSlide, PuzzleOperationIds.ColumnSwap],
            ["TwoTallRowSlide"] = [PuzzleOperationIds.RowSlide, PuzzleOperationIds.RowSwap],
            ["Hybrid"] = [PuzzleOperationIds.Shuffle, PuzzleOperationIds.Slide],
            ["ZeroSlide"] = [PuzzleOperationIds.ColumnSwap, PuzzleOperationIds.RowSwap, PuzzleOperationIds.Shuffle],
            ["FreeStyle"] =
            [
                PuzzleOperationIds.ColumnSwap, PuzzleOperationIds.RowSwap, PuzzleOperationIds.Shuffle,
                PuzzleOperationIds.Slide
            ]
        };

        foreach (var candidate in candidates)
        {
            Assert.Equal(expected[candidate.Name], candidate.ScrambleAllowed);
            Assert.Equal(expected[candidate.Name], candidate.SolveAllowed);
            Assert.Equal(expected[candidate.Name], candidate.UIEnabled);
            Assert.Equal($"Rules: {candidate.Name}", SelectionPreviewRules.Format(candidate)[0]);
        }

        Assert.Equal(new[] { PuzzleOperationIds.Slide }, activeRules.ScrambleAllowed);
        Assert.Equal(new[] { PuzzleOperationIds.Slide }, activeRules.SolveAllowed);
        Assert.Equal(new[] { PuzzleOperationIds.Slide }, activeRules.UIEnabled);
    }

    [Fact]
    public void PresetPreviewIncludesBoardCompatibilityWithoutHidingPermissions()
    {
        var twoWide = PuzzleRuleSet.CreateTwoWide();
        var swapLogic = PuzzleRuleSet.CreateSwapLogic();

        var twoWideLines = SelectionPreviewRules.Format(twoWide, 2, 2);
        var swapLogicLines = SelectionPreviewRules.Format(swapLogic, 2, 2);

        Assert.Contains("Scramble: ColumnSwap, Slide", twoWideLines);
        Assert.Contains("Solve: ColumnSwap, Slide", twoWideLines);
        Assert.Contains("Input (initial): ColumnSwap, Slide", twoWideLines);
        Assert.Contains("Board: Portable Lens operations require a 3 × 3 board; Slide remains available.",
            twoWideLines);
        Assert.Contains("Board: SwapLogic requires a 3 × 3 board for its configured Portable Lens operations.",
            swapLogicLines);
    }

    [Fact]
    public void LegacyRecordingPreviewUsesClassicRules()
    {
        var recording = new SessionRecording(new[,] { { 0, 1 }, { 2, 3 } }, Array.Empty<RecordedOperation>(),
            PuzzleRuleSet.CreateHybrid());
        var legacyData = JsonNode.Parse(recording.ToJson())!.AsObject();
        legacyData["version"] = 2;
        legacyData.Remove("ruleSet");
        legacyData.Remove("operations");
        legacyData["moves"] = new JsonArray();

        var loaded = SessionRecording.FromJson(legacyData.ToJsonString());

        Assert.Equal(new[] { PuzzleOperationIds.Slide }, loaded.RuleSet.ScrambleAllowed);
        Assert.Equal(new[] { PuzzleOperationIds.Slide }, loaded.RuleSet.SolveAllowed);
        Assert.Equal(new[] { PuzzleOperationIds.Slide }, loaded.RuleSet.UIEnabled);
        Assert.Equal(new[]
        {
            "Rules: Classic",
            "Scramble: Slide",
            "Solve: Slide",
            "Input (initial): Slide"
        }, SelectionPreviewRules.Format(loaded.RuleSet));
    }

    [Theory]
    [InlineData(1200, 800, 4, 4)]
    [InlineData(420, 1200, 4, 7)]
    [InlineData(1200, 420, 7, 4)]
    [InlineData(180, 260, 3, 5)]
    [InlineData(260, 180, 5, 3)]
    public void LayoutKeepsRulesPanelDirectlyAboveBoardInResponsiveAreas(int width, int height, int columns,
        int rows)
    {
        var previewArea = new SKRect(17, 23, 17 + width, 23 + height);

        var layout = SelectionPreviewLayout.Calculate(previewArea, columns, rows, 4);

        Assert.True(layout.RulesBounds.Width > 0);
        Assert.Equal(layout.BoardBounds.Left, layout.RulesBounds.Left, 3);
        Assert.Equal(layout.BoardBounds.Right, layout.RulesBounds.Right, 3);
        Assert.Equal(layout.BoardBounds.Top, layout.RulesBounds.Bottom, 3);
        Assert.InRange(layout.RulesBounds.Left, previewArea.Left, previewArea.Right);
        Assert.InRange(layout.RulesBounds.Right, previewArea.Left, previewArea.Right);
        Assert.InRange(layout.RulesBounds.Top, previewArea.Top, previewArea.Bottom);
        Assert.InRange(layout.BoardBounds.Left, previewArea.Left, previewArea.Right);
        Assert.InRange(layout.BoardBounds.Right, previewArea.Left, previewArea.Right);
        Assert.InRange(layout.BoardBounds.Top, previewArea.Top, previewArea.Bottom);
        Assert.InRange(layout.BoardBounds.Bottom, previewArea.Top, previewArea.Bottom);
        Assert.Equal(layout.BoardBounds.Width / columns, layout.BoardBounds.Height / rows, 3);
    }

    [Fact]
    public void LayoutUsesAvailablePreviewAreaWhenNoRulesArePresent()
    {
        var previewArea = new SKRect(0, 0, 500, 300);

        var layout = SelectionPreviewLayout.Calculate(previewArea, 4, 3, 0);

        Assert.Equal(0, layout.RulesBounds.Width);
        Assert.Equal(previewArea.MidX, layout.BoardBounds.MidX, 3);
        Assert.Equal(previewArea.MidY, layout.BoardBounds.MidY, 3);
    }

    [Theory]
    [InlineData(1200, 800, false)]
    [InlineData(320, 900, true)]
    [InlineData(240, 180, true)]
    [InlineData(420, 160, true)]
    public void PresetMenuPreviewLayoutKeepsMenuAndBubbleInsideConstrainedWindows(int width, int height,
        bool expectedStacked)
    {
        var window = new SKRect(13, 17, 13 + width, 17 + height);

        var layout = PresetMenuPreviewLayout.Calculate(window, 340, 331,
            SelectionPreviewRules.PresetPreviewLineCount);

        Assert.Equal(expectedStacked, layout.IsStacked);
        Assert.True(layout.MenuBounds.Width > 0);
        Assert.True(layout.MenuBounds.Height > 0);
        Assert.True(layout.PreviewBounds.Width > 0);
        Assert.True(layout.PreviewBounds.Height > 0);
        Assert.InRange(layout.MenuBounds.Left, window.Left, window.Right);
        Assert.InRange(layout.MenuBounds.Right, window.Left, window.Right);
        Assert.InRange(layout.MenuBounds.Top, window.Top, window.Bottom);
        Assert.InRange(layout.MenuBounds.Bottom, window.Top, window.Bottom);
        Assert.InRange(layout.PreviewBounds.Left, window.Left, window.Right);
        Assert.InRange(layout.PreviewBounds.Right, window.Left, window.Right);
        Assert.InRange(layout.PreviewBounds.Top, window.Top, window.Bottom);
        Assert.InRange(layout.PreviewBounds.Bottom, window.Top, window.Bottom);
        Assert.False(layout.MenuBounds.Right > layout.PreviewBounds.Left &&
                     layout.MenuBounds.Left < layout.PreviewBounds.Right &&
                     layout.MenuBounds.Bottom > layout.PreviewBounds.Top &&
                     layout.MenuBounds.Top < layout.PreviewBounds.Bottom);
    }
}