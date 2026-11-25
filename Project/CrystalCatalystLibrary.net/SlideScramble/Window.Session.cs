using CrystalCatalystLibrary.net;
using CrystalOpenAL;
using CrystalOpenGL;
using CrystalSkia.net;
using Silk.NET.OpenGL;
using SkiaSharp;

namespace SlideScramble;

public partial class Window
{
    private class MediaLayout
    {
        public float X, Y, Width, Height, Scale;
        public int Columns;

        public SKRect Button(int index)
        {
            float buttonWidth = (Width - 24) / Columns;
            float left = 12 + index % Columns * buttonWidth;
            float top = 68 + index / Columns * 30;
            return new SKRect(X + left * Scale, Y + top * Scale,
                X + (left + buttonWidth - 3) * Scale, Y + (top + 25) * Scale);
        }
    }

    private MediaLayout GetMediaLayout(int width, int height)
    {
        float w = Math.Max(1, width), h = Math.Max(1, height);
        float top = Math.Min(58, h / 3), bottom = Math.Max(top + 1, h - Math.Min(36, h / 3));
        float left = Math.Clamp(tileView!.xx - 8, 0, w);
        float right = Math.Clamp(tileView.xx + tileView.width + 8, 0, w);
        float boardTop = Math.Clamp(tileView.yy - 8, top, bottom);
        float boardBottom = Math.Clamp(tileView.yy + tileView.height + 8, top, bottom);
        SKRect[] gutters =
        {
            new SKRect(0, top, left, bottom), new SKRect(right, top, w, bottom),
            new SKRect(0, top, w, boardTop), new SKRect(0, boardBottom, w, bottom)
        };
        SKRect gutter = gutters.OrderByDescending(r => r.Width * r.Height).First();
        float panelWidth = Math.Min(320, Math.Max(40, w - 16));
        if (gutter.Width >= 160) panelWidth = Math.Min(panelWidth, gutter.Width - 8);
        int columns = Math.Clamp((int)((panelWidth - 24) / 65), 1, mediaLabels.Length);
        float panelHeight = 68 + (float)Math.Ceiling((double)mediaLabels.Length / columns) * 30 + 34;
        float scale = Math.Min(1, Math.Min(w / panelWidth, Math.Max(1, bottom - top) / panelHeight));
        float x = Math.Clamp(gutter.MidX - panelWidth * scale / 2, 0, Math.Max(0, w - panelWidth * scale));
        float y = Math.Clamp(gutter.MidY - panelHeight * scale / 2, top, Math.Max(top, bottom - panelHeight * scale));
        return new MediaLayout
            { X = x, Y = y, Width = panelWidth, Height = panelHeight, Scale = scale, Columns = columns };
    }

    private void UpdateMediaEditorBounds(MediaLayout layout)
    {
        editorFont.Size = hudFont.Size * layout.Scale;
        filenameEditor.Bounds = new System.Drawing.RectangleF(layout.X + 12 * layout.Scale,
            layout.Y + 34 * layout.Scale,
            Math.Max(1, layout.Width - 24) * layout.Scale, 24 * layout.Scale);
    }

    private void DrawMedia(SKCanvas canvas, MediaLayout layout)
    {
        canvas.Save();
        canvas.Translate(layout.X, layout.Y);
        canvas.Scale(layout.Scale);
        var panel = new SKRoundRect(new SKRect(0, 0, layout.Width, layout.Height), 8, 8);
        canvas.DrawRoundRect(panel, hudBarPaint);
        canvas.DrawRoundRect(panel, hudChipBorderPaint);
        canvas.DrawText("Session  [R] close", 12, 23, hudBoldFont, hudTextPaint);
        for (int i = 0; i < mediaLabels.Length; i++)
        {
            var screenRect = layout.Button(i);
            var buttonRect = new SKRect((screenRect.Left - layout.X) / layout.Scale,
                (screenRect.Top - layout.Y) / layout.Scale,
                (screenRect.Right - layout.X) / layout.Scale, (screenRect.Bottom - layout.Y) / layout.Scale);
            canvas.DrawRoundRect(new SKRoundRect(buttonRect, 4, 4), hudChipPaint);
            string label = i == 0 && sessions.Mode == RecordingControllerMode.Recording ? "Stop Rec" :
                i == 6 && sessions.IsAutoplaying ? "Pause" : mediaLabels[i];
            canvas.Save();
            canvas.ClipRect(buttonRect);
            canvas.DrawText(label, buttonRect.MidX - footerFont.MeasureText(label) / 2, buttonRect.Top + 17, footerFont,
                hudTextPaint);
            canvas.Restore();
        }

        canvas.Save();
        canvas.ClipRect(new SKRect(8, layout.Height - 30, layout.Width - 8, layout.Height));
        canvas.DrawText($"{sessions.Mode}  {FormatPrimitivePosition(sessions.Position, sessions.MoveCount)}", 12,
            layout.Height - 18, footerFont, hudMutedPaint);
        canvas.DrawText(sessionMessage, 12, layout.Height - 4, footerFont, hudMutedPaint);
        canvas.Restore();
        canvas.Restore();
        if ((ICrystal)filenameEditor is ICrystalSkia drawable) drawable.OnDraw(canvas);
    }

    private string SessionPath()
    {
        string name = filenameEditor.Text.Trim();
        if (name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) name = name[..^5];
        if (name.Length == 0 || name.Length > 100 || name == "." || name == ".." ||
            name.Any(c => char.IsControl(c) || "<>:\"/\\|?*".Contains(c)) || name.EndsWith('.'))
            throw new ArgumentException("Enter a valid filename without a path.");
        return Path.Combine(AppContext.BaseDirectory, "sessions", name + ".json");
    }

    private void ReverseSession()
    {
        if (ShowGridSetup)
        {
            showMedia = true;
            sessionMessage = "Close grid setup before reversing";
            return;
        }

        if (sessions.Mode == RecordingControllerMode.Recording)
        {
            showMedia = true;
            sessionMessage = "Stop recording before reversing";
            return;
        }

        if (drag != null || Animations.Count != 0 || State.Mode == GameMode.Scrambling)
        {
            showMedia = true;
            sessionMessage = "Finish the current move first";
            return;
        }

        var source = savedSession ?? sessions.Playback;
        if (source == null)
        {
            showMedia = true;
            sessionMessage = "No session to reverse";
            return;
        }

        var reversed = source.Reverse();
        int? reversedGoal = reversed.GetVerifiedGoalMoveCost();
        var grid = CreateRecordingGrid(reversed, out var controller);
        MyGrid = grid;
        sessions = controller;
        activeRules = reversed.RuleSet;
        lensInteraction.Cancel();
        savedSession = reversed;
        goal = reversedGoal;
        State.Reset();
        animationLockHoriz = null;
        animationTileOffset = 0;
        moveHighlight = null;
        UpdateTileView();
        sessionMessage = "Time slides both ways! Recording reversed";
        wnd.QueueRedraw();
    }

    private void MediaAction(int index)
    {
        if (index != 2 && index != 8) State.TakeBackSolved(wnd.uptimeSeconds());

        if (drag != null || Animations.Count != 0 || State.Mode == GameMode.Scrambling)
        {
            sessionMessage = "Finish the current move first";
            return;
        }

        try
        {
            if (index == 8 && (ShowGridSetup || popupHost.IsVisible))
                throw new InvalidOperationException("Close setup and selectors before continuing.");
            switch (index)
            {
                case 0:
                    if (sessions.Mode == RecordingControllerMode.Recording)
                    {
                        savedSession = sessions.StopRecording();
                        sessionMessage = "Recording stopped";
                    }
                    else if (sessions.Mode == RecordingControllerMode.Idle)
                    {
                        sessions.StartRecording();
                        sessionMessage = "Recording";
                    }

                    break;
                case 1:
                    if (sessions.Mode == RecordingControllerMode.Recording)
                        throw new InvalidOperationException("Stop recording before saving.");
                    var toSave = savedSession ??
                                 sessions.Playback ?? throw new InvalidOperationException("No session to save.");
                    string savePath = SessionPath();
                    Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
                    File.WriteAllText(savePath, toSave.ToJson());
                    sessionMessage = "Session saved";
                    break;
                case 2:
                    if (sessions.Mode == RecordingControllerMode.Recording)
                        throw new InvalidOperationException("Stop recording before loading.");
                    ShowSessionSelectionPopup();
                    break;
                case 3:
                    ResumePlaybackIfStopped();
                    sessions.ResetPlayback();
                    moveHighlight = null;
                    break;
                case 4:
                    ResumePlaybackIfStopped();
                    QueuePlaybackAnimation(false);
                    break;
                case 5:
                    ResumePlaybackIfStopped();
                    sessions.SetAutoplay(false);
                    QueuePlaybackAnimation(true);
                    break;
                case 6:
                    ResumePlaybackIfStopped();
                    sessions.SetAutoplay(!sessions.IsAutoplaying);
                    lastPlaybackTick = wnd.uptimeSeconds();
                    break;
                case 7:
                    if (sessions.Mode == RecordingControllerMode.Playback) sessions.StopPlayback();
                    moveHighlight = null;
                    break;
                case 8:
                    ShowContinueRecordingDialog();
                    break;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException
                                       or InvalidOperationException or System.Text.Json.JsonException
                                       or NotSupportedException)
        {
            sessionMessage = ex.Message;
        }
    }

    private void ConfirmContinueRecording(SessionRecordingController expectedController, int expectedPosition)
    {
        // The dialog blocks other interactions, but verify its captured context
        // before making the one-way change to the working recording.
        if (!ReferenceEquals(sessions, expectedController) ||
            sessions.Mode != RecordingControllerMode.Playback ||
            sessions.Position != expectedPosition ||
            !sessions.IsOperationBoundary(expectedPosition) ||
            drag != null || Animations.Count != 0 || State.Mode == GameMode.Scrambling)
        {
            sessionMessage = "Recording position changed; reopen Continue.";
            popupHost.Cancel();
            return;
        }

        popupHost.Accept();
        int prefixCost = sessions.Playback!.Operations.Take(sessions.Playback.GetOperationCountAtBoundary(expectedPosition))
            .Sum(operation => operation.MoveCost ?? 0);
        int prefixCount = sessions.ContinueRecordingFromHere();
        savedSession = null; // Never save an obsolete, unmodified recording.
        goal = null;
        State.BeginContinuedRun(prefixCost, wnd.uptimeSeconds());
        moveHighlight = null;
        sessionMessage = FormatContinueRecordingStatus(prefixCount);
        wnd.QueueRedraw();
    }

    private void ResumePlaybackIfStopped()
    {
        if (sessions.Mode != RecordingControllerMode.Idle) return;
        if (sessions.Playback == null && savedSession != null)
        {
            MyGrid = CreateRecordingGrid(savedSession, out var controller);
            sessions = controller;
            activeRules = savedSession.RuleSet;
            lensInteraction.Cancel();
            UpdateTileView();
            goal = savedSession.GetVerifiedGoalMoveCost();
            return;
        }
        sessions.ResumePlayback();
        goal = sessions.Playback!.GetVerifiedGoalMoveCost();
    }

    private void QueuePlaybackAnimation(bool forward)
    {
        if (sessions.Mode != RecordingControllerMode.Playback)
            throw new InvalidOperationException("Not in playback.");
        if (!forward) sessions.SetAutoplay(false);
        var playback = sessions.Playback!;
        int index = forward ? sessions.Position : sessions.Position - 1;
        if (index < 0 || index >= playback.Moves.Count)
        {
            if (forward) sessions.SetAutoplay(false);
            return;
        }

        State.TakeBackSolved(wnd.uptimeSeconds());
        var controller = sessions;
        var move = playback.Moves[index];
        bool column = move.Axis == RecordedMoveAxis.Column;
        int length = column ? MyGrid.Height : MyGrid.Width;
        int delta = move.Delta % length;
        if (!forward) delta = -delta;
        moveHighlight = forward
            ? (move.StartX, move.StartY)
            : column
                ? (move.StartX, (move.StartY + delta + length) % length)
                : ((move.StartX + delta + length) % length, move.StartY);
        animationLockHoriz = column;
        animationOrdinal = move.Index;
        animationTileOffset = 0;
        double start = -1;
        double duration = Math.Max(0.1, Math.Min(0.35, Math.Abs(delta) * 0.12));
        Animations.Enqueue(new Animation
        {
            animation = (window, animation) =>
            {
                double time = wnd.uptimeSeconds();
                if (start < 0) start = time;
                float t = Math.Clamp((float)((time - start) / duration), 0f, 1f);
                if (t >= 1f)
                {
                    if (forward) controller.Step();
                    else controller.StepBack();
                    animationLockHoriz = null;
                    animationTileOffset = 0;
                    moveHighlight = null;
                    lastPlaybackTick = time;
                    return false;
                }

                animationTileOffset = delta * (t * t * (3f - 2f * t));
                return true;
            }
        });
    }
}