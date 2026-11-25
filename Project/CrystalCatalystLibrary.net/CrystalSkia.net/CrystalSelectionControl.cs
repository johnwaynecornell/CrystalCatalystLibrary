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
    private readonly bool[] buttonEnabled;
    private readonly CrystalTextEditor editor;
    private readonly CrystalScrollBar scrollBar;
    private int selectedIndex = -1;
    private int hoveredIndex = -1;
    private int hoveredButton = -1;
    private int focusedButton = -1;
    private RectangleF bounds;
    private bool scrollBarPointerDown;

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

    /// <summary>Raised when the resolved selection changes, with the editor text and matching index, or -1 if unmatched.</summary>
    public event Action<string, int>? SelectionHint;

    /// <summary>Raised when the resolved selection changes.</summary>
    public event Action? SelectionChanged;

    /// <summary>Sets whether a button can be activated.</summary>
    public void SetButtonEnabled(int index, bool enabled)
    {
        if ((uint)index >= (uint)buttonEnabled.Length) throw new ArgumentOutOfRangeException(nameof(index));
        buttonEnabled[index] = enabled;
        if (!enabled && hoveredButton == index) hoveredButton = -1;
        if (!enabled && focusedButton == index)
        {
            focusedButton = -1;
            editor.SetFocus(true);
        }
    }

    /// <summary>Returns whether a button can be activated.</summary>
    public bool IsButtonEnabled(int index)
    {
        if ((uint)index >= (uint)buttonEnabled.Length) throw new ArgumentOutOfRangeException(nameof(index));
        return buttonEnabled[index];
    }

    /// <summary>
    /// Normalized scroll offset in the range [0.0, 1.0].
    /// 0.0 means scrolled to the start, 1.0 means scrolled to the end.
    /// Readable by the consumer.
    /// </summary>
    public float ScrollOffset => scrollBar.Offset;

    /// <summary>Whether the editor or an action button currently has focus.</summary>
    public bool IsFocused => editor.IsFocused || focusedButton >= 0;

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
        this.bounds = bounds;
        this.items = new List<string>(items);
        this.buttonLabels = new List<string>(buttonLabels);
        buttonEnabled = Enumerable.Repeat(true, this.buttonLabels.Count).ToArray();
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

        UpdateScrollBarViewport();
        UpdateSelectedIndex();
    }

    public RectangleF Bounds
    {
        get => bounds;
        set
        {
            bounds = value;
            editor.Bounds = new RectangleF(Padding, Padding, Math.Max(0, value.Width - Padding * 2 - ScrollBarWidth), EditorHeight);
            UpdateScrollBarViewport();
        }
    }

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

    private int VisibleCount()
    {
        float available = Bounds.Height - (Padding * 2 + EditorHeight + SectionGap * 2
            + (buttonLabels.Count > 0 ? ButtonHeight : 0));
        int heightCount = (int)(Math.Max(0f, available) / ItemHeight);
        return Math.Min(items.Count, heightCount);
    }

    private void UpdateScrollBarViewport()
    {
        int visibleCount = VisibleCount();
        float listHeight = visibleCount * ItemHeight;
        scrollBar.Bounds = new RectangleF(Bounds.Width - Padding - ScrollBarWidth,
            Padding + EditorHeight + SectionGap, ScrollBarWidth, listHeight);

        if (items.Count == 0)
        {
            scrollBar.ViewportRatio = 1f;
            return;
        }

        scrollBar.ViewportRatio = (float)visibleCount / items.Count;
    }

    private float GetScrollOffsetPixels()
    {
        int visibleCount = VisibleCount();
        float listHeight = visibleCount * ItemHeight;
        float totalHeight = items.Count * ItemHeight;
        float availableScroll = Math.Max(0f, totalHeight - listHeight);
        return scrollBar.Offset * availableScroll;
    }

    private void SetScrollOffsetPixels(float pixels)
    {
        int visibleCount = VisibleCount();
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
        int resolvedIndex = -1;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == text)
            {
                resolvedIndex = i;
                break;
            }
        }
        if (selectedIndex == resolvedIndex) return;
        selectedIndex = resolvedIndex;
        SelectionHint?.Invoke(text, resolvedIndex);
        SelectionChanged?.Invoke();
    }

    public void SetFocus(bool focused)
    {
        focusedButton = -1;
        editor.SetFocus(focused);
    }

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
            int visibleCount = VisibleCount();
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

                    if (!buttonEnabled[i])
                    {
                        using var disabledPaint = new SKPaint { Color = ToSkColor(ButtonColor).WithAlpha(70), IsAntialias = true };
                        canvas.DrawRoundRect(btnRRect, disabledPaint);
                    }
                    else if (i == hoveredButton || i == focusedButton)
                        canvas.DrawRoundRect(btnRRect, buttonHoverPaint);
                    else
                        canvas.DrawRoundRect(btnRRect, buttonPaint);

                    string label = buttonLabels[i];
                    float labelX = btnRect.MidX - editor.Font.MeasureText(label) / 2;
                    float labelY = btnRect.Top + (btnRect.Height - editor.Font.Metrics.Ascent) / 2 - editor.Font.Metrics.Descent;
                    if (buttonEnabled[i])
                        canvas.DrawText(label, labelX, labelY, editor.Font, textPaint);
                    else
                    {
                        using var disabledTextPaint = new SKPaint { Color = ToSkColor(TextColor).WithAlpha(100), IsAntialias = true };
                        canvas.DrawText(label, labelX, labelY, editor.Font, disabledTextPaint);
                    }
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

        // Compute layout dynamically to stay in sync even before the first OnDraw
        float x = Padding;
        float y = Padding;
        float contentWidth = Bounds.Width - Padding * 2 - ScrollBarWidth;
        RectangleF editorBounds = new RectangleF(Padding, Padding, contentWidth, EditorHeight);

        // Compute scroll bar bounds dynamically
        RectangleF scrollBarBounds = scrollBar.Bounds;

        // Check scroll bar first
        if (scrollBar.Visible && scrollBarBounds.Contains(localX, localY))
        {
            scrollBarPointerDown = true;
            PointF scrollLocal = new(localX - scrollBarBounds.X, localY - scrollBarBounds.Y);
            scrollBar.PointerDown(scrollLocal, button);
            return;
        }

        // Check editor
        if (editorBounds.Contains(localX, localY))
        {
            if (focusedButton >= 0) editor.SetFocus(true);
            focusedButton = -1;
            PointF editorLocal = new(localX - editorBounds.X, localY - editorBounds.Y);
            editor.PointerDown(editorLocal, button);
            return;
        }

        y += EditorHeight + SectionGap;

        // Compute button position first — buttons are drawn on top, so check them first
        int visibleCount = VisibleCount();
        float listHeight = visibleCount * ItemHeight;
        float buttonsY = y + listHeight + SectionGap;

        // Check buttons (drawn on top of list area)
        if (buttonLabels.Count > 0)
        {
            float buttonWidth = (contentWidth - (buttonLabels.Count - 1) * ButtonGap) / buttonLabels.Count;
            for (int i = 0; i < buttonLabels.Count; i++)
            {
                float btnX = x + i * (buttonWidth + ButtonGap);
                float btnY = buttonsY;
                var btnRect = new SKRect(btnX, btnY, btnX + buttonWidth, btnY + ButtonHeight);
                if (btnRect.Contains(localX, localY))
                {
                    if (buttonEnabled[i])
                        ButtonActivated?.Invoke(new SelectionButtonEventArgs(buttonLabels[i], editor.Text, selectedIndex));
                    return;
                }
            }
        }

        // Check list items (clipped to list area)
        float scrollPixels = GetScrollOffsetPixels();
        float listBottom = y + listHeight;

        for (int i = 0; i < items.Count; i++)
        {
            float itemY = y + i * ItemHeight - scrollPixels;
            if (itemY + ItemHeight < y || itemY > listBottom)
                continue;

            var itemRect = new SKRect(x + 2, itemY + 2, x + contentWidth - 2, itemY + ItemHeight - 2);
            if (localY >= y && localY < listBottom && itemRect.Contains(localX, localY))
            {
                if (focusedButton >= 0)
                {
                    focusedButton = -1;
                    editor.SetFocus(true);
                }
                // Copy item text to editor
                editor.Text = items[i];
                UpdateSelectedIndex();
                return;
            }
        }
    }

    public void PointerMove(PointF position)
    {
        float localX = position.X;
        float localY = position.Y;

        // Compute layout dynamically to stay in sync even before the first OnDraw
        float x = Padding;
        float y = Padding;
        float contentWidth = Bounds.Width - Padding * 2 - ScrollBarWidth;
        RectangleF editorBounds = new RectangleF(Padding, Padding, contentWidth, EditorHeight);

        // Compute scroll bar bounds dynamically
        RectangleF scrollBarBounds = scrollBar.Bounds;

        // Forward to scroll bar if within its bounds
        if (scrollBarPointerDown || (scrollBar.Visible && scrollBarBounds.Contains(localX, localY)))
        {
            PointF scrollLocal = new(localX - scrollBarBounds.X, localY - scrollBarBounds.Y);
            scrollBar.PointerMove(scrollLocal);
            return;
        }

        // Forward to editor if focused and within bounds
        if (editor.IsFocused && editorBounds.Contains(localX, localY))
        {
            PointF editorLocal = new(localX - editorBounds.X, localY - editorBounds.Y);
            editor.PointerMove(editorLocal);
            return;
        }

        y += EditorHeight + SectionGap;

        // Update hovered button first — buttons are drawn on top
        int visibleCount = VisibleCount();
        float listHeight = visibleCount * ItemHeight;
        float buttonsY = y + listHeight + SectionGap;
        hoveredButton = -1;
        if (buttonLabels.Count > 0)
        {
            float buttonWidth = (contentWidth - (buttonLabels.Count - 1) * ButtonGap) / buttonLabels.Count;
            for (int i = 0; i < buttonLabels.Count; i++)
            {
                float btnX = x + i * (buttonWidth + ButtonGap);
                float btnY = buttonsY;
                var btnRect = new SKRect(btnX, btnY, btnX + buttonWidth, btnY + ButtonHeight);
                if (btnRect.Contains(localX, localY))
                {
                    hoveredButton = buttonEnabled[i] ? i : -1;
                    return;
                }
            }
        }

        // Update hovered item (clipped to list area)
        float scrollPixels = GetScrollOffsetPixels();
        hoveredIndex = -1;

        for (int i = 0; i < items.Count; i++)
        {
            float itemY = y + i * ItemHeight - scrollPixels;
            if (itemY + ItemHeight < y || itemY > y + listHeight)
                continue;

            var itemRect = new SKRect(x + 2, itemY + 2, x + contentWidth - 2, itemY + ItemHeight - 2);
            if (localY >= y && localY < y + listHeight && itemRect.Contains(localX, localY))
            {
                hoveredIndex = i;
                return;
            }
        }
    }

    public void PointerUp(PointF position, CrystalMouseButton button)
    {
        float localX = position.X;
        float localY = position.Y;

        // Compute scroll bar bounds dynamically
        RectangleF scrollBarBounds = scrollBar.Bounds;

        // Forward to scroll bar if within its bounds
        if (scrollBarPointerDown)
        {
            scrollBarPointerDown = false;
            PointF scrollLocal = new(localX - scrollBarBounds.X, localY - scrollBarBounds.Y);
            scrollBar.PointerUp(scrollLocal, button);
            return;
        }

        // Compute editor bounds dynamically to stay in sync even before the first OnDraw
        float contentWidth = Bounds.Width - Padding * 2 - ScrollBarWidth;
        RectangleF editorBounds = new RectangleF(Padding, Padding, contentWidth, EditorHeight);

        if (editor.IsFocused && editorBounds.Contains(localX, localY))
        {
            PointF editorLocal = new(localX - editorBounds.X, localY - editorBounds.Y);
            editor.PointerUp(editorLocal, button);
        }
    }

    public void KeyDown(KeyCode key, bool shift, bool control)
    {
        if (key == KeyCode.Tab)
        {
            int next = focusedButton;
            do
            {
                next += shift ? -1 : 1;
                if (next < -1) next = buttonLabels.Count - 1;
                if (next >= buttonLabels.Count) next = -1;
            } while (next >= 0 && !buttonEnabled[next]);
            focusedButton = next;
            editor.SetFocus(next < 0);
            return;
        }

        if (focusedButton >= 0)
        {
            if (key == KeyCode.Enter && buttonEnabled[focusedButton])
                ButtonActivated?.Invoke(new SelectionButtonEventArgs(buttonLabels[focusedButton], editor.Text, selectedIndex));
            return;
        }

        switch (key)
        {
            case KeyCode.Up:
                if (selectedIndex >= 0 && selectedIndex > 0)
                {
                    editor.Text = items[selectedIndex - 1];
                    AdjustScrollForSelection();
                }
                else if (selectedIndex < 0 && items.Count > 0)
                {
                    editor.Text = items[items.Count - 1];
                    AdjustScrollForSelection();
                }
                break;

            case KeyCode.Down:
                if (selectedIndex < items.Count - 1)
                {
                    editor.Text = items[selectedIndex + 1];
                    AdjustScrollForSelection();
                }
                break;

            case KeyCode.PageUp:
                {
                    if (items.Count == 0) break;
                    int visibleCount = Math.Max(1, VisibleCount());
                    int target = Math.Max(0, selectedIndex - visibleCount);
                    if (target < 0 && items.Count > 0) target = items.Count - visibleCount;
                    target = Math.Max(0, target);
                    editor.Text = items[target];
                    AdjustScrollForSelection();
                }
                break;

            case KeyCode.PageDown:
                {
                    if (items.Count == 0) break;
                    int visibleCount = Math.Max(1, VisibleCount());
                    int target = Math.Min(items.Count - 1, selectedIndex + visibleCount);
                    if (target < 0) target = 0;
                    editor.Text = items[target];
                    AdjustScrollForSelection();
                }
                break;

            case KeyCode.Home:
                if (items.Count > 0)
                {
                    editor.Text = items[0];
                    SetScrollOffsetPixels(0f);
                }
                break;

            case KeyCode.End:
                if (items.Count > 0)
                {
                    editor.Text = items[items.Count - 1];
                    AdjustScrollForSelection();
                }
                break;

            default:
                editor.KeyDown(key, shift, control);
                break;
        }
    }

    public void TextInput(string text)
    {
        if (focusedButton < 0) editor.TextInput(text);
    }

    private void AdjustScrollForSelection()
    {
        int visibleCount = VisibleCount();
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
        int visibleCount = VisibleCount();
        float listHeight = visibleCount * ItemHeight;
        float currentScroll = GetScrollOffsetPixels();
        SetScrollOffsetPixels(Math.Max(0f, currentScroll - listHeight));
    }

    /// <summary>Scroll down by one page.</summary>
    public void ScrollDown()
    {
        int visibleCount = VisibleCount();
        float listHeight = visibleCount * ItemHeight;
        float currentScroll = GetScrollOffsetPixels();
        float totalHeight = items.Count * ItemHeight;
        float maxScroll = Math.Max(0f, totalHeight - listHeight);
        SetScrollOffsetPixels(Math.Min(maxScroll, currentScroll + listHeight));
    }
}
