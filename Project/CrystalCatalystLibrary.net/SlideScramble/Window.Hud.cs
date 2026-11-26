using CrystalCatalystLibrary.net;
using CrystalOpenAL;
using CrystalOpenGL;
using CrystalSkia.net;
using Silk.NET.OpenGL;
using SkiaSharp;

namespace SlideScramble;

public partial class Window
{
    private const float FpsHudX = 132;
    private const float FpsHudGap = 12;

    internal static float CalculateCenterHudScale(float width, float centerTotalWidth) =>
        Math.Min(1, Math.Max(1, width - 260f) / centerTotalWidth);

    internal static bool ShouldDrawFps(float width, float fpsTextWidth, float centerTotalWidth)
    {
        if (width < 650 || centerTotalWidth <= 0) return false;

        float centerAreaStart = width / 2f - centerTotalWidth / 2f;
        float hudScale = CalculateCenterHudScale(width, centerTotalWidth);
        float renderedCenterStart = width / 2f + (centerAreaStart - width / 2f) * hudScale;
        return FpsHudX + fpsTextWidth + FpsHudGap <= renderedCenterStart;
    }

    private static void DrawDropdownArrow(SKCanvas canvas, float x, float y, float size, SKPaint paint)
    {
        using var path = new SKPath();
        path.MoveTo(x - size, y - size * 0.5f);
        path.LineTo(x + size, y - size * 0.5f);
        path.LineTo(x, y + size * 0.7f);
        path.Close();
        canvas.DrawPath(path, paint);
    }

    private static void DrawStar(SKCanvas canvas, float cx, float cy, float outerRadius, float innerRadius,
        SKPaint fillPaint)
    {
        using var path = new SKPath();
        double angleStep = Math.PI / 5.0;
        double startAngle = -Math.PI / 2.0;

        for (int i = 0; i < 10; i++)
        {
            double angle = startAngle + i * angleStep;
            float r = (i % 2 == 0) ? outerRadius : innerRadius;
            float x = cx + (float)(r * Math.Cos(angle));
            float y = cy + (float)(r * Math.Sin(angle));
            if (i == 0)
                path.MoveTo(x, y);
            else
                path.LineTo(x, y);
        }

        path.Close();
        canvas.DrawPath(path, fillPaint);
    }

    private static void DrawCloseCross(SKCanvas canvas, float cx, float cy, float size, SKPaint strokePaint)
    {
        canvas.DrawLine(cx - size, cy - size, cx + size, cy + size, strokePaint);
        canvas.DrawLine(cx - size, cy + size, cx + size, cy - size, strokePaint);
    }

    private void OnResize(CrystalWindow windowHandle, int width, int height)
    {
        if (_glAvailable && _glPresenter != null)
        {
            _glPresenter.SetViewport(width, height);
        }
    }

    private void OnDraw(CrystalWindow windowHandle)
    {
        windowHandle.GetSize(out int width, out int height);

        string fpsText;

        double time = wnd.uptimeSeconds();
        double time_delta = time - last_time;
        last_time = time;

        sample_deltas[sample_index] = time_delta;
        sample_index = (sample_index + 1) % sample_deltas.Length;
        if (sample_count < sample_deltas.Length) sample_count++;

        double total_delta = 0;
        for (int i = 0; i < sample_count; i++)
        {
            total_delta += sample_deltas[i];
        }

        fpsText = $"{sample_count / total_delta:0.0} FPS";

        // Initialize GL presenter on first draw
        if (_glAvailable && _glPresenter == null && _gl != null)
        {
            try
            {
                wnd.GLMakeCurrent();
                _glPresenter = new GLPixDataPresenter(_gl);
                _glPresenter.SetViewport(width, height);
                _glPresenter.CollectDiagnostics = true;
                Console.WriteLine("[GL] Presenter initialized successfully.");
            }
            catch (Exception ex)
            {
                _glError = ex.Message;
                _glAvailable = false;
                Console.WriteLine($"[GL] Warning: Presenter init failed ({_glError}) — falling back to legacy.");
            }
        }

        if (!pixelBacking || pixelBacking.width != width || pixelBacking.height != height)
        {
            if (pixelBacking)
            {
                pixelBacking.Dispose();
            }

            pixelBacking = CrystalSkia.net.FixedPixDataRenderer.CreateFixed(width, height, (canvas, info) => { });
        }

        UpdateTileView(width, height);
        var media = GetMediaLayout(width, height);
        UpdateMediaEditorBounds(media);

        if (tileView == null) return;

        CrystalSkia.net.PixDataSkia.WithCanvasView(pixelBacking, (bitmap, canvas) =>
        {
            // Clear background
            canvas.DrawRect(new SKRect(0, 0, width, height), bgPaint);

            // Draw Board Container Background
            var boardRect = new SKRect(tileView.xx - 8, tileView.yy - 8, tileView.xx + tileView.width + 8,
                tileView.yy + tileView.height + 8);
            var boardRRect = new SKRoundRect(boardRect, 12, 12);
            canvas.DrawRoundRect(boardRRect, boardBgPaint);
            canvas.DrawRoundRect(boardRRect, boardBorderPaint);

            float tileW = tileView.width / MyGrid.Width;
            float tileH = tileView.height / MyGrid.Height;

            if (activeOperationAnimation is { } operationAnimation)
            {
                DrawOperationAnimation(canvas, operationAnimation, tileW, tileH);
            }
            else
            {
                // Draw Static Grid Tiles
                for (int y = 0; y < MyGrid.Height; y++)
                {
                    if (animationLockHoriz != null && animationLockHoriz.Value == false &&
                        animationOrdinal == y) continue;

                    for (int x = 0; x < MyGrid.Width; x++)
                    {
                        if (animationLockHoriz != null && animationLockHoriz.Value == true && animationOrdinal == x)
                            continue;

                        SKImage image = (boardDraft ?? MyGrid.Tiles)[y, x].Image;

                        float _x = tileView.xx + tileW * x;
                        float _y = tileView.yy + tileH * y;

                        var dest = new SKRect(_x, _y, _x + tileW, _y + tileH);
                        var src = new SKRect(0, 0, image.Width, image.Height);

                        canvas.DrawImage(image, src, dest, paint: tilePaint);
                    }
                }
            }

            if (boardDraft != null)
            {
                DrawBoardEditor(canvas, width);
                return;
            }

            // Draw Active Animated Row / Column
            if (activeOperationAnimation == null && animationLockHoriz != null)
            {
                bool horiz = animationLockHoriz.Value;
                if (!horiz)
                {
                    // Active row moving horizontally
                    int y = animationOrdinal;
                    float rowTop = tileView.yy + tileH * y;
                    float rowHeight = tileH;
                    var clipRect = new SKRect(tileView.xx, rowTop, tileView.xx + tileView.width, rowTop + rowHeight);

                    canvas.Save();
                    canvas.ClipRect(clipRect);

                    float offset = (float)(animationTileOffset % MyGrid.Width);
                    if (offset < 0) offset += MyGrid.Width;

                    for (int x = 0; x < MyGrid.Width; x++)
                    {
                        SKImage image = MyGrid.Tiles[y, x].Image;

                        float coord = x - offset;
                        float _x = tileView.xx + tileW * coord;
                        float _y = rowTop;

                        var src = new SKRect(0, 0, image.Width, image.Height);

                        var dest = new SKRect(_x, _y, _x + tileW, _y + rowHeight);
                        canvas.DrawImage(image, src, dest, paint: tilePaint);

                        float _xWrapped = _x + tileView.width;
                        var destWrapped = new SKRect(_xWrapped, _y, _xWrapped + tileW, _y + rowHeight);
                        canvas.DrawImage(image, src, destWrapped, paint: tilePaint);
                    }

                    canvas.DrawRoundRect(
                        new SKRoundRect(
                            new SKRect(tileView.xx, rowTop, tileView.xx + tileView.width, rowTop + rowHeight), 6, 6),
                        activeLinePaint);

                    canvas.Restore();
                }
                else
                {
                    // Active column moving vertically
                    int x = animationOrdinal;
                    float colLeft = tileView.xx + tileW * x;
                    float colWidth = tileW;
                    var clipRect = new SKRect(colLeft, tileView.yy, colLeft + colWidth, tileView.yy + tileView.height);

                    canvas.Save();
                    canvas.ClipRect(clipRect);

                    float offset = (float)(animationTileOffset % MyGrid.Height);
                    if (offset < 0) offset += MyGrid.Height;

                    for (int y = 0; y < MyGrid.Height; y++)
                    {
                        SKImage image = MyGrid.Tiles[y, x].Image;

                        float coord = y - offset;
                        float _x = colLeft;
                        float _y = tileView.yy + tileH * coord;

                        var src = new SKRect(0, 0, image.Width, image.Height);

                        var dest = new SKRect(_x, _y, _x + colWidth, _y + tileH);
                        canvas.DrawImage(image, src, dest, paint: tilePaint);

                        float _yWrapped = _y + tileView.height;
                        var destWrapped = new SKRect(_x, _yWrapped, _x + colWidth, _yWrapped + tileH);
                        canvas.DrawImage(image, src, destWrapped, paint: tilePaint);
                    }

                    canvas.DrawRoundRect(
                        new SKRoundRect(
                            new SKRect(colLeft, tileView.yy, colLeft + colWidth, tileView.yy + tileView.height), 6, 6),
                        activeLinePaint);

                    canvas.Restore();
                }
            }

            DrawPortableLensOverlay(canvas);

            if (moveHighlight is { } highlighted)
            {
                float x = tileView.xx + tileW * highlighted.X;
                float y = tileView.yy + tileH * highlighted.Y;
                float wrapX = 0, wrapY = 0;
                if (animationLockHoriz is { } column)
                {
                    int length = column ? MyGrid.Height : MyGrid.Width;
                    float offset = animationTileOffset % length;
                    if (offset < 0) offset += length;
                    if (column)
                    {
                        y -= tileH * offset;
                        wrapY = tileView.height;
                    }
                    else
                    {
                        x -= tileW * offset;
                        wrapX = tileView.width;
                    }
                }

                canvas.Save();
                canvas.ClipRect(new SKRect(tileView.xx, tileView.yy, tileView.xx + tileView.width,
                    tileView.yy + tileView.height));
                canvas.DrawRoundRect(new SKRoundRect(new SKRect(x + 3, y + 3, x + tileW - 3, y + tileH - 3), 6, 6),
                    activeLinePaint);
                if (wrapX != 0 || wrapY != 0)
                    canvas.DrawRoundRect(
                        new SKRoundRect(
                            new SKRect(x + wrapX + 3, y + wrapY + 3, x + wrapX + tileW - 3, y + wrapY + tileH - 3), 6,
                            6), activeLinePaint);
                canvas.Restore();
            }

            // Draw Top HUD Bar
            var hudRect = new SKRect(16, 10, width - 16, 50);
            var hudRRect = new SKRoundRect(hudRect, 8, 8);
            canvas.DrawRoundRect(hudRRect, hudBarPaint);

            // Left HUD: Clickable Grid Button & FPS
            var gridBtnRect = new SKRect(22, 14, 120, 46);
            var gridBtnRRect = new SKRoundRect(gridBtnRect, 6, 6);
            canvas.DrawRoundRect(gridBtnRRect, hudChipPaint);
            canvas.DrawRoundRect(gridBtnRRect, hudChipBorderPaint);

            string gridInfo = $"Grid: {MyGrid.Width}x{MyGrid.Height}";
            hudBoldFont.MeasureText(gridInfo, out SKRect gridBounds, hudAccentPaint);
            hudFont.MeasureText(fpsText, out SKRect fpsBounds, hudMutedPaint);
            canvas.DrawText(gridInfo, 28, 35, hudBoldFont, hudAccentPaint);
            DrawDropdownArrow(canvas, 28 + gridBounds.Width + 8, 30, 4, hudAccentPaint);

            // Diagnostics overlay
            if (ShowDiagnostics)
            {
                if (_glAvailable && _glPresenter != null)
                {
                    var diag = _glPresenter.LastFrameDiagnostics;
                    string diagText =
                        $"GL: {diag.TextureUploadMs:F1}ms upload + {diag.DrawSubmissionMs:F1}ms draw | {diag.TextureWidth}x{diag.TextureHeight} -> {diag.ViewportWidth}x{diag.ViewportHeight}";
                    canvas.DrawText(diagText, 16, height - 30, hudFont, hudMutedPaint);
                }
            }

            // Center HUD: Mode, Moves & Time
            string ruleSetText = $"Mode: {activeRules.Name}";
            string movesText = FormatGameScoreText(State.MoveCount, goal);
            string timeText = $"Time: {State.FormatTime(time)}";
            string? recordingText = null;
            if (sessions.Mode == RecordingControllerMode.Playback)
                recordingText = FormatRecordingStatus(sessions.Mode, sessions.Position, sessions.MoveCount);

            if (sessions.Mode == RecordingControllerMode.Recording)
                recordingText = FormatRecordingStatus(sessions.Mode, sessions.Position, sessions.MoveCount);

            hudBoldFont.MeasureText(ruleSetText, out SKRect ruleSetBounds, hudMutedPaint);
            hudBoldFont.MeasureText(movesText, out SKRect movesBounds, hudTextPaint);
            hudBoldFont.MeasureText(timeText, out SKRect timeBounds, hudAccentPaint);
            SKRect? recordingBounds = null;
            if (recordingText != null)
            {
                hudBoldFont.MeasureText(recordingText, out SKRect rb, hudMutedPaint);
                recordingBounds = rb;
            }

            float centerTotalWidth = ruleSetBounds.Width + movesBounds.Width + timeBounds.Width + 48;
            if (recordingBounds.HasValue) centerTotalWidth += recordingBounds.Value.Width + 16;
            float centerAreaStart = width / 2f - centerTotalWidth / 2f;
            float hudScale = CalculateCenterHudScale(width, centerTotalWidth);
            if (width >= 500)
            {
                canvas.Save();
                canvas.Translate(width / 2f, 35);
                canvas.Scale(hudScale);
                canvas.Translate(-width / 2f, -35);
                canvas.DrawText(ruleSetText, centerAreaStart, 35, hudBoldFont, hudMutedPaint);
                float movesX = centerAreaStart + ruleSetBounds.Width + 16;
                canvas.DrawText(movesText, movesX, 35, hudBoldFont, hudTextPaint);
                float timeX = movesX + movesBounds.Width + 16;
                canvas.DrawText(timeText, timeX, 35, hudBoldFont, hudAccentPaint);
                if (recordingText != null && recordingBounds.HasValue)
                {
                    float recX = timeX + timeBounds.Width + 16;
                    canvas.DrawText(recordingText, recX, 35, hudBoldFont, hudMutedPaint);
                }

                canvas.Restore();
            }
            else
            {
                string compactStats = recordingText == null
                    ? $"{ruleSetText}  {movesText}"
                    : $"{ruleSetText}  {movesText}  {recordingText}";
                float compactWidth = hudFont.MeasureText(compactStats);
                canvas.Save();
                canvas.Translate(width / 2f, 56);
                canvas.Scale(Math.Min(1, Math.Max(1, width - 16f) / Math.Max(1, compactWidth)));
                canvas.DrawText(compactStats, -compactWidth / 2, 0, hudFont, hudTextPaint);
                canvas.Restore();
            }

            if (ShouldDrawFps(width, fpsBounds.Width, centerTotalWidth))
                canvas.DrawText(fpsText, FpsHudX, 35, hudFont, hudMutedPaint);

            // Right HUD: Game Mode
            string modeText = State.Mode switch
            {
                GameMode.Ready => "READY",
                GameMode.Scrambling => "SCRAMBLING...",
                GameMode.InGame => "IN PLAY",
                GameMode.Solved => "SOLVED!",
                _ => ""
            };
            SKPaint statePaint = State.Mode switch
            {
                GameMode.Solved => bannerBorderPaint,
                GameMode.Scrambling => activeLinePaint,
                GameMode.InGame => hudAccentPaint,
                _ => hudMutedPaint
            };

            hudBoldFont.MeasureText(modeText, out SKRect modeBounds, statePaint);
            float modeTextX = width - 36 - modeBounds.Width;
            if (width >= 650) canvas.DrawText(modeText, modeTextX, 35, hudBoldFont, statePaint);
            if (width >= 650 && State.Mode == GameMode.Solved)
            {
                DrawStar(canvas, modeTextX - 12, 30, 5.5f, 2.5f, bannerBorderPaint);
            }

            // Victory Banner Overlay
            if (State.Mode == GameMode.Solved)
            {
                float bannerW = Math.Min(500f, width - 60f);
                float bannerH = 120f;
                float bx = (width - bannerW) / 2f;
                float by = tileView.yy + (tileView.height - bannerH) / 2f;

                var vRect = new SKRect(bx, by, bx + bannerW, by + bannerH);
                var vRRect = new SKRoundRect(vRect, 16, 16);
                canvas.DrawRoundRect(vRRect, bannerBgPaint);
                canvas.DrawRoundRect(vRRect, bannerBorderPaint);

                string winTitle = "PUZZLE SOLVED!";
                bannerTitleFont.MeasureText(winTitle, out SKRect wtBounds, bannerTitlePaint);
                float titleX = bx + (bannerW - wtBounds.Width) / 2f;
                canvas.DrawText(winTitle, titleX, by + 42, bannerTitleFont, bannerTitlePaint);
                DrawStar(canvas, titleX - 22, by + 34, 9, 4f, bannerTitlePaint);
                DrawStar(canvas, titleX + wtBounds.Width + 22, by + 34, 9, 4f, bannerTitlePaint);

                string stats = $"Time: {State.FormatTime(time)}  •  {FormatGameScoreText(State.MoveCount, goal)}";
                bannerTextFont.MeasureText(stats, out SKRect sBounds, bannerTextPaint);
                canvas.DrawText(stats, bx + (bannerW - sBounds.Width) / 2f, by + 74, bannerTextFont, bannerTextPaint);

                string prompt = "Press [S] to scramble or [G] to change grid size";
                footerFont.MeasureText(prompt, out SKRect pBounds, hudAccentPaint);
                canvas.DrawText(prompt, bx + (bannerW - pBounds.Width) / 2f, by + 100, footerFont, hudAccentPaint);
            }

            if (showMedia) DrawMedia(canvas, media);

            // Session Selection Popup
            if (popupHost.IsVisible && commandMenu == null && challengeGenerator == null &&
                continueRecordingDialog == null)
            {
                LayoutSelectionPopup(width, height);
                ((ICrystalSkia)popupHost).OnDraw(canvas);
                DrawPreview(canvas);
            }

            // Grid Setup Overlay Panel
            if (ShowGridSetup)
            {
                // Semi-transparent backdrop scrim
                canvas.DrawRect(new SKRect(0, 0, width, height), modalBackdropPaint);

                float panelWidth = 360f;
                float panelHeight = 220f;
                float panelX = (width - panelWidth) / 2f;
                float panelY = (height - panelHeight) / 2f;

                var panelRect = new SKRect(panelX, panelY, panelX + panelWidth, panelY + panelHeight);
                var panelRRect = new SKRoundRect(panelRect, 14, 14);
                canvas.DrawRoundRect(panelRRect, panelBgPaint);
                canvas.DrawRoundRect(panelRRect, panelBorderPaint);

                // Header Title
                string title = "Grid Setup";
                canvas.DrawText(title, panelX + 24, panelY + 36, panelTitleFont, hudTextPaint);

                // Close button icon
                using var closeIconPaint = new SKPaint
                {
                    Color = new SKColor(148, 163, 184),
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 2f,
                    StrokeCap = SKStrokeCap.Round,
                    IsAntialias = true
                };
                DrawCloseCross(canvas, panelX + panelWidth - 22, panelY + 24, 5.5f, closeIconPaint);

                float trackStartX = panelX + 130;
                float trackEndX = panelX + 270;

                // Row 1: Columns slider
                float row1Y = panelY + 80;
                canvas.DrawText("Columns", panelX + 24, row1Y + 5, hudBoldFont, hudTextPaint);
                canvas.DrawText("2", panelX + 112, row1Y + 5, hudFont, hudMutedPaint);
                canvas.DrawLine(trackStartX, row1Y, trackEndX, row1Y, sliderTrackPaint);

                float colT = (MyGrid.Width - 2) / 7f;
                float colThumbX = trackStartX + colT * (trackEndX - trackStartX);
                if (colT > 0.001f)
                {
                    canvas.DrawLine(trackStartX, row1Y, colThumbX, row1Y, sliderFillPaint);
                }

                canvas.DrawCircle(colThumbX, row1Y, 8, sliderThumbPaint);
                canvas.DrawCircle(colThumbX, row1Y, 8, sliderThumbBorderPaint);

                canvas.DrawText("9", panelX + 280, row1Y + 5, hudFont, hudMutedPaint);
                string colValText = $"{MyGrid.Width}";
                canvas.DrawText(colValText, panelX + 310, row1Y + 5, hudBoldFont, hudAccentPaint);

                // Row 2: Rows slider
                float row2Y = panelY + 130;
                canvas.DrawText("Rows", panelX + 24, row2Y + 5, hudBoldFont, hudTextPaint);
                canvas.DrawText("2", panelX + 112, row2Y + 5, hudFont, hudMutedPaint);
                canvas.DrawLine(trackStartX, row2Y, trackEndX, row2Y, sliderTrackPaint);

                float rowT = (MyGrid.Height - 2) / 7f;
                float rowThumbX = trackStartX + rowT * (trackEndX - trackStartX);
                if (rowT > 0.001f)
                {
                    canvas.DrawLine(trackStartX, row2Y, rowThumbX, row2Y, sliderFillPaint);
                }

                canvas.DrawCircle(rowThumbX, row2Y, 8, sliderThumbPaint);
                canvas.DrawCircle(rowThumbX, row2Y, 8, sliderThumbBorderPaint);

                canvas.DrawText("9", panelX + 280, row2Y + 5, hudFont, hudMutedPaint);
                string rowValText = $"{MyGrid.Height}";
                canvas.DrawText(rowValText, panelX + 310, row2Y + 5, hudBoldFont, hudAccentPaint);

                // Dimension Summary Pill
                string dimSummary = $"{MyGrid.Width} x {MyGrid.Height}";
                hudBoldFont.MeasureText(dimSummary, out SKRect dimBounds, hudAccentPaint);
                var badgeRect = new SKRect(panelX + (panelWidth - dimBounds.Width) / 2f - 16, panelY + 158,
                    panelX + (panelWidth + dimBounds.Width) / 2f + 16, panelY + 184);
                canvas.DrawRoundRect(new SKRoundRect(badgeRect, 6, 6), hudChipPaint);
                canvas.DrawRoundRect(new SKRoundRect(badgeRect, 6, 6), hudChipBorderPaint);
                canvas.DrawText(dimSummary, panelX + (panelWidth - dimBounds.Width) / 2f, panelY + 177, hudBoldFont,
                    hudAccentPaint);

                string hint = "[G] or [Esc] to Close";
                footerFont.MeasureText(hint, out SKRect hintBounds, hudMutedPaint);
                canvas.DrawText(hint, panelX + (panelWidth - hintBounds.Width) / 2f, panelY + 204, footerFont,
                    hudMutedPaint);
            }

            // Bottom Footer Shortcuts
            string footerText = "[Space] Commands";
            footerFont.MeasureText(footerText, out SKRect footerBounds, hudMutedPaint);
            canvas.Save();
            float footerScale = Math.Min(1, Math.Max(1, width - 16) / Math.Max(1, footerBounds.Width));
            canvas.Translate(width / 2f, height - 12);
            canvas.Scale(footerScale);
            canvas.DrawText(footerText, -footerBounds.Width / 2f, 0, footerFont, hudMutedPaint);
            canvas.Restore();

            if (challengeGenerator != null) DrawChallengeGenerator(canvas, width, height);
            if (continueRecordingDialog != null) DrawContinueRecordingDialog(canvas, width, height);

            if (commandMenu != null)
            {
                var area = new System.Drawing.RectangleF(0, 0, width, height);
                if (inputHelpPreview != null)
                {
                    popupHost.SetLayout(area, inputHelpPreview.Layout(area, commandMenu));
                    popupHost.OnDraw(canvas);
                    inputHelpPreview.Current?.OnDraw(canvas);
                }
                else if (presetMenuVisible)
                {
                    var layout = PresetMenuPreviewLayout.Calculate(new SKRect(0, 0, width, height),
                        commandMenu.PreferredWidth, commandMenu.PreferredHeight,
                        SelectionPreviewRules.PresetPreviewLineCount);
                    popupHost.SetLayout(area, layout.MenuBounds);
                    popupHost.OnDraw(canvas);
                    if (presetPreviewRules != null)
                        DrawPreviewRules(canvas,
                            SelectionPreviewRules.Format(presetPreviewRules, MyGrid.Width, MyGrid.Height),
                            layout.PreviewBounds);
                }
                else
                {
                    popupHost.SetLayout(area, commandMenu.GetCenteredBounds(area));
                    popupHost.OnDraw(canvas);
                }
            }
        });

        // Present via OpenGL or legacy path
        if (_glAvailable && _glPresenter != null)
        {
            wnd.GLMakeCurrent();
            bool presented = _glPresenter.Present(ref pixelBacking);
            if (presented)
            {
                wnd.GLPresent();
            }
            else
            {
                // Upload or draw failed — fall back to legacy presentation this frame
                Console.WriteLine($"[GL] Present failed: {_glPresenter.LastError} — using legacy fallback.");
                windowHandle.PresentPix(ref pixelBacking);
            }
        }
        else
        {
            windowHandle.PresentPix(ref pixelBacking);
        }
    }
}