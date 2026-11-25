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
}