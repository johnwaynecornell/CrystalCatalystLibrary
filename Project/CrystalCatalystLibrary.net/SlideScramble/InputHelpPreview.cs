using System.Drawing;
using CrystalSkia.net;

namespace SlideScramble;

/// <summary>Passive help for the highlighted operation in an input preferences menu.</summary>
internal sealed class InputHelpPreview
{
    private const string LensPlacement = "Double-click a tile to place or move the lens. Press Escape to dismiss it.";
    private readonly CrystalTextPopup?[] panels;

    public InputHelpPreview(CrystalCommandMenu menu, IEnumerable<string> operationIds, AnsiSkiaRenderer renderer)
    {
        panels = operationIds.Select(id => Describe(id) is { } text ? new CrystalTextPopup(text, renderer) : null)
            .ToArray();
        menu.SelectionChanged += index =>
            Current = index >= 0 && index < panels.Length ? panels[index] : null;
    }

    public CrystalTextPopup? Current { get; private set; }

    private static string? Describe(string operationId) => operationId switch
    {
        PuzzleOperationIds.Slide =>
            "Drag a tile horizontally to slide its row, or vertically to slide its column. Tiles wrap around the board." +
            "\nIf the lens is active, press Escape first.",
        PuzzleOperationIds.Shuffle => LensPlacement +
            "\nWith the lens active, drag to a neighboring symbol horizontally or vertically without holding Ctrl.",
        PuzzleOperationIds.ColumnSwap => LensPlacement +
            "\nWith the lens active, hold Ctrl and drag horizontally to the neighboring symbol.",
        PuzzleOperationIds.RowSwap => LensPlacement +
            "\nWith the lens active, hold Ctrl and drag vertically to the neighboring symbol.",
        _ => null
    };

    public RectangleF Layout(RectangleF area, CrystalCommandMenu menu)
    {
        float gap = Math.Min(12, Math.Min(area.Width, area.Height) * 0.04f);
        const float maxPanelWidth = 390;
        bool beside = area.Width >= menu.PreferredWidth + 250 + 3 * gap;
        float panelWidth = Math.Max(1, Math.Min(maxPanelWidth,
            area.Width - (beside ? menu.PreferredWidth + 3 * gap : 2 * gap)));
        // Reserve space for the longest help text so highlighting cannot move the menu under the pointer.
        float panelHeight = panels.Select(panel => panel?.Measure(panelWidth).Height ?? 0).DefaultIfEmpty().Max();
        panelHeight = Math.Max(1, Math.Min(panelHeight, area.Height * (beside ? 0.9f : 0.55f)));
        if (beside)
        {
            float left = area.Left + (area.Width - menu.PreferredWidth - gap - panelWidth) / 2;
            var menuArea = new RectangleF(left - gap, area.Top, menu.PreferredWidth + 2 * gap, area.Height);
            if (Current != null)
                Current.Bounds = new RectangleF(left + menu.PreferredWidth + gap,
                    area.Top + (area.Height - panelHeight) / 2, panelWidth, panelHeight);
            return menu.GetCenteredBounds(menuArea);
        }

        var menuBounds = menu.GetCenteredBounds(new RectangleF(area.Left, area.Top + panelHeight + gap,
            area.Width, Math.Max(1, area.Height - panelHeight - gap)));
        float top = Math.Max(area.Top, menuBounds.Top - gap - panelHeight);
        if (Current != null)
            Current.Bounds = new RectangleF(area.Left + (area.Width - panelWidth) / 2, top, panelWidth, panelHeight);
        return menuBounds;
    }
}
