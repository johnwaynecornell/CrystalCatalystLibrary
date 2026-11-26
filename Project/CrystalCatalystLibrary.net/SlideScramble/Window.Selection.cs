using CrystalCatalystLibrary.net;
using CrystalOpenAL;
using CrystalOpenGL;
using CrystalSkia.net;
using Silk.NET.OpenGL;
using SkiaSharp;

namespace SlideScramble;

public partial class Window
{
    private PuzzleRuleSet? previewRules;

    private void ShowSessionSelectionPopup()
    {
        ShowSelectionPopup(SelectionKind.Session);
    }

    private void ShowSelectionPopup(SelectionKind kind)
    {
        if (popupHost.IsVisible) return;
        if (sessions.Mode == RecordingControllerMode.Recording || drag != null || Animations.Count != 0 ||
            State.Mode == GameMode.Scrambling || ShowGridSetup || activeSlider != ActiveSlider.None)
        {
            sessionMessage = "Stop recording and finish the current move or setup first";
            showMedia = true;
            return;
        }

        try
        {
            var names = kind == SelectionKind.Session
                ? (Directory.Exists(Path.Combine(AppContext.BaseDirectory, "sessions"))
                    ? Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "sessions"), "*.json")
                        .Select(Path.GetFileNameWithoutExtension).Where(n => n != null).Select(n => n!).ToArray()
                    : Array.Empty<string>())
                : (challengeCatalog = ChallengeCatalog.LoadWithUserChallenges()).Names.ToArray();
            Array.Sort(names, StringComparer.Ordinal);
            selectionKind = kind;
            selectionMessage = names.Length == 0 ? "No entries available" : "Select an entry";
            var control = new CrystalSelectionControl(new System.Drawing.RectangleF(0, 0, 360,
                    CrystalSelectionControl.CalculateHeight(names.Length, 3, 360)), names, "",
                new[] { "Load", "Moves", "Cancel" }, editorFont);
            control.SetButtonEnabled(0, false);
            control.SetButtonEnabled(1, false);
            control.SelectionHint += (_, _) => UpdateSelectionPreview();
            control.ButtonActivated += OnSelectionButtonActivated;
            selectionControl = control;
            interaction.SetFocus(null);
            wnd.GetSize(out int width, out int height);
            popupHost.Show(control, new System.Drawing.RectangleF(0, 0, width, height));
            LayoutSelectionPopup(width, height);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException
                                       or InvalidOperationException or NotSupportedException)
        {
            selectionKind = SelectionKind.None;
            ClearPreview();
            sessionMessage = ex.Message;
            showMedia = true;
        }
    }

    private SessionRecording ResolveSelection(string name)
    {
        SessionRecording recording;
        if (selectionKind == SelectionKind.Challenge)
        {
            if (challengeCatalog == null || !challengeCatalog.TryGet(name, out var challenge) || challenge == null)
                throw new InvalidOperationException("Challenge is unavailable.");
            recording = challenge;
        }
        else
        {
            string path = Path.Combine(AppContext.BaseDirectory, "sessions", name + ".json");
            if (new FileInfo(path).Length > 4_000_000)
                throw new InvalidOperationException("Session file is too large.");
            recording = SessionRecording.FromJson(File.ReadAllText(path));
        }

        if (recording.Width < 2 || recording.Height < 2 || recording.Width > 9 || recording.Height > 9)
            throw new InvalidOperationException("Board dimensions must be between 2 and 9.");
        return recording;
    }

    private static Grid CreateRecordingGrid(SessionRecording recording, out SessionRecordingController controller)
    {
        var grid = new Grid(recording.Width, recording.Height);
        controller = new SessionRecordingController(grid, recording.RuleSet);
        controller.LoadPlayback(recording);
        return grid;
    }

    private void ClearPreview()
    {
        previewRules = null;
        if (previewGrid != null)
        {
            foreach (var tile in previewGrid.Tiles) tile.Image.Dispose();
            previewGrid = null;
        }
    }

    private void UpdateSelectionPreview()
    {
        ClearPreview();
        var control = selectionControl!;
        control.SetButtonEnabled(0, false);
        control.SetButtonEnabled(1, false);
        selectionMessage = "Select an entry";
        if (control.SelectedIndex >= 0)
        {
            try
            {
                var recording = ResolveSelection(control.ResultText);
                previewGrid = CreateRecordingGrid(recording, out _);
                previewRules = recording.RuleSet.Snapshot();
                control.SetButtonEnabled(0, true);
                control.SetButtonEnabled(1, recording.Moves.Count > 0);
                selectionMessage = $"Starting board  Goal: {recording.GetVerifiedGoalMoveCost()?.ToString() ?? "?"}";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException
                                           or InvalidOperationException or System.Text.Json.JsonException
                                           or NotSupportedException)
            {
                selectionMessage = ex.Message;
            }
        }

        wnd.QueueRedraw();
    }

    private void OnSelectionButtonActivated(SelectionButtonEventArgs e)
    {
        if (e.ButtonLabel == "Cancel")
        {
            popupHost.Cancel();
            return;
        }

        if (e.SelectedIndex < 0) return;
        try
        {
            var recording = ResolveSelection(e.CurrentText);
            if (e.ButtonLabel == "Moves" && recording.Moves.Count == 0)
                throw new InvalidOperationException("This entry has no recorded moves.");
            int? selectedGoal = recording.GetVerifiedGoalMoveCost();
            var grid = CreateRecordingGrid(recording, out var controller);
            if (e.ButtonLabel == "Load") controller = new SessionRecordingController(grid, recording.RuleSet);
            MyGrid = grid;
            sessions = controller;
            activeRules = recording.RuleSet;
            lensInteraction.Cancel();
            savedSession = e.ButtonLabel == "Moves" ? recording : null;
            State.Reset();
            goal = selectedGoal;
            animationLockHoriz = null;
            animationTileOffset = 0;
            moveHighlight = null;
            UpdateTileView();
            if (selectionKind == SelectionKind.Session) filenameEditor.Text = e.CurrentText;
            sessionMessage = e.ButtonLabel == "Moves" ? "Moves loaded" : "Board loaded";
            popupHost.Accept(e.CurrentText, e.SelectedIndex);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException
                                       or InvalidOperationException or System.Text.Json.JsonException
                                       or NotSupportedException)
        {
            selectionMessage = ex.Message;
        }
    }

    private void OnPopupClosed(PopupResult result)
    {
        commandMenu = null;
        rulesMenuVisible = false;
        ClearPresetRulesPreview();
        challengeGenerator = null;
        continueRecordingDialog = null;
        selectionKind = SelectionKind.None;
        selectionControl = null;
        challengeCatalog = null;
        ClearPreview();
        selectionMessage = "";
        lastPlaybackTick = wnd.uptimeSeconds();
    }

    private void LayoutSelectionPopup(int width, int height)
    {
        if (selectionControl == null) return;
        float w = Math.Max(1, width), h = Math.Max(1, height);
        float margin = 12, gap = 12;
        float usableW = Math.Max(1, w - margin * 2), usableH = Math.Max(1, h - margin * 2 - 30);
        int cols = previewGrid?.Width ?? 4, rows = previewGrid?.Height ?? 4;
        float sideWidth = Math.Min(360, usableW * 0.48f);
        float sideTile = Math.Min(Math.Max(1, usableW - sideWidth - gap) / cols, usableH / rows);
        float stackHeight = Math.Min(240, usableH * 0.48f);
        float stackTile = Math.Min(usableW / cols, Math.Max(1, usableH - stackHeight - gap) / rows);
        bool side = usableW >= 400 && (sideTile >= stackTile * 0.85f || usableH < 300);
        System.Drawing.RectangleF popup;
        if (side)
        {
            popup = new System.Drawing.RectangleF(margin, margin, sideWidth, usableH);
            previewArea = new SKRect(margin + sideWidth + gap, margin, w - margin, margin + usableH);
        }
        else
        {
            popup = new System.Drawing.RectangleF(margin, margin, usableW, stackHeight);
            previewArea = new SKRect(margin, margin + stackHeight + gap, w - margin, margin + usableH);
        }

        popupHost.SetLayout(new System.Drawing.RectangleF(0, 0, w, h), popup);
    }

    private void DrawPreview(SKCanvas canvas)
    {
        if (previewGrid != null && previewArea.Width > 0 && previewArea.Height > 0)
        {
            var ruleLines = SelectionPreviewRules.Format(previewRules);
            var layout = SelectionPreviewLayout.Calculate(previewArea, previewGrid.Width, previewGrid.Height,
                ruleLines.Count);
            DrawPreviewRules(canvas, ruleLines, layout.RulesBounds);
            float size = layout.BoardBounds.Width / previewGrid.Width;
            float left = layout.BoardBounds.Left;
            float top = layout.BoardBounds.Top;
            for (int y = 0; y < previewGrid.Height; y++)
            for (int x = 0; x < previewGrid.Width; x++)
            {
                var image = previewGrid.Tiles[y, x].Image;
                canvas.DrawImage(image,
                    new SKRect(left + x * size, top + y * size, left + (x + 1) * size, top + (y + 1) * size),
                    tilePaint);
            }
        }

        canvas.Save();
        float messageX = popupHost.Bounds.Left + 4;
        float messageWidth = Math.Max(1, popupHost.Bounds.Width - 8);
        canvas.ClipRect(new SKRect(messageX, previewArea.Bottom + 2, messageX + messageWidth, previewArea.Bottom + 22));
        canvas.Translate(messageX, previewArea.Bottom + 16);
        canvas.Scale(Math.Min(1, messageWidth / Math.Max(1, footerFont.MeasureText(selectionMessage))), 1);
        canvas.DrawText(selectionMessage, 0, 0, footerFont, hudTextPaint);
        canvas.Restore();
    }

    private void DrawPreviewRules(SKCanvas canvas, IReadOnlyList<string> lines, SKRect header)
    {
        if (lines.Count == 0 || header.Width <= 0 || header.Height <= 0) return;

        using var glassFillPaint = new SKPaint
        {
            Color = new SKColor(15, 23, 42, 205),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        using var glassBorderPaint = new SKPaint
        {
            Color = new SKColor(148, 163, 184, 110),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
            IsAntialias = true
        };
        canvas.DrawRoundRect(header, 6, 6, glassFillPaint);
        canvas.DrawRoundRect(header, 6, 6, glassBorderPaint);

        float lineHeight = header.Height / lines.Count;
        canvas.Save();
        canvas.ClipRoundRect(new SKRoundRect(header, 6, 6));
        for (int i = 0; i < lines.Count; i++)
        {
            string line = lines[i];
            float textWidth = Math.Max(1, footerFont.MeasureText(line));
            float horizontalScale = Math.Min(1, Math.Max(1, header.Width - 12) / textWidth);
            float verticalScale = Math.Min(1, lineHeight / 14);
            canvas.Save();
            canvas.Translate(header.Left + 6, header.Top + i * lineHeight + lineHeight * 0.85f);
            canvas.Scale(horizontalScale, verticalScale);
            canvas.DrawText(line, 0, 0, footerFont, hudTextPaint);
            canvas.Restore();
        }

        canvas.Restore();
    }
}