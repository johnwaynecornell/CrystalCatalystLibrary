using CrystalSkia.net;

namespace SlideScramble;

public partial class Window
{
    private bool rulesMenuVisible;

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
        ShowRulesList("Rule presets", new[]
        {
            ("Classic", (Action)(() => SelectPreset(PuzzleRuleSet.CreateClassic(activeRules.Registry)))),
            ("ShuffleLogic", (Action)(() => SelectPreset(PuzzleRuleSet.CreateShuffleLogic(activeRules.Registry)))),
            ("Hybrid", (Action)(() => SelectPreset(PuzzleRuleSet.CreateHybrid(activeRules.Registry)))),
            ("Back", (Action)ShowRulesMenu)
        });
    }

    private void SelectPreset(PuzzleRuleSet rules)
    {
        if (rules.Name == "ShuffleLogic" && (MyGrid.Width < 3 || MyGrid.Height < 3))
        {
            showMedia = true;
            sessionMessage = "ShuffleLogic requires at least a 3 × 3 board";
            return;
        }
        ApplyConfiguredRules(rules, false);
    }

    private void ShowRuleOperations(bool scramble)
    {
        var allowed = scramble ? activeRules.ScrambleAllowed : activeRules.SolveAllowed;
        var entries = activeRules.Registry.RegisteredOperationIds.Select(id =>
            ($"{id}: {(allowed.Contains(id) ? "on" : "off")}", (Action)(() =>
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
            ($"{id}: {(activeRules.UIEnabled.Contains(id) ? "on" : "off")}", (Action)(() =>
            {
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
            ($"{id}: {(sessions.RuntimeUIEnabled.Contains(id) ? "on" : "off")}", (Action)(() =>
            {
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
        if (sessions.Mode != RecordingControllerMode.Idle || State.Mode == GameMode.Scrambling ||
            drag != null || Animations.Count != 0 || boardDraft != null || activeSlider != ActiveSlider.None)
        {
            showMedia = true;
            sessionMessage = "Stop recording or playback and finish the current activity before changing rules";
            return false;
        }

        var liveInput = sessions.RuntimeUIEnabled.ToArray();
        savedSession ??= sessions.Playback;
        activeRules = rules.Snapshot();
        sessions = new SessionRecordingController(MyGrid, activeRules);
        if (preserveLiveInput)
            foreach (string id in activeRules.Registry.RegisteredOperationIds)
                sessions.SetRuntimeUIEnabled(id, liveInput.Contains(id));
        goal = null;
        lensInteraction.Cancel();
        lensDoubleClick.Reset();
        moveHighlight = null;
        sessionMessage = $"{rules.Name} rules selected; board unchanged";
        wnd.QueueRedraw();
        return true;
    }

    private void ShowRulesList(string title, IEnumerable<(string Label, Action Execute)> commands)
    {
        var items = commands.ToArray();
        var menu = new CrystalCommandMenu(title, items.Select(item => new CrystalMenuEntry(item.Label)), hudFont);
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
        popupHost.SetLayout(area, menu.GetCenteredBounds(area));
        wnd.QueueRedraw();
    }
}