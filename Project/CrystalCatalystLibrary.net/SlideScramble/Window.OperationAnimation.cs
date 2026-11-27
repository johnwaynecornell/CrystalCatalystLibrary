using System.Text.Json;
using SkiaSharp;

namespace SlideScramble;

public partial class Window
{
    private sealed class ActiveOperationAnimation(
        OperationAnimationTimeline timeline,
        OperationPlan? operationPlan,
        JsonElement? recordingParameters,
        bool isScramble,
        RecordedOperationPlaybackStep? playbackStep,
        SKImage[] tileImages,
        Grid grid,
        SessionRecordingController controller)
    {
        public OperationAnimationTimeline Timeline { get; } = timeline;
        public OperationPlan? OperationPlan { get; } = operationPlan;
        public JsonElement? RecordingParameters { get; } = recordingParameters;
        public bool IsScramble { get; } = isScramble;
        public RecordedOperationPlaybackStep? PlaybackStep { get; } = playbackStep;
        public SKImage[] TileImages { get; } = tileImages;
        public Grid Grid { get; } = grid;
        public SessionRecordingController Controller { get; } = controller;
        public RecordingControllerMode Mode { get; } = controller.Mode;
        public int Position { get; } = controller.Position;
        public OperationAnimationFrame Frame { get; set; }
        public double StartedAt { get; set; } = -1;
        public bool[] MovingTiles { get; } = new bool[timeline.Plan.Width * timeline.Plan.Height];
    }

    private readonly SKPaint operationTileBorderPaint = new()
    {
        Color = new SKColor(226, 232, 240, 230),
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1.5f,
        IsAntialias = true
    };

    private ActiveOperationAnimation? activeOperationAnimation;
    private OperationAnimationStyle? operationAnimationPreference;
    private bool showAnimatedLens = true;

    private void ShowOperationAnimationMenu()
    {
        ShowRulesList("Operation Animation", new[]
        {
            ("Instant", (Action)(() => SetOperationAnimationPreference(OperationAnimationStyle.Instant))),
            ("Primitive", (Action)(() => SetOperationAnimationPreference(OperationAnimationStyle.Primitive))),
            ("Logical", (Action)(() => SetOperationAnimationPreference(OperationAnimationStyle.Logical))),
            ($"Automatic (current: {operationAnimationPreference?.ToString() ?? "per operation"})", (Action)(() =>
                SetOperationAnimationPreference(null))),
            ($"Lens in scramble and playback: {(showAnimatedLens ? "On" : "Off")}", (Action)ToggleAnimatedLens),
            ("Back to commands", (Action)ShowCommandMenu)
        });
    }

    private void ToggleAnimatedLens()
    {
        showAnimatedLens = !showAnimatedLens;
        ShowOperationAnimationMenu();
    }

    private void SetOperationAnimationPreference(OperationAnimationStyle? preference)
    {
        operationAnimationPreference = preference;
        sessionMessage = preference.HasValue
            ? $"Portable Lens animation set to {preference.Value}"
            : "Portable Lens animation set to automatic per operation";
        ShowCommandMenu();
    }

    private void BeginOperationAnimation(OperationPlan plan, JsonElement recordingParameters)
    {
        BeginOperationAnimation(plan, recordingParameters, false);
    }

    private void BeginScrambleOperationAnimation(OperationPlan plan)
    {
        if (plan.Purpose != OperationPlanPurpose.Scramble)
            throw new ArgumentException("Only scramble operations can use generated-challenge animation.", nameof(plan));
        BeginOperationAnimation(plan, null, true);
    }

    private void BeginOperationAnimation(OperationPlan plan, JsonElement? recordingParameters, bool isScramble)
    {
        if (isScramble != (plan.Purpose == OperationPlanPurpose.Scramble))
            throw new ArgumentException("The operation purpose does not match its animation context.", nameof(plan));

        var board = new OperationBoardSnapshot(CaptureBoardCells());
        var visualPlan = new OperationAnimationPlan(board, plan);
        OperationAnimationStyle style = OperationAnimationPolicy.Select(plan,
            visualPlan.SupportsLogicalAnimation, operationAnimationPreference, isScramble);
        if (style == OperationAnimationStyle.Instant)
        {
            if (isScramble) CommitScrambleOperation(plan);
            else CommitLensOperation(plan, recordingParameters!.Value);
            return;
        }

        QueueOperationAnimation(visualPlan, style, plan, recordingParameters, isScramble, null);
    }

    private void BeginPlaybackOperationAnimation(RecordedOperationPlaybackStep step)
    {
        var visualPlan = step.CreateAnimationPlan(new OperationBoardSnapshot(CaptureBoardCells()));
        var style = OperationAnimationPolicy.Select(step.Operation.Accounting, step.Operation.Purpose,
            visualPlan.SupportsLogicalAnimation, operationAnimationPreference, true);
        if (style == OperationAnimationStyle.Instant)
        {
            sessions.CommitPlaybackOperationStep(step);
            lastPlaybackTick = wnd.uptimeSeconds();
            State.TakeBackSolved(lastPlaybackTick);
            return;
        }

        QueueOperationAnimation(visualPlan, style, null, null, false, step);
    }

    private void QueueOperationAnimation(OperationAnimationPlan visualPlan, OperationAnimationStyle style,
        OperationPlan? plan, JsonElement? recordingParameters, bool isScramble,
        RecordedOperationPlaybackStep? playbackStep)
    {
        var tileImages = new SKImage[MyGrid.Width * MyGrid.Height];
        foreach (Tile tile in MyGrid.Tiles)
            tileImages[tile.OriginY * MyGrid.Width + tile.OriginX] = tile.Image;

        var active = new ActiveOperationAnimation(
            new OperationAnimationTimeline(visualPlan, style), plan, recordingParameters?.Clone(), isScramble, playbackStep,
            tileImages, MyGrid, sessions);
        activeOperationAnimation = active;
        animationLockHoriz = null;
        animationTileOffset = 0;
        moveHighlight = null;
        lensInteraction.CancelGesture();
        sessionMessage = $"{visualPlan.OperationId} · {style} animation · Esc cancels";

        var animation = new Animation();
        animation.animation = (_, _) => AdvanceOperationAnimation(active);
        Animations.Enqueue(animation);
    }

    private bool AdvanceOperationAnimation(ActiveOperationAnimation active)
    {
        if (!ReferenceEquals(activeOperationAnimation, active)) return false;
        double now = wnd.uptimeSeconds();
        if (active.StartedAt < 0) active.StartedAt = now;
        active.Frame = active.Timeline.GetFrame(now - active.StartedAt);
        if (!active.Frame.IsComplete) return true;

        try
        {
            if (!CanCommitOperationAnimation(active))
            {
                active.Timeline.Cancel();
                sessionMessage = "The board or session changed; operation animation was cancelled";
            }
            else if (active.Timeline.TryCommit(() =>
                     {
                         if (active.PlaybackStep != null)
                         {
                             sessions.CommitPlaybackOperationStep(active.PlaybackStep);
                             lastPlaybackTick = wnd.uptimeSeconds();
                             State.TakeBackSolved(lastPlaybackTick);
                             sessionMessage = $"Playback · {active.PlaybackStep.Operation.OperationId}";
                         }
                         else if (active.IsScramble) CommitScrambleOperation(active.OperationPlan!);
                         else CommitLensOperation(active.OperationPlan!, active.RecordingParameters!.Value);
                     }))
            {
                return FinishOperationAnimation(active);
            }
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            sessionMessage = ex.Message;
        }

        if (active.PlaybackStep != null && ReferenceEquals(sessions, active.Controller) &&
            sessions.Mode == RecordingControllerMode.Playback)
            sessions.SetAutoplay(false);
        return FinishOperationAnimation(active);
    }

    private bool CanCommitOperationAnimation(ActiveOperationAnimation active)
    {
        if (!ReferenceEquals(MyGrid, active.Grid) || !ReferenceEquals(sessions, active.Controller) ||
            sessions.Mode != active.Mode || sessions.Position != active.Position ||
            (State.Mode == GameMode.Scrambling) != active.IsScramble ||
            MyGrid.Width != active.Timeline.Plan.Width || MyGrid.Height != active.Timeline.Plan.Height)
            return false;

        int[,] current = CaptureBoardCells();
        int[,] expected = active.Timeline.Plan.StartingCells;
        for (int y = 0; y < MyGrid.Height; y++)
        for (int x = 0; x < MyGrid.Width; x++)
            if (current[y, x] != expected[y, x])
                return false;
        return true;
    }

    private bool FinishOperationAnimation(ActiveOperationAnimation active)
    {
        if (ReferenceEquals(activeOperationAnimation, active)) activeOperationAnimation = null;
        return false;
    }

    private void CancelOperationAnimation()
    {
        if (activeOperationAnimation is not { } active) return;
        active.Timeline.Cancel();
        activeOperationAnimation = null;
        if (active.PlaybackStep != null && ReferenceEquals(sessions, active.Controller) &&
            sessions.Mode == RecordingControllerMode.Playback)
            sessions.SetAutoplay(false);
        sessionMessage = "Operation animation cancelled; board and recording unchanged";
        wnd.QueueRedraw();
    }

    private void CommitLensOperation(OperationPlan plan, JsonElement recordingParameters)
    {
        sessions.ApplyOperation(plan, recordingParameters);
        State.TakeBackSolved(wnd.uptimeSeconds());
        for (int i = 0; i < (plan.MoveCost ?? 0); i++) State.RecordMove(wnd.uptimeSeconds());
        if (State.CheckSolved(MyGrid, wnd.uptimeSeconds())) Audio.PlayVictoryChime();
        int primitiveCount = plan.Expansion.Count;
        sessionMessage = plan.Accounting == OperationMoveAccounting.LogicalOperation
            ? $"{plan.OperationId} committed · 1 logical move · {primitiveCount} primitive rotations"
            : $"{plan.OperationId} committed · {plan.MoveCost} primitive moves";
    }

    private void CommitScrambleOperation(OperationPlan plan)
    {
        foreach (RecordedMove move in plan.Expansion)
        {
            if (move.Axis == RecordedMoveAxis.Row) MyGrid.MoveRow(move.Index, move.Delta);
            else MyGrid.MoveColumn(move.Index, move.Delta);
        }
    }

    private void DrawOperationAnimation(SKCanvas canvas, ActiveOperationAnimation active, float tileWidth,
        float tileHeight)
    {
        OperationAnimationPlan plan = active.Timeline.Plan;
        Array.Clear(active.MovingTiles);
        IReadOnlyList<OperationTileMotion> motions;
        if (active.Timeline.Style == OperationAnimationStyle.Logical)
        {
            motions = plan.LogicalMotions;
            foreach (OperationTileMotion motion in motions) active.MovingTiles[motion.TileId] = true;
        }
        else
        {
            motions = plan.GetPrimitiveMotions(active.Frame.PrimitiveIndex);
            foreach (OperationTileMotion motion in motions) active.MovingTiles[motion.TileId] = true;
        }

        var boardClip = new SKRect(tileView!.xx, tileView.yy, tileView.xx + tileView.width,
            tileView.yy + tileView.height);
        canvas.Save();
        canvas.ClipRect(boardClip);

        for (int y = 0; y < plan.Height; y++)
        for (int x = 0; x < plan.Width; x++)
        {
            int tileId = active.Timeline.Style == OperationAnimationStyle.Logical
                ? plan.TileAtStart(x, y)
                : plan.TileAtPrimitiveStart(active.Frame.PrimitiveIndex, x, y);
            if (active.MovingTiles[tileId]) continue;
            DrawOperationTile(canvas, active.TileImages[tileId], tileView.xx + (x + 0.5f) * tileWidth,
                tileView.yy + (y + 0.5f) * tileHeight, 1, tileWidth, tileHeight);
        }

        float progress = active.Frame.Progress;
        RecordedMove? primitiveMove = active.Timeline.Style == OperationAnimationStyle.Primitive
            ? plan.PrimitiveMoves[active.Frame.PrimitiveIndex]
            : null;
        if (active.Timeline.Style == OperationAnimationStyle.Logical &&
            plan.OperationId == PuzzleOperationIds.Shuffle && plan.LensCells.Count == 4)
        {
            foreach (var cell in plan.LensCells)
            {
                canvas.Save();
                canvas.ClipRect(new SKRect(tileView.xx + cell.X * tileWidth,
                    tileView.yy + cell.Y * tileHeight, tileView.xx + (cell.X + 1) * tileWidth,
                    tileView.yy + (cell.Y + 1) * tileHeight));
                DrawOperationMotions(canvas, active, motions, primitiveMove, progress, tileWidth, tileHeight);
                canvas.Restore();
            }
        }
        else
        {
            DrawOperationMotions(canvas, active, motions, primitiveMove, progress, tileWidth, tileHeight);
        }

        if (primitiveMove is not null)
        {
            var line = primitiveMove.Axis == RecordedMoveAxis.Row
                ? new SKRect(tileView.xx, tileView.yy + primitiveMove.Index * tileHeight,
                    tileView.xx + tileView.width, tileView.yy + (primitiveMove.Index + 1) * tileHeight)
                : new SKRect(tileView.xx + primitiveMove.Index * tileWidth, tileView.yy,
                    tileView.xx + (primitiveMove.Index + 1) * tileWidth, tileView.yy + tileView.height);
            canvas.DrawRoundRect(line, 6, 6, activeLinePaint);
        }

        canvas.Restore();
    }

    private void DrawOperationMotions(SKCanvas canvas, ActiveOperationAnimation active,
        IReadOnlyList<OperationTileMotion> motions, RecordedMove? primitiveMove, float progress, float tileWidth,
        float tileHeight)
    {
        foreach (OperationTileMotion motion in motions)
        {
            bool highlight = primitiveMove is not null && motion.SourceX == primitiveMove.StartX &&
                             motion.SourceY == primitiveMove.StartY;
            int poseCount = active.Timeline.Style == OperationAnimationStyle.Logical
                ? active.Timeline.Plan.GetLogicalPoseCount(motion.TileId)
                : 1;
            for (int poseIndex = 0; poseIndex < poseCount; poseIndex++)
            {
                float centerX;
                float centerY;
                float scale;
                if (active.Timeline.Style == OperationAnimationStyle.Logical)
                {
                    OperationTilePose pose = active.Timeline.Plan.GetLogicalPose(motion.TileId, progress, poseIndex);
                    centerX = tileView!.xx + pose.CenterX * tileWidth;
                    centerY = tileView.yy + pose.CenterY * tileHeight;
                    scale = pose.Scale;
                }
                else
                {
                    centerX = tileView!.xx + (motion.SourceX + 0.5f + motion.DeltaX * progress) * tileWidth;
                    centerY = tileView.yy + (motion.SourceY + 0.5f + motion.DeltaY * progress) * tileHeight;
                    scale = 1;
                }

                DrawOperationTile(canvas, active.TileImages[motion.TileId], centerX, centerY, scale,
                    tileWidth, tileHeight, highlight);
            }
        }
    }

    private void DrawOperationTile(SKCanvas canvas, SKImage image, float centerX, float centerY, float scale,
        float tileWidth, float tileHeight, bool highlight = false)
    {
        if (tileView is not { } view) return;
        float boardWidth = MyGrid.Width;
        float boardHeight = MyGrid.Height;
        var copies = OperationAnimationGeometry.GetVisibleWrappedCopies(
            (centerX - view.xx) / tileWidth, (centerY - view.yy) / tileHeight,
            scale, scale, boardWidth, boardHeight);
        var source = new SKRect(0, 0, image.Width, image.Height);
        foreach (var copy in copies)
        {
            float left = view.xx + copy.X * tileWidth - tileWidth * scale / 2;
            float top = view.yy + copy.Y * tileHeight - tileHeight * scale / 2;
            var destination = new SKRect(left, top, left + tileWidth * scale, top + tileHeight * scale);
            float cornerRadius = Math.Min(destination.Width, destination.Height) * 0.06f;
            using var rounded = new SKRoundRect(destination, cornerRadius, cornerRadius);
            canvas.Save();
            canvas.ClipRoundRect(rounded);
            canvas.DrawImage(image, source, destination, paint: tilePaint);
            canvas.Restore();
            canvas.DrawRoundRect(rounded, operationTileBorderPaint);
            if (highlight)
                canvas.DrawRoundRect(new SKRect(left + 3, top + 3,
                    destination.Right - 3, destination.Bottom - 3), 6, 6, activeLinePaint);
        }
    }
}