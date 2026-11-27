using System.Text.Json;
using SkiaSharp;

namespace SlideScramble;

public partial class Window
{
    private readonly SKPaint lensCellFillPaint = new()
    {
        Color = new SKColor(14, 165, 233, 38),
        Style = SKPaintStyle.Fill,
        IsAntialias = true
    };

    private readonly SKPaint lensCellBorderPaint = new()
    {
        Color = new SKColor(125, 211, 252, 235),
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 3,
        IsAntialias = true
    };

    private readonly SKPaint lensSymbolPaint = new()
    {
        Color = new SKColor(253, 230, 138),
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 2.5f,
        IsAntialias = true
    };

    private readonly SKPaint lensStarPaint = new()
    {
        Color = new SKColor(253, 230, 138),
        Style = SKPaintStyle.Fill,
        IsAntialias = true
    };

    private readonly SKPaint lensGestureArrowPaint = new()
    {
        Color = new SKColor(253, 224, 71, 235),
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 4,
        StrokeCap = SKStrokeCap.Round,
        IsAntialias = true
    };

    private readonly SKPaint lensGestureArrowHeadPaint = new()
    {
        Color = new SKColor(253, 224, 71, 235),
        Style = SKPaintStyle.Fill,
        IsAntialias = true
    };
    private bool IsShuffleLogic => activeRules.Name == "ShuffleLogic";

    private void ToggleGamePreset()
    {
        if (!CanChangeBoard)
        {
            sessionMessage = "Finish the current activity before changing game mode";
            return;
        }

        if (!IsShuffleLogic && (MyGrid.Width < 3 || MyGrid.Height < 3))
        {
            sessionMessage = "ShuffleLogic requires at least a 3 × 3 board";
            return;
        }

        activeRules = IsShuffleLogic
            ? PuzzleRuleSet.CreateClassic(activeRules.Registry)
            : PuzzleRuleSet.CreateShuffleLogic(activeRules.Registry);
        MyGrid.Reset();
        State.Reset();
        sessions = new SessionRecordingController(MyGrid, activeRules);
        savedSession = null;
        goal = null;
        lensInteraction.Cancel();
        sessionMessage = IsShuffleLogic
            ? "ShuffleLogic selected. Choose Position Portable Lens to begin."
            : "Classic SlideScramble selected";
    }

    private void BeginPortableLensPlacement()
    {
        lensDoubleClick.Reset();
        if (!HasRuntimeLensOperation)
        {
            sessionMessage = "No Portable Lens operation is enabled in the current input preferences";
            return;
        }
        if (sessions.Mode == RecordingControllerMode.Playback || State.Mode == GameMode.Scrambling ||
            drag != null || Animations.Count != 0 || popupHost.IsVisible || ShowGridSetup || boardDraft != null)
        {
            sessionMessage = "Finish the current activity before positioning the lens";
            return;
        }
        if (!lensInteraction.BeginPlacement(MyGrid.Width, MyGrid.Height))
        {
            sessionMessage = "Portable Lens requires a board at least 3 × 3";
            return;
        }

        interaction.SetFocus(null);
        sessionMessage = "Click a 2 × 2 lens anchor, then press Enter; Escape cancels";
    }

    private bool CanHandleLensInput =>
        sessions.Mode != RecordingControllerMode.Playback && State.Mode != GameMode.Scrambling &&
        drag == null && Animations.Count == 0 && !popupHost.IsVisible && !ShowGridSetup && boardDraft == null;

    private bool HasRuntimeLensOperation =>
        sessions.RuntimeUIEnabled.Contains(PuzzleOperationIds.Shuffle) ||
        sessions.RuntimeUIEnabled.Contains(PuzzleOperationIds.ColumnSwap) ||
        sessions.RuntimeUIEnabled.Contains(PuzzleOperationIds.RowSwap);

    private void CommitLensGesture(LensGestureRequest request)
    {
        if (!CanHandleLensInput)
        {
            sessionMessage = "Finish the current activity before making a lens move";
            return;
        }

        IOperationIntent? intent = request.Intent;
        if (intent == null)
        {
            sessionMessage = "The lens gesture does not select a supported operation";
            return;
        }
        var rules = sessions.RuleSet;
        var evaluator = new OperationRuleEvaluator(rules.Registry);
        var evaluation = evaluator.EvaluateUiRequest(new OperationBoardSnapshot(CaptureBoardCells()), intent, rules,
            sessions.RuntimeUIEnabled);
        if (!evaluation.IsAccepted)
        {
            sessionMessage = evaluation.RejectionReason ?? $"{request.OperationId} operation was rejected";
            return;
        }

        OperationPlan plan = evaluation.Plan!;
        JsonElement recordingParameters = intent switch
        {
            ShuffleOperationIntent shuffleIntent => shuffleIntent.ToRecordingParameters(),
            PortableLensSwapIntent swapIntent => swapIntent.ToRecordingParameters(),
            _ => throw new InvalidOperationException("The lens operation has no recording parameter serializer.")
        };
        try
        {
            BeginOperationAnimation(plan, recordingParameters);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            sessionMessage = ex.Message;
        }
    }

    private int[,] CaptureBoardCells()
    {
        var cells = new int[MyGrid.Height, MyGrid.Width];
        for (int y = 0; y < MyGrid.Height; y++)
        for (int x = 0; x < MyGrid.Width; x++)
        {
            Tile tile = MyGrid.Tiles[y, x];
            cells[y, x] = tile.OriginY * MyGrid.Width + tile.OriginX;
        }

        return cells;
    }

    private void BeginShuffleChallenge(int operationCount)
    {
        if (!CanChangeBoard) return;
        if (!IsShuffleLogic)
        {
            sessionMessage = "Select ShuffleLogic before generating a Shuffle challenge";
            return;
        }

        try
        {
            var evaluator = new OperationRuleEvaluator(activeRules.Registry);
            var challenge = ShuffleChallengeFactory.Generate(MyGrid.Width, MyGrid.Height, operationCount,
                activeRules, evaluator, rand);
            var grid = new Grid(MyGrid.Width, MyGrid.Height);
            var controller = new SessionRecordingController(grid, activeRules);
            controller.LoadPlayback(challenge.Solution);
            controller.StopPlayback();

            MyGrid = grid;
            sessions = controller;
            savedSession = challenge.Solution;
            goal = challenge.Solution.GetVerifiedGoalMoveCost();
            lensInteraction.Cancel();
            State.StartScrambling();
            State.FinishScrambling(wnd.uptimeSeconds());
            UpdateTileView();
            Audio.PlayGameStartChime();
            sessionMessage = $"ShuffleLogic challenge ready · Goal: {goal ?? 0} logical moves";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            sessionMessage = ex.Message;
        }
    }

    private void DrawPortableLensOverlay(SKCanvas canvas)
    {
        if (lensInteraction.Phase == PortableLensInteractionPhase.Inactive || tileView == null) return;

        float tileW = tileView.width / MyGrid.Width;
        float tileH = tileView.height / MyGrid.Height;
        DrawLensCells(canvas, (lensInteraction.AnchorX, lensInteraction.AnchorY), tileView.xx, tileView.yy,
            MyGrid.Width, MyGrid.Height, tileW, tileH,
            lensInteraction.Phase == PortableLensInteractionPhase.Positioning);

        if (lensInteraction.GestureStartPointer is { } start && lensInteraction.GesturePointer is { } pointer)
        {
            float deltaX = pointer.X - start.X;
            float deltaY = pointer.Y - start.Y;
            float length = MathF.Sqrt(deltaX * deltaX + deltaY * deltaY);
            if (length > 2)
            {
                DrawLensGestureArrow(canvas, start, pointer);

                int pointerCellX = (int)MathF.Floor((pointer.X - tileView.xx) * MyGrid.Width / tileView.width);
                int pointerCellY = (int)MathF.Floor((pointer.Y - tileView.yy) * MyGrid.Height / tileView.height);
                if (lensInteraction.TryGetGestureTarget(pointerCellX, pointerCellY, out _,
                        out int wrapOffsetX, out int wrapOffsetY) && (wrapOffsetX != 0 || wrapOffsetY != 0))
                {
                    DrawLensGestureArrow(canvas,
                        (start.X + wrapOffsetX * tileW, start.Y + wrapOffsetY * tileH),
                        (pointer.X + wrapOffsetX * tileW, pointer.Y + wrapOffsetY * tileH));
                }
            }
        }
    }

    private void DrawLensCells(SKCanvas canvas, (int X, int Y) anchor, float originX, float originY,
        int width, int height, float tileW, float tileH, bool positioning, float opacity = 1)
    {
        if (opacity <= 0) return;

        SKColor Fade(SKColor color) => color.WithAlpha((byte)MathF.Round(color.Alpha * opacity));

        lensCellFillPaint.Color = Fade(positioning ? new SKColor(14, 165, 233, 38) : new SKColor(234, 179, 8, 38));
        lensCellBorderPaint.Color = Fade(positioning ? new SKColor(125, 211, 252, 235) : new SKColor(253, 224, 71, 240));
        lensStarPaint.Color = lensSymbolPaint.Color = Fade(new SKColor(253, 230, 138));

        foreach (PortableLensCell cell in Enum.GetValues<PortableLensCell>())
        {
            var (x, y) = LensGestureOperationMapper.BoardPosition(cell, anchor, width, height);
            float left = originX + x * tileW;
            float top = originY + y * tileH;
            var rect = new SKRect(left + 3, top + 3, left + tileW - 3, top + tileH - 3);
            using var rounded = new SKRoundRect(rect, 9, 9);
            canvas.DrawRoundRect(rounded, lensCellFillPaint);
            canvas.DrawRoundRect(rounded, lensCellBorderPaint);

            float centerX = rect.MidX;
            float centerY = rect.MidY;
            float symbolSize = Math.Min(tileW, tileH) * 0.18f;
            switch (cell)
            {
                case PortableLensCell.Circle:
                    canvas.DrawCircle(centerX, centerY, symbolSize, lensSymbolPaint);
                    break;
                case PortableLensCell.Star:
                    DrawStar(canvas, centerX, centerY, symbolSize, symbolSize * 0.45f, lensStarPaint);
                    break;
                case PortableLensCell.Triangle:
                    using (var path = new SKPath())
                    {
                        path.MoveTo(centerX, centerY - symbolSize);
                        path.LineTo(centerX + symbolSize, centerY + symbolSize * 0.8f);
                        path.LineTo(centerX - symbolSize, centerY + symbolSize * 0.8f);
                        path.Close();
                        canvas.DrawPath(path, lensSymbolPaint);
                    }
                    break;
                case PortableLensCell.Square:
                    canvas.DrawRect(new SKRect(centerX - symbolSize * 0.75f, centerY - symbolSize * 0.75f,
                        centerX + symbolSize * 0.75f, centerY + symbolSize * 0.75f), lensSymbolPaint);
                    break;
            }
        }
    }

    private void DrawLensGestureArrow(SKCanvas canvas, (float X, float Y) start, (float X, float Y) pointer)
    {
        float deltaX = pointer.X - start.X;
        float deltaY = pointer.Y - start.Y;
        float length = MathF.Sqrt(deltaX * deltaX + deltaY * deltaY);
        if (length <= 2 || tileView == null) return;

        float unitX = deltaX / length;
        float unitY = deltaY / length;
        const float headLength = 14;
        const float headHalfWidth = 6;
        float baseX = pointer.X - unitX * headLength;
        float baseY = pointer.Y - unitY * headLength;
        using var arrowHead = new SKPath();
        arrowHead.MoveTo(pointer.X, pointer.Y);
        arrowHead.LineTo(baseX - unitY * headHalfWidth, baseY + unitX * headHalfWidth);
        arrowHead.LineTo(baseX + unitY * headHalfWidth, baseY - unitX * headHalfWidth);
        arrowHead.Close();
        canvas.Save();
        canvas.ClipRect(new SKRect(tileView.xx, tileView.yy,
            tileView.xx + tileView.width, tileView.yy + tileView.height));
        canvas.DrawLine(start.X, start.Y, pointer.X - unitX * headLength * 0.6f,
            pointer.Y - unitY * headLength * 0.6f, lensGestureArrowPaint);
        canvas.DrawPath(arrowHead, lensGestureArrowHeadPaint);
        canvas.Restore();
    }
}