using CrystalSkia.net;
using SkiaSharp;

namespace SlideScramble;

public partial class Window
{
    private bool rulesMenuVisible;
    private bool presetMenuVisible;
    private PuzzleRuleSet? presetPreviewRules;
    private InputHelpPreview? inputHelpPreview;
    private readonly AnsiSkiaRenderer inputHelpRenderer = new() { FontSize = 14 };

    private void ShowRulesMenu()
    {
        ShowRulesList("Rules and Input", new[]
        {
            ("Choose preset", (Action)ShowPresetMenu),
            ("Scramble permissions", (Action)(() => ShowRuleOperations(true))),
            ("Solve permissions", (Action)(() => ShowRuleOperations(false))),
            ("Initial input preferences", (Action)ShowInitialInputMenu),
            ("Live input preferences", (Action)ShowLiveInputMenu),
            ("Back to commands", (Action)ShowCommandMenu)
        });
    }

    private void ShowPresetMenu()
    {
        var presets = SelectionPreviewRules.CreatePresets(activeRules.Registry);
        var entries = presets.Select(preset => (preset.Name, (Action)(() => SelectPreset(preset)))).ToList();
        entries.Add(("Back", ShowRulesMenu));
        ShowRulesList("Rule presets", entries, index =>
        {
            presetPreviewRules = index >= 0 && index < presets.Count ? presets[index] : null;
            wnd.QueueRedraw();
        });
    }

    private void SelectPreset(PuzzleRuleSet rules) => ApplyConfiguredRules(rules, false);

    private void ShowRuleOperations(bool scramble)
    {
        var allowed = scramble ? activeRules.ScrambleAllowed : activeRules.SolveAllowed;
        var entries = SlidePermissions.MenuEntries(activeRules.Registry).Select(id =>
            ($"{id}: {(SlidePermissions.Includes(allowed, id) ? "on" : "off")}" +
             (IsLensOperationUnavailable(id) ? " (unavailable: requires 3 × 3)" : ""), (Action)(() =>
            {
                var ids = new HashSet<string>(allowed, StringComparer.Ordinal);
                SlidePermissions.Set(ids, id, !SlidePermissions.Includes(ids, id));
                var rules = new PuzzleRuleSet("Custom", scramble ? ids : activeRules.ScrambleAllowed,
                    scramble ? activeRules.SolveAllowed : ids, activeRules.UIEnabled, activeRules.Registry);
                if (ApplyConfiguredRules(rules, true)) ShowRuleOperations(scramble);
            }))).ToList();
        entries.Add(("Back", ShowRulesMenu));
        ShowRulesList(scramble ? "Scramble permissions" : "Solve permissions", entries);
    }

    private void ShowInitialInputMenu()
    {
        var operationIds = SlidePermissions.MenuEntries(activeRules.Registry).ToArray();
        var entries = operationIds.Select(id =>
            ($"{id}: {(SlidePermissions.Includes(activeRules.UIEnabled, id) ? "on" : "off")}" +
             (IsLensOperationUnavailable(id) ? " (unavailable: requires 3 × 3)" : ""), (Action)(() =>
            {
                if (IsLensOperationUnavailable(id) && !SlidePermissions.Includes(activeRules.UIEnabled, id))
                {
                    sessionMessage = "Portable Lens operations require a 3 × 3 board";
                    ShowInitialInputMenu();
                    return;
                }

                var ids = new HashSet<string>(activeRules.UIEnabled, StringComparer.Ordinal);
                SlidePermissions.Set(ids, id, !SlidePermissions.Includes(ids, id));
                if (ApplyConfiguredRules(activeRules.WithUIEnabled(ids), false)) ShowInitialInputMenu();
            }))).ToList();
        entries.Add(("Back", ShowRulesMenu));
        ShowRulesList("Initial input preferences", entries, inputOperations: operationIds);
    }

    private void ShowLiveInputMenu()
    {
        var operationIds = SlidePermissions.MenuEntries(sessions.RuleSet.Registry).ToArray();
        var entries = operationIds.Select(id =>
            ($"{id}: {(SlidePermissions.Includes(sessions.RuntimeUIEnabled, id) ? "on" : "off")}" +
             (IsLensOperationUnavailable(id) ? " (unavailable: requires 3 × 3)" : ""), (Action)(() =>
            {
                if (IsLensOperationUnavailable(id) && !SlidePermissions.Includes(sessions.RuntimeUIEnabled, id))
                {
                    sessionMessage = "Portable Lens operations require a 3 × 3 board";
                    ShowLiveInputMenu();
                    return;
                }

                bool enabled = !SlidePermissions.Includes(sessions.RuntimeUIEnabled, id);
                if (sessions.SetRuntimeUIEnabled(id, enabled))
                {
                    lensInteraction.CancelGesture();
                    lensDoubleClick.Reset();
                    sessionMessage = $"{id} input {(enabled ? "enabled" : "disabled")}";
                }
                else sessionMessage = $"Could not change {id} input";

                ShowLiveInputMenu();
            }))).ToList();
        entries.Add(("Back", ShowRulesMenu));
        ShowRulesList("Live input preferences", entries, inputOperations: operationIds);
    }

    private bool ApplyConfiguredRules(PuzzleRuleSet rules, bool preserveLiveInput)
    {
        if (!rules.IsBoardCompatible(MyGrid.Width, MyGrid.Height))
        {
            showMedia = true;
            sessionMessage = rules.GetBoardCompatibilityMessage(MyGrid.Width, MyGrid.Height) ??
                             "The selected rules are not compatible with this board";
            return false;
        }

        if (sessions.Mode != RecordingControllerMode.Idle || State.Mode == GameMode.Scrambling ||
            drag != null || Animations.Count != 0 || boardDraft != null || activeSlider != ActiveSlider.None)
        {
            showMedia = true;
            sessionMessage = "Stop recording or playback and finish the current activity before changing rules";
            return false;
        }

        bool mathematicalRulesChanged = !activeRules.HasSameMathematicalPermissions(rules);
        var liveInput = sessions.RuntimeUIEnabled.ToArray();
        savedSession = sessions.Playback ?? savedSession;
        activeRules = rules.Snapshot();
        sessions = new SessionRecordingController(MyGrid, activeRules);
        if (mathematicalRulesChanged) State.Reset();
        if (preserveLiveInput)
            foreach (string id in SlidePermissions.MenuEntries(activeRules.Registry))
                sessions.SetRuntimeUIEnabled(id, SlidePermissions.Includes(liveInput, id));
        goal = null;
        if (mathematicalRulesChanged) lensInteraction.Cancel();
        else lensInteraction.CancelGesture();
        lensDoubleClick.Reset();
        moveHighlight = null;
        sessionMessage = mathematicalRulesChanged
            ? $"{rules.Name} rules selected; board unchanged and run reset"
            : $"{rules.Name} rules selected; board unchanged";
        string? compatibilityMessage = activeRules.GetBoardCompatibilityMessage(MyGrid.Width, MyGrid.Height);
        if (compatibilityMessage != null) sessionMessage += $" · {compatibilityMessage}";
        wnd.QueueRedraw();
        return true;
    }

    private bool IsLensOperationUnavailable(string operationId) => !SlidePermissions.IsName(operationId) &&
                                                                   (MyGrid.Width < 3 || MyGrid.Height < 3);

    private void ShowRulesList(string title, IEnumerable<(string Label, Action Execute)> commands,
        Action<int>? selectionChanged = null, IEnumerable<string>? inputOperations = null)
    {
        var items = commands.ToArray();
        var menu = new CrystalCommandMenu(title, items.Select(item => new CrystalMenuEntry(item.Label)), hudFont);
        presetMenuVisible = selectionChanged != null;
        presetPreviewRules = null;
        if (selectionChanged != null) menu.SelectionChanged += selectionChanged;
        inputHelpPreview = inputOperations == null ? null : new InputHelpPreview(menu, inputOperations, inputHelpRenderer);
        if (inputHelpPreview != null) menu.SelectionChanged += _ => wnd.QueueRedraw();
        menu.Activated += index =>
        {
            popupHost.Accept(selectedIndex: index);
            items[index].Execute();
        };
        commandMenu = menu;
        rulesMenuVisible = true;
        wnd.GetSize(out int width, out int height);
        var area = new System.Drawing.RectangleF(0, 0, width, height);
        popupHost.Show(menu, area);
        var menuBounds = inputHelpPreview != null ? inputHelpPreview.Layout(area, menu) : presetMenuVisible
            ? PresetMenuPreviewLayout.Calculate(new SKRect(0, 0, width, height), menu.PreferredWidth,
                menu.PreferredHeight, SelectionPreviewRules.PresetPreviewLineCount).MenuBounds
            : menu.GetCenteredBounds(area);
        popupHost.SetLayout(area, menuBounds);
        wnd.QueueRedraw();
    }

    private void ClearPresetRulesPreview()
    {
        presetMenuVisible = false;
        presetPreviewRules = null;
    }
}