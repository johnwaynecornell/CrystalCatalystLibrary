using CrystalCatalystLibrary.net;
using CrystalOpenAL;
using CrystalOpenGL;
using CrystalSkia.net;
using Silk.NET.OpenGL;
using SkiaSharp;

namespace SlideScramble;

public partial class Window
{
    public void UpdateTileView(int width = 0, int height = 0)
    {
        if (width <= 0 || height <= 0)
        {
            if (wnd != null)
            {
                wnd.GetSize(out width, out height);
            }
            else
            {
                width = 850;
                height = 700;
            }
        }

        if (tileView == null)
        {
            tileView = new TileView();
        }

        float topHeaderHeight = 62f;
        float footerHeight = 36f;
        float availableWidth = Math.Max(1f, width - 40f);
        float availableHeight = Math.Max(1f, height - topHeaderHeight - footerHeight - 20f);

        float tileSize = Math.Min(
            availableWidth / MyGrid.Width,
            availableHeight / MyGrid.Height);

        float boardWidth = tileSize * MyGrid.Width;
        float boardHeight = tileSize * MyGrid.Height;

        tileView.width = boardWidth;
        tileView.height = boardHeight;
        tileView.xx = (width - boardWidth) / 2f;
        tileView.yy = topHeaderHeight + (availableHeight - boardHeight) / 2f;
    }

    public void SetGridSize(int width, int height)
    {
        width = Math.Clamp(width, 2, 9);
        height = Math.Clamp(height, 2, 9);
        if (!activeRules.IsBoardCompatible(width, height))
        {
            sessionMessage = activeRules.GetBoardCompatibilityMessage(width, height) ??
                             "The selected rules are not compatible with this board";
            return;
        }
        if (CanChangeBoard)
        {
            MyGrid = new Grid(width, height);
            sessions = new SessionRecordingController(MyGrid, activeRules);
            State.Reset();
            goal = null;
            animationLockHoriz = null;
            animationTileOffset = 0;
            lensInteraction.Cancel();
            UpdateTileView();
            string? compatibilityMessage = activeRules.GetBoardCompatibilityMessage(width, height);
            if (compatibilityMessage != null) sessionMessage = compatibilityMessage;
        }
    }

    public void SetGridSize(int size) => SetGridSize(size, size);

    private void QueueSnapAnimation(bool horizontal, int ordinal, float startOffset, float targetOffset, int delta, int cellX = 0, int cellY = 0)
    {
        Animation anim = new Animation();
        double start = -1;
        float diff = targetOffset - startOffset;
        double duration = Math.Max(0.08, Math.Min(0.25, Math.Abs(diff) * 0.2));

        anim.animation = (window, animation1) =>
        {
            double time = wnd.uptimeSeconds();
            if (start == -1) start = time;
            double elapsed = time - start;

            if (elapsed >= duration)
            {
                animationLockHoriz = null;
                animationTileOffset = 0;
                moveHighlight = null;
                if (delta != 0)
                {
                    var axis = horizontal ? RecordedMoveAxis.Column : RecordedMoveAxis.Row;
                    if (sessions.GetSlideInputRejection(axis) is { } rejection)
                    {
                        ShowBlockedSlideMessage(rejection);
                        return false;
                    }

                    int[,]? before = UseClassicScramble ? null : CaptureBoardCells();
                    bool committed;
                    if (!horizontal)
                    {
                        committed = sessions.MoveRow(ordinal, delta, cellX, cellY);
                    }
                    else
                    {
                        committed = sessions.MoveColumn(ordinal, delta, cellX, cellY);
                    }
                    if (committed)
                    {
                        if (before != null)
                            TrackSolveMove(before, new SlideOperationIntent(axis, ordinal, delta));
                        State.RecordMove(wnd.uptimeSeconds());
                    }
                    if (committed && State.CheckSolved(MyGrid, wnd.uptimeSeconds()))
                    {
                        Audio.PlayVictoryChime();
                    }
                }
                return false;
            }

            float t = Math.Clamp((float)(elapsed / duration), 0f, 1f);
            float progress = 1f - MathF.Pow(1f - t, 3f); // cubic-out easing

            animationLockHoriz = horizontal;
            animationOrdinal = ordinal;
            animationTileOffset = startOffset + diff * progress;

            return true;
        };

        Animations.Enqueue(anim);
    }

    private void OnIdle(CrystalWindow windowHandle)
    {
        if (boardDraft != null)
        {
            windowHandle.QueueRedraw();
            return;
        }
        interaction.OnIdle(windowHandle.uptimeSeconds());
        if (popupHost.IsVisible)
        {
            popupHost.OnIdle(windowHandle.uptimeSeconds());
            windowHandle.QueueRedraw();
            return;
        }
        if (sessions.IsAutoplaying && Animations.Count == 0 && drag == null && wnd.uptimeSeconds() - lastPlaybackTick >= 0.4)
        {
            QueuePlaybackAnimation(true);
            lastPlaybackTick = wnd.uptimeSeconds();
        }
        if (Animations.TryPeek(out Animation? animation))
        {
            if (animation?.animation != null && !animation.animation(this, animation))
            {
                Animations.Dequeue();
            }
        }
        else // no current animation
        {
            if (ScrambleCount <= 0 && State.Mode == GameMode.Scrambling)
            {
                FinishGeneratedChallenge();
                State.FinishScrambling(wnd.uptimeSeconds());
                Audio.PlayGameStartChime();
            }
            if (State.Mode == GameMode.Scrambling)
            {
                if (pendingGeneratedOperations is { } pending)
                {
                    if (pending.Count == 0)
                    {
                        ScrambleCount = 0;
                        windowHandle.QueueRedraw();
                        return;
                    }
                    OperationPlan operation = pending.Dequeue();
                    ScrambleCount -= operation.Expansion.Count;
                    BeginScrambleOperationAnimation(operation);
                }
                else
                {
                    RecordedMove move = NextScrambleMove();
                    bool column = move.Axis == RecordedMoveAxis.Column;
                    int ordinal = move.Index;
                    int delta = move.Delta;

                    Animation anim = new Animation();
                    double start = -1;
                    double duration = OperationAnimationTimeline.LogicalDurationSeconds;
                    anim.animation = (window, animation1) =>
                    {
                        double time = wnd.uptimeSeconds();

                        if (start == -1) start = time;
                        double elapsed = time - start;

                        if (elapsed >= duration)
                        {
                            animationLockHoriz = null;
                            animationTileOffset = 0;
                            if (column) MyGrid.MoveColumn(ordinal, delta);
                            else MyGrid.MoveRow(ordinal, delta);
                            generatedMoves?.Add(move);
                            return false;
                        }
                        animationLockHoriz = column;
                        animationOrdinal = ordinal;
                        float t = Math.Clamp((float)(elapsed / duration), 0f, 1f);
                        float progress = t * t * (3f - 2f * t); // smoothstep
                        animationTileOffset = delta * progress;
                        return true;
                    };
                    Animations.Enqueue(anim);
                    ScrambleCount--;
                }
            }
        }

        windowHandle.QueueRedraw();
    }
}