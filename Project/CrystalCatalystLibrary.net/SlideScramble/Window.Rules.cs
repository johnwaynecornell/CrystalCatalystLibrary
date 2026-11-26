using CrystalSkia.net;
using SkiaSharp;

namespace SlideScramble;

public partial class Window
{
    private bool rulesMenuVisible;
    private bool presetMenuVisible;
    private PuzzleRuleSet? presetPreviewRules;

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
        var entries = activeRules.Registry.RegisteredOperationIds.Select(id =>
            ($"{id}: {(allowed.Contains(id) ? "on" : "off")}" +
             (IsLensOperationUnavailable(id) ? " (unavailable: requires 3 × 3)" : ""), (Action)(() =>
            {
                var ids = new HashSet<string>(allowed, StringComparer.Ordinal);
                if (!ids.Add(id)) ids.Remove(id);
                var rules = new PuzzleRuleSet("Custom", scramble ? ids : activeRules.ScrambleAllowed,
                    scramble ? activeRules.SolveAllowed : ids, activeRules.UIEnabled, activeRules.Registry);
                if (ApplyConfiguredRules(rules, true)) ShowRuleOperations(scramble);
            }))).ToList();
        entries.Add(("Back", ShowRulesMenu));
        ShowRulesList(scramble ? "Scramble permissions" : "Solve permissions", entries);
    }

    private void ShowInitialInputMenu()
    {
        var entries = activeRules.Registry.RegisteredOperationIds.Select(id =>
            ($"{id}: {(activeRules.UIEnabled.Contains(id) ? "on" : "off")}" +
             (IsLensOperationUnavailable(id) ? " (unavailable: requires 3 × 3)" : ""), (Action)(() =>
            {
                if (IsLensOperationUnavailable(id) && !activeRules.UIEnabled.Contains(id))
                {
                    sessionMessage = "Portable Lens operations require a 3 × 3 board";
                    ShowInitialInputMenu();
                    return;
                }

                var ids = new HashSet<string>(activeRules.UIEnabled, StringComparer.Ordinal);
                if (!ids.Add(id)) ids.Remove(id);
                if (ApplyConfiguredRules(activeRules.WithUIEnabled(ids), false)) ShowInitialInputMenu();
            }))).ToList();
        entries.Add(("Back", ShowRulesMenu));
        ShowRulesList("Initial input preferences", entries);
    }

    private void ShowLiveInputMenu()
    {
        var entries = sessions.RuleSet.Registry.RegisteredOperationIds.Select(id =>
            ($"{id}: {(sessions.RuntimeUIEnabled.Contains(id) ? "on" : "off")}" +
             (IsLensOperationUnavailable(id) ? " (unavailable: requires 3 × 3)" : ""), (Action)(() =>
            {
                if (IsLensOperationUnavailable(id) && !sessions.RuntimeUIEnabled.Contains(id))
                {
                    sessionMessage = "Portable Lens operations require a 3 × 3 board";
                    ShowLiveInputMenu();
                    return;
                }

                bool enabled = !sessions.RuntimeUIEnabled.Contains(id);
                if (sessions.SetRuntimeUIEnabled(id, enabled))
                {
                    lensInteraction.CancelGesture();
                    lensDoubleClick.Reset();
                    if (!HasRuntimeLensOperation) lensInteraction.Cancel();
                    sessionMessage = $"{id} input {(enabled ? "enabled" : "disabled")}";
                }
                else sessionMessage = $"Could not change {id} input";

                ShowLiveInputMenu();
            }))).ToList();
        entries.Add(("Back", ShowRulesMenu));
        ShowRulesList("Live input preferences", entries);
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
            foreach (string id in activeRules.Registry.RegisteredOperationIds)
                sessions.SetRuntimeUIEnabled(id, liveInput.Contains(id));
        goal = null;
        lensInteraction.Cancel();
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

    private bool IsLensOperationUnavailable(string operationId) => operationId != PuzzleOperationIds.Slide &&
                                                                   (MyGrid.Width < 3 || MyGrid.Height < 3);

    private void ShowRulesList(string title, IEnumerable<(string Label, Action Execute)> commands,
        Action<int>? selectionChanged = null)
    {
        var items = commands.ToArray();
        var menu = new CrystalCommandMenu(title, items.Select(item => new CrystalMenuEntry(item.Label)), hudFont);
        presetMenuVisible = selectionChanged != null;
        presetPreviewRules = null;
        if (selectionChanged != null) menu.SelectionChanged += selectionChanged;
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
        var menuBounds = presetMenuVisible
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