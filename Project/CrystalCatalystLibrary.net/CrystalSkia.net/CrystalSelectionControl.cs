using System.Drawing;
using CrystalCatalystLibrary.net;
using SkiaSharp;

namespace CrystalSkia.net;

/// <summary>
/// Event data raised when a button is activated in a <see cref="CrystalSelectionControl"/>.
/// </summary>
public sealed class SelectionButtonEventArgs
{
    /// <summary>The label of the activated button.</summary>
    public string ButtonLabel { get; }

    /// <summary>The current text in the editor at activation time.</summary>
    public string CurrentText { get; }

    /// <summary>The currently selected index, or -1 when the edited text does not match a list item.</summary>
    public int SelectedIndex { get; }

    public SelectionButtonEventArgs(string buttonLabel, string currentText, int selectedIndex)
    {
        ButtonLabel = buttonLabel;
        CurrentText = currentText;
        SelectedIndex = selectedIndex;
    }
}

/// <summary>
/// Reusable string-selection control with an embedded text editor, scrollable list, and action buttons.
/// Implements <see cref="ICrystalSkia"/> so it can be drawn and interacted with via <see cref="CrystalInteraction"/>.
/// </summary>
public sealed class CrystalSelectionControl : ICrystalSkia
{
    private readonly List<string> items;
    private readonly List<string> buttonLabels;
    private readonly CrystalTextEditor editor;
    private readonly CrystalScrollBar scrollBar;
    private int selectedIndex = -1;
    private int hoveredIndex = -1;
    private int hoveredButton = -1;

    // Layout constants
    private const float Padding = 8f;
    private const float ItemHeight = 28f;
    private const float ButtonHeight = 32f;
    private const float ButtonGap = 8f;
    private const float EditorHeight = 28f;
    private const float SectionGap = 8f;
    private const float CornerRadius = 6f;

    // Styling
    public Color BackgroundColor { get; set; } = Color.FromArgb(30, 41, 59);
    public Color ItemBackgroundColor { get; set; } = Color.FromArgb(51, 65, 85);
    public Color ItemSelectedColor { get; set; } = Color.FromArgb(56, 189, 248);
    public Color ItemHoverColor { get; set; } = Color.FromArgb(71, 85, 105);
    public Color TextColor { get; set; } = Color.FromArgb(241, 245, 249);
    public Color ButtonColor { get; set; } = Color.FromArgb(51, 65, 85, 180);
    public Color ButtonHoverColor { get; set; } = Color.FromArgb(71, 85, 105);

    /// <summary>Width of the scroll bar in pixels.</summary>
    public const float ScrollBarWidth = 10f;

    /// <summary>The embedded text editor.</summary>
    public CrystalTextEditor Editor => editor;

    /// <summary>Current result text from the editor.</summary>
    public string ResultText => editor.Text;

    /// <summary>Selected index in the list, or -1 when the edited text does not match an item.</summary>
    public int SelectedIndex => selectedIndex;

    /// <summary>
    /// Normalized scroll offset in the range [0.0, 1.0].
    /// 0.0 means scrolled to the start, 1.0 means scrolled to the end.
    /// Readable by the consumer.
    /// </summary>
    public float ScrollOffset => scrollBar.Offset;

    /// <summary>Whether this control currently has focus (delegated to the editor).</summary>
    public bool IsFocused => editor.IsFocused;

    /// <summary>Event raised when a button is activated.</summary>
    public event Action<SelectionButtonEventArgs>? ButtonActivated;

    /// <summary>
    /// Creates a new selection control.
    /// </summary>
    /// <param name="bounds">Overall bounds of the control.</param>
    /// <param name="items">Strings to display in the list.</param>
    /// <param name="initialText">Initial text value for the editor.</param>
    /// <param name="buttonLabels">Ordered button labels to display at the bottom.</param>
    /// <param name="font">Font for rendering text.</param>
    public CrystalSelectionControl(
        RectangleF bounds,
        IEnumerable<string> items,
        string initialText,
        IEnumerable<string> buttonLabels,
        SKFont font)
    {
        Bounds = bounds;
        this.items = new List<string>(items);
        this.buttonLabels = new List<string>(buttonLabels);
        ArgumentNullException.ThrowIfNull(font);

        editor = new CrystalTextEditor(
            new RectangleF(Padding, Padding, bounds.Width - Padding * 2 - ScrollBarWidth, EditorHeight),
            font);
        editor.Text = initialText ?? string.Empty;
        editor.Changed += OnEditorChanged;

        scrollBar = new CrystalScrollBar(
            new RectangleF(bounds.Width - Padding - ScrollBarWidth, Padding, ScrollBarWidth, bounds.Height - Padding * 2),
            ScrollBarOrientation.Vertical);
        scrollBar.Changed += OnScrollBarChanged;

        UpdateSelectedIndex();
    }

    public RectangleF Bounds { get; set; }

    /// <summary>Calculates the total height required for the given number of items and buttons.</summary>
    public static float CalculateHeight(int itemCount, int buttonCount, float width)
    {
        float listHeight = Math.Min(itemCount, MaxVisibleItems(width)) * ItemHeight;
        float buttonsHeight = buttonCount > 0 ? ButtonHeight : 0;
        return Padding * 2 + EditorHeight + SectionGap + listHeight + SectionGap + buttonsHeight;
    }

    private static int MaxVisibleItems(float width)
    {
        // Account for scroll bar width when calculating available width
        float effectiveWidth = width - Padding * 2 - ScrollBarWidth;
        return Math.Max(3, Math.Min(12, (int)((effectiveWidth > 0 ? effectiveWidth * 0.6f : 300) / ItemHeight)));
    }

    private void UpdateScrollBarViewport()
    {
        int visibleCount = Math.Min(items.Count, MaxVisibleItems(Bounds.Width));
        float totalHeight = items.Count * ItemHeight;
        float listHeight = visibleCount * ItemHeight;

        if (items.Count == 0)
        {
            scrollBar.ViewportRatio = 1f;
            return;
        }

        scrollBar.ViewportRatio = (float)visibleCount / items.Count;
    }

    private float GetScrollOffsetPixels()
    {
        int visibleCount = Math.Min(items.Count, MaxVisibleItems(Bounds.Width));
        float listHeight = visibleCount * ItemHeight;
        float totalHeight = items.Count * ItemHeight;
        float availableScroll = Math.Max(0f, totalHeight - listHeight);
        return scrollBar.Offset * availableScroll;
    }

    private void SetScrollOffsetPixels(float pixels)
    {
        int visibleCount = Math.Min(items.Count, MaxVisibleItems(Bounds.Width));
        float listHeight = visibleCount * ItemHeight;
        float totalHeight = items.Count * ItemHeight;
        float availableScroll = Math.Max(0f, totalHeight - listHeight);

        if (availableScroll <= 0f)
        {
            scrollBar.Offset = 0f;
            return;
        }

        scrollBar.Offset = Math.Clamp(pixels / availableScroll, 0f, 1f);
    }

    private void OnEditorChanged()
    {
        UpdateSelectedIndex();
    }

    private void OnScrollBarChanged()
    {
        // Scroll bar offset changed — the consumer can read ScrollOffset to know the position.
        // No internal action needed; the offset is applied during draw.
    }

    private void UpdateSelectedIndex()
    {
        string text = editor.Text;
        selectedIndex = -1;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == text)
            {
                selectedIndex = i;
                break;
            }
        }
    }

    public void SetFocus(bool focused) => editor.SetFocus(focused);

    public void OnIdle(double timeSeconds)
    {
        editor.OnIdle(timeSeconds);
        UpdateScrollBarViewport();
    }

    public void OnDraw(SKCanvas canvas)
    {
        UpdateScrollBarViewport();

        using var bgPaint = new SKPaint { Color = ToSkColor(BackgroundColor), IsAntialias = true };
        using var textPaint = new SKPaint { Color = ToSkColor(TextColor), IsAntialias = true };
        using var itemBgPaint = new SKPaint { Color = ToSkColor(ItemBackgroundColor), IsAntialias = true };
        using var itemSelectedPaint = new SKPaint { Color = ToSkColor(ItemSelectedColor), IsAntialias = true };
        using var itemHoverPaint = new SKPaint { Color = ToSkColor(ItemHoverColor), IsAntialias = true };
        using var buttonPaint = new SKPaint { Color = ToSkColor(ButtonColor), IsAntialias = true };
        using var buttonHoverPaint = new SKPaint { Color = ToSkColor(ButtonHoverColor), IsAntialias = true };

        canvas.Save();
        try
        {
            canvas.Translate(Bounds.X, Bounds.Y);
            var area = new SKRect(0, 0, Bounds.Width, Bounds.Height);
            canvas.ClipRect(area);

            // Background
            var bgRect = new SKRoundRect(area, CornerRadius, CornerRadius);
            canvas.DrawRoundRect(bgRect, bgPaint);

            float x = Padding;
            float y = Padding;
            float contentWidth = Bounds.Width - Padding * 2 - ScrollBarWidth;

            float scrollPixels = GetScrollOffsetPixels();

            // Editor
            if ((ICrystal)editor is ICrystalSkia editorSkia)
            {
                editor.Bounds = new RectangleF(x, y, contentWidth, EditorHeight);
                editorSkia.OnDraw(canvas);
            }

            y += EditorHeight + SectionGap;

            // List area
            int visibleCount = Math.Min(items.Count, MaxVisibleItems(Bounds.Width));
            float listHeight = visibleCount * ItemHeight;

            // Draw list items
            canvas.Save();
            var listClip = new SKRect(x, y, x + contentWidth, y + listHeight);
            canvas.ClipRect(listClip);

            for (int i = 0; i < items.Count; i++)
            {
                float itemY = y + i * ItemHeight - scrollPixels;
                if (itemY + ItemHeight < y || itemY > y + listHeight)
                    continue; // Outside visible area

                var itemRect = new SKRect(x + 2, itemY + 2, x + contentWidth - 2, itemY + ItemHeight - 2);
                var itemRRect = new SKRoundRect(itemRect, 4f, 4f);

                // Background
                if (i == selectedIndex)
                    canvas.DrawRoundRect(itemRRect, itemSelectedPaint);
                else if (i == hoveredIndex)
                    canvas.DrawRoundRect(itemRRect, itemHoverPaint);
                else
                    canvas.DrawRoundRect(itemRRect, itemBgPaint);

                // Text
                string text = items[i];
                float textX = itemRect.Left + 6;
                float textY = itemRect.Top + (itemRect.Height - editor.Font.Metrics.Ascent) / 2 - editor.Font.Metrics.Descent;
                canvas.DrawText(text, textX, textY, editor.Font, textPaint);
            }

            canvas.Restore();

            y += listHeight + SectionGap;

            // Buttons
            if (buttonLabels.Count > 0)
            {
                float buttonWidth = (contentWidth - (buttonLabels.Count - 1) * ButtonGap) / buttonLabels.Count;

                for (int i = 0; i < buttonLabels.Count; i++)
                {
                    float btnX = x + i * (buttonWidth + ButtonGap);
                    float btnY = y;
                    var btnRect = new SKRect(btnX, btnY, btnX + buttonWidth, btnY + ButtonHeight);
                    var btnRRect = new SKRoundRect(btnRect, 4f, 4f);

                    if (i == hoveredButton)
                        canvas.DrawRoundRect(btnRRect, buttonHoverPaint);
                    else
                        canvas.DrawRoundRect(btnRRect, buttonPaint);

                    string label = buttonLabels[i];
                    float labelX = btnRect.MidX - editor.Font.MeasureText(label) / 2;
                    float labelY = btnRect.Top + (btnRect.Height - editor.Font.Metrics.Ascent) / 2 - editor.Font.Metrics.Descent;
                    canvas.DrawText(label, labelX, labelY, editor.Font, textPaint);
                }
            }

            // Draw scroll bar
            if (scrollBar is ICrystalSkia scrollBarSkia)
            {
                scrollBarSkia.OnDraw(canvas);
            }
        }
        finally
        {
            canvas.Restore();
        }
    }

    private static SKColor ToSkColor(Color color) => new(color.R, color.G, color.B, color.A);

    public void PointerDown(PointF position, CrystalMouseButton button)
    {
        if (button != CrystalMouseButton.Left) return;

        float localX = position.X;
        float localY = position.Y;

        // Check scroll bar first
        if (scrollBar.Bounds.Contains(localX, localY))
        {
            PointF scrollLocal = new(localX - scrollBar.Bounds.X, localY - scrollBar.Bounds.Y);
            scrollBar.PointerDown(scrollLocal, button);
            return;
        }

        // Check editor first
        if (editor.Bounds.Contains(localX, localY))
        {
            editor.PointerDown(position, button);
            return;
        }

        float x = Padding;
        float y = Padding;
        float contentWidth = Bounds.Width - Padding * 2 - ScrollBarWidth;

        y += EditorHeight + SectionGap;

        // Check list items
        int visibleCount = Math.Min(items.Count, MaxVisibleItems(Bounds.Width));
        float listHeight = visibleCount * ItemHeight;
        float scrollPixels = GetScrollOffsetPixels();

        for (int i = 0; i < items.Count; i++)
        {
            float itemY = y + i * ItemHeight - scrollPixels;
            if (itemY + ItemHeight < y || itemY > y + listHeight)
                continue;

            var itemRect = new SKRect(x + 2, itemY + 2, x + contentWidth - 2, itemY + ItemHeight - 2);
            if (itemRect.Contains(localX, localY))
            {
                // Copy item text to editor
                editor.Text = items[i];
                UpdateSelectedIndex();
                return;
            }
        }

        y += listHeight + SectionGap;

        // Check buttons
        if (buttonLabels.Count > 0)
        {
            float buttonWidth = (contentWidth - (buttonLabels.Count - 1) * ButtonGap) / buttonLabels.Count;
            for (int i = 0; i < buttonLabels.Count; i++)
            {
                float btnX = x + i * (buttonWidth + ButtonGap);
                float btnY = y;
                var btnRect = new SKRect(btnX, btnY, btnX + buttonWidth, btnY + ButtonHeight);
                if (btnRect.Contains(localX, localY))
                {
                    ButtonActivated?.Invoke(new SelectionButtonEventArgs(buttonLabels[i], editor.Text, selectedIndex));
                    return;
                }
            }
        }
    }

    public void PointerMove(PointF position)
    {
        float localX = position.X;
        float localY = position.Y;

        // Forward to scroll bar if within its bounds
        if (scrollBar.Bounds.Contains(localX, localY))
        {
            PointF scrollLocal = new(localX - scrollBar.Bounds.X, localY - scrollBar.Bounds.Y);
            scrollBar.PointerMove(scrollLocal);
            return;
        }

        // Forward to editor if focused and within bounds
        if (editor.IsFocused && editor.Bounds.Contains(localX, localY))
        {
            editor.PointerMove(position);
            return;
        }

        float x = Padding;
        float y = Padding;
        float contentWidth = Bounds.Width - Padding * 2 - ScrollBarWidth;

        y += EditorHeight + SectionGap;

        // Update hovered item
        int visibleCount = Math.Min(items.Count, MaxVisibleItems(Bounds.Width));
        float listHeight = visibleCount * ItemHeight;
        float scrollPixels = GetScrollOffsetPixels();
        hoveredIndex = -1;

        for (int i = 0; i < items.Count; i++)
        {
            float itemY = y + i * ItemHeight - scrollPixels;
            if (itemY + ItemHeight < y || itemY > y + listHeight)
                continue;

            var itemRect = new SKRect(x + 2, itemY + 2, x + contentWidth - 2, itemY + ItemHeight - 2);
            if (itemRect.Contains(localX, localY))
            {
                hoveredIndex = i;
                return;
            }
        }

        y += listHeight + SectionGap;

        // Update hovered button
        hoveredButton = -1;
        if (buttonLabels.Count > 0)
        {
            float buttonWidth = (contentWidth - (buttonLabels.Count - 1) * ButtonGap) / buttonLabels.Count;
            for (int i = 0; i < buttonLabels.Count; i++)
            {
                float btnX = x + i * (buttonWidth + ButtonGap);
                float btnY = y;
                var btnRect = new SKRect(btnX, btnY, btnX + buttonWidth, btnY + ButtonHeight);
                if (btnRect.Contains(localX, localY))
                {
                    hoveredButton = i;
                    return;
                }
            }
        }
    }

    public void PointerUp(PointF position, CrystalMouseButton button)
    {
        // Forward to scroll bar if within its bounds
        if (scrollBar.Bounds.Contains(position.X, position.Y))
        {
            scrollBar.PointerUp(position, button);
            return;
        }

        if (editor.IsFocused && editor.Bounds.Contains(position.X, position.Y))
        {
            editor.PointerUp(position, button);
        }
    }

    public void KeyDown(KeyCode key, bool shift, bool control)
    {
        switch (key)
        {
            case KeyCode.Up:
                if (selectedIndex >= 0 && selectedIndex > 0)
                {
                    selectedIndex--;
                    editor.Text = items[selectedIndex];
                    AdjustScrollForSelection();
                }
                else if (selectedIndex < 0 && items.Count > 0)
                {
                    selectedIndex = items.Count - 1;
                    editor.Text = items[selectedIndex];
                    AdjustScrollForSelection();
                }
                break;

            case KeyCode.Down:
                if (selectedIndex < items.Count - 1)
                {
                    selectedIndex++;
                    editor.Text = items[selectedIndex];
                    AdjustScrollForSelection();
                }
                break;

            case KeyCode.PageUp:
                {
                    int visibleCount = MaxVisibleItems(Bounds.Width);
                    int target = Math.Max(0, selectedIndex - visibleCount);
                    if (target < 0 && items.Count > 0) target = items.Count - visibleCount;
                    target = Math.Max(0, target);
                    selectedIndex = target;
                    editor.Text = items[target];
                    AdjustScrollForSelection();
                }
                break;

            case KeyCode.PageDown:
                {
                    int visibleCount = MaxVisibleItems(Bounds.Width);
                    int target = Math.Min(items.Count - 1, selectedIndex + visibleCount);
                    if (target < 0) target = 0;
                    selectedIndex = target;
                    editor.Text = items[target];
                    AdjustScrollForSelection();
                }
                break;

            case KeyCode.Home:
                if (items.Count > 0)
                {
                    selectedIndex = 0;
                    editor.Text = items[0];
                    SetScrollOffsetPixels(0f);
                }
                break;

            case KeyCode.End:
                if (items.Count > 0)
                {
                    selectedIndex = items.Count - 1;
                    editor.Text = items[selectedIndex];
                    AdjustScrollForSelection();
                }
                break;

            default:
                editor.KeyDown(key, shift, control);
                break;
        }
    }

    public void TextInput(string text) => editor.TextInput(text);

    private void AdjustScrollForSelection()
    {
        int visibleCount = MaxVisibleItems(Bounds.Width);
        float listHeight = visibleCount * ItemHeight;

        if (selectedIndex < 0) return;

        float currentScroll = GetScrollOffsetPixels();
        float selectedItemTop = selectedIndex * ItemHeight;
        float selectedItemBottom = (selectedIndex + 1) * ItemHeight;

        float newScroll = currentScroll;
        if (selectedItemTop < currentScroll)
            newScroll = selectedItemTop;
        else if (selectedItemBottom > currentScroll + listHeight)
            newScroll = selectedItemBottom - listHeight;

        SetScrollOffsetPixels(newScroll);
    }

    /// <summary>Scroll up by one page.</summary>
    public void ScrollUp()
    {
        int visibleCount = MaxVisibleItems(Bounds.Width);
        float listHeight = visibleCount * ItemHeight;
        float currentScroll = GetScrollOffsetPixels();
        SetScrollOffsetPixels(Math.Max(0f, currentScroll - listHeight));
    }

    /// <summary>Scroll down by one page.</summary>
    public void ScrollDown()
    {
        int visibleCount = MaxVisibleItems(Bounds.Width);
        float listHeight = visibleCount * ItemHeight;
        float currentScroll = GetScrollOffsetPixels();
        float totalHeight = items.Count * ItemHeight;
        float maxScroll = Math.Max(0f, totalHeight - listHeight);
        SetScrollOffsetPixels(Math.Min(maxScroll, currentScroll + listHeight));
    }
}
