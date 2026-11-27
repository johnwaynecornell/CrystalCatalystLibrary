using CrystalCatalystLibrary.net;
using CrystalOpenAL;
using CrystalOpenGL;
using CrystalSkia.net;
using Silk.NET.OpenGL;
using SkiaSharp;

namespace SlideScramble;

public partial class Window
{
    private bool DispatchBoardCommand(ShortcutKeys keys, int key)
    {
        for (int i = 0; i < boardCommands.Length; i++)
        {
            var command = boardCommands[i];
            if (!command.Entry.Enabled || !command.Matches(keys, key)) continue;
            if (commandMenu != null) ActivateMenuCommand(i);
            else command.Execute?.Invoke();
            return true;
        }
        return false;
    }

    private void ShowCommandMenu()
    {
        if (drag != null || activeSlider != ActiveSlider.None || Animations.Count != 0) return;
        lensInteraction.CancelGesture();
        var menu = new CrystalCommandMenu("SlideScramble Commands", boardCommands.Select(c => c.Entry), hudFont);
        menu.Activated += ActivateMenuCommand;
        commandMenu = menu;
        wnd.GetSize(out int width, out int height);
        var area = new System.Drawing.RectangleF(0, 0, width, height);
        popupHost.Show(menu, area);
        popupHost.SetLayout(area, menu.GetCenteredBounds(area));
        wnd.QueueRedraw();
    }

    private void ActivateMenuCommand(int index)
    {
        var command = boardCommands[index];
        if (command.Execute == null)
        {
            popupHost.Cancel();
            return;
        }

        popupHost.Accept(selectedIndex: index);
        command.Execute();
    }


    private static int CalculateSliderValue(float mouseX, float trackStartX, float trackEndX)
    {
        float t = (mouseX - trackStartX) / (trackEndX - trackStartX);
        int val = (int)Math.Round(2 + t * 7);
        return Math.Clamp(val, 2, 9);
    }

    private void OnMouseMove(CrystalWindow windowHandle, int x, int y)
    {
        if (activeOperationAnimation != null) return;
        if (boardDraft != null)
        {
            return;
        }
        if (popupHost.IsVisible)
        {
            popupHost.PointerMove(new System.Drawing.PointF(x, y));
            return;
        }
        interaction.PointerMove(x, y);
        if (activeSlider != ActiveSlider.None)
        {
            windowHandle.GetSize(out int width, out int height);
            float panelWidth = 360f;
            float panelX = (width - panelWidth) / 2f;
            float trackStartX = panelX + 130;
            float trackEndX = panelX + 270;

            if (activeSlider == ActiveSlider.Columns)
            {
                int newCols = CalculateSliderValue(x, trackStartX, trackEndX);
                if (newCols != MyGrid.Width)
                {
                    SetGridSize(newCols, MyGrid.Height);
                }
            }
            else if (activeSlider == ActiveSlider.Rows)
            {
                int newRows = CalculateSliderValue(x, trackStartX, trackEndX);
                if (newRows != MyGrid.Height)
                {
                    SetGridSize(MyGrid.Width, newRows);
                }
            }
            return;
        }

        if (lensInteraction.Phase != PortableLensInteractionPhase.Inactive)
        {
            if (lensInteraction.HasPendingGesture)
            {
                lensInteraction.UpdateGesturePointer(x, y);
                windowHandle.QueueRedraw();
            }
            windowHandle.QueueRedraw();
            return;
        }

        if (drag is { slideBlocked: false } && tileView != null)
        {
            int xd = x - drag.startX;
            int yd = y - drag.startY;
            
            if (animationLockHoriz == null)
            {
                const int slideDragThreshold = 4;
                if (Math.Max(Math.Abs(xd), Math.Abs(yd)) < slideDragThreshold) return;

                bool column = Math.Abs(xd) < Math.Abs(yd);
                var axis = column ? RecordedMoveAxis.Column : RecordedMoveAxis.Row;
                if (sessions.GetSlideInputRejection(axis) is { } rejection)
                {
                    drag.slideBlocked = true;
                    moveHighlight = null;
                    ShowBoardMessage(rejection);
                    return;
                }

                if (column)
                {
                    // Vertical drag -> column move
                    animationLockHoriz = true;
                    int rawOrdinal = drag.cellX;
                    animationOrdinal = Math.Clamp(rawOrdinal, 0, MyGrid.Width - 1);
                }
                else
                {
                    // Horizontal drag -> row move
                    animationLockHoriz = false;
                    int rawOrdinal = drag.cellY;
                    animationOrdinal = Math.Clamp(rawOrdinal, 0, MyGrid.Height - 1);
                }
            }
                
            if (animationLockHoriz == false)
            {
                animationTileOffset = -xd * MyGrid.Width / tileView.width;
            }
            else if (animationLockHoriz == true)
            {
                animationTileOffset = -yd * MyGrid.Height / tileView.height;
            }
        }
    }


    private void ShowBoardMessage(string message)
    {
        boardMessage = message;
        boardMessageUntil = Environment.TickCount64 + 2500;
        wnd.QueueRedraw();
    }

    private void OnMouseUp(CrystalWindow windowHandle, int button, int x, int y)
    {
        if (activeOperationAnimation != null) return;
        if (boardDraft != null)
        {
            lensDoubleClick.Reset();
            return;
        }
        if (popupHost.IsVisible)
        {
            lensDoubleClick.Reset();
            popupHost.PointerUp(new System.Drawing.PointF(x, y), (CrystalMouseButton)button);
            return;
        }
        if (button == (int)CrystalMouseButton.Left && drag is { slideBlocked: true })
            lensDoubleClick.Reset();
        else if (button == (int)CrystalMouseButton.Left && TryHandleLensDoubleClick(windowHandle, x, y))
            return;
        if (button != (int)CrystalMouseButton.Left) lensDoubleClick.Reset();
        if (lensInteraction.Phase != PortableLensInteractionPhase.Inactive)
        {
            if (button == (int)CrystalMouseButton.Left &&
                lensInteraction.Phase == PortableLensInteractionPhase.Accepted &&
                lensInteraction.HasPendingGesture)
            {
                PortableLensCell? target = null;
                if (tileView != null)
                {
                    int cellX = (int)Math.Floor((x - tileView.xx) * (double)MyGrid.Width / tileView.width);
                    int cellY = (int)Math.Floor((y - tileView.yy) * (double)MyGrid.Height / tileView.height);
                    if (lensInteraction.TryGetGestureTarget(cellX, cellY, out PortableLensCell mappedTarget,
                            out _, out _))
                        target = mappedTarget;
                }

                LensGestureRequest? request = target.HasValue
                    ? lensInteraction.CompleteGesture(target.Value)
                    : null;
                if (!target.HasValue) lensInteraction.CancelGesture();
                if (request != null) CommitLensGesture(request);
            }

            windowHandle.QueueRedraw();
            return;
        }
        interaction.PointerUp(x, y, (CrystalMouseButton)button);
        if (button == (int)CrystalMouseButton.Left)
        {
            if (activeSlider != ActiveSlider.None)
            {
                activeSlider = ActiveSlider.None;
                return;
            }

            if (drag != null && tileView != null)
            {
                int cellX = drag.cellX;
                int cellY = drag.cellY;
                drag = null;

                if (animationLockHoriz != null)
                {
                    bool horizontal = animationLockHoriz.Value;
                    int ordinal = animationOrdinal;
                    float currentOffset = animationTileOffset;
                    int delta = (int)Math.Round(currentOffset);

                    QueueSnapAnimation(horizontal, ordinal, currentOffset, delta, delta, cellX, cellY);
                }
                else
                {
                    animationLockHoriz = null;
                    animationTileOffset = 0;
                    moveHighlight = null;
                }
            }
        }
    }

    private void OnMouseDown(CrystalWindow windowHandle, int button, int x, int y)
    {
        if (activeOperationAnimation != null) return;
        if (boardDraft != null)
        {
            if (button == (int)CrystalMouseButton.Left) EditBoardClick(x, y);
            return;
        }
        if (button != (int)CrystalMouseButton.Left) return;

        // Route to popup when visible
        if (popupHost.IsVisible)
        {
            popupHost.PointerDown(new System.Drawing.PointF(x, y), CrystalMouseButton.Left);
            return;
        }

        windowHandle.GetSize(out int width, out int height);
        UpdateTileView(width, height);
        var media = GetMediaLayout(width, height);
        UpdateMediaEditorBounds(media);
        bool insideBoard = tileView != null && x >= tileView.xx && x < tileView.xx + tileView.width &&
                           y >= tileView.yy && y < tileView.yy + tileView.height;
        if (!ShowGridSetup && showMedia)
        {
            for (int i = 0; i < mediaLabels.Length; i++)
            {
                if (!media.Button(i).Contains(x, y)) continue;
                MediaAction(i);
                return;
            }
        }
        lensDoubleClick.BeginPress(x, y, Environment.TickCount64,
            insideBoard && CanHandleLensInput && HasRuntimeLensOperation);
        if (lensInteraction.Phase != PortableLensInteractionPhase.Inactive)
        {
            if (lensInteraction.Phase == PortableLensInteractionPhase.Positioning)
            {
                if (tileView != null && x >= tileView.xx && x < tileView.xx + tileView.width &&
                    y >= tileView.yy && y < tileView.yy + tileView.height)
                {
                    int anchorX = (int)((x - tileView.xx) * MyGrid.Width / tileView.width);
                    int anchorY = (int)((y - tileView.yy) * MyGrid.Height / tileView.height);
                    if (lensInteraction.SetPlacementAnchor(anchorX, anchorY))
                        sessionMessage = $"Lens anchor: {anchorX + 1},{anchorY + 1} · press Enter to accept";
                }
            }
            else if (CanHandleLensInput && tileView != null && x >= tileView.xx &&
                     x < tileView.xx + tileView.width && y >= tileView.yy && y < tileView.yy + tileView.height)
            {
                int cellX = (int)((x - tileView.xx) * MyGrid.Width / tileView.width);
                int cellY = (int)((y - tileView.yy) * MyGrid.Height / tileView.height);
                PortableLensCell? start = lensInteraction.CellAt(cellX, cellY);
                if (start.HasValue) lensInteraction.BeginGesture(start.Value, controlPressed, x, y);
            }

            windowHandle.QueueRedraw();
            return;
        }
        if (!ShowGridSetup && showMedia && filenameEditor.Bounds.Contains(x, y))
        {
            if (!filenameEditor.IsFocused) filenameBeforeEdit = filenameEditor.Text;
            interaction.PointerDown(x, y, CrystalMouseButton.Left);
            return;
        }
        interaction.SetFocus(null);
        if (!ShowGridSetup && showMedia)
        {
            if (new SKRect(media.X, media.Y, media.X + media.Width * media.Scale, media.Y + media.Height * media.Scale).Contains(x, y)) return;
        }

        // Check HUD Grid button hotspot
        var hudGridButtonRect = new SKRect(20, 10, 125, 50);
        if (hudGridButtonRect.Contains(x, y))
        {
            ShowGridSetup = !ShowGridSetup;
            return;
        }

        // Handle Setup Overlay interactions
        if (ShowGridSetup)
        {
            float panelWidth = 360f;
            float panelHeight = 220f;
            float panelX = (width - panelWidth) / 2f;
            float panelY = (height - panelHeight) / 2f;

            var panelRect = new SKRect(panelX, panelY, panelX + panelWidth, panelY + panelHeight);
            var closeRect = new SKRect(panelX + panelWidth - 36, panelY + 10, panelX + panelWidth - 8, panelY + 38);

            float trackStartX = panelX + 130;
            float trackEndX = panelX + 270;
            var colsHitRect = new SKRect(panelX + 20, panelY + 55, panelX + panelWidth - 20, panelY + 95);
            var rowsHitRect = new SKRect(panelX + 20, panelY + 105, panelX + panelWidth - 20, panelY + 145);

            if (closeRect.Contains(x, y))
            {
                ShowGridSetup = false;
                return;
            }
            else if (colsHitRect.Contains(x, y))
            {
                activeSlider = ActiveSlider.Columns;
                int newCols = CalculateSliderValue(x, trackStartX, trackEndX);
                if (newCols != MyGrid.Width)
                {
                    SetGridSize(newCols, MyGrid.Height);
                }
                return;
            }
            else if (rowsHitRect.Contains(x, y))
            {
                activeSlider = ActiveSlider.Rows;
                int newRows = CalculateSliderValue(x, trackStartX, trackEndX);
                if (newRows != MyGrid.Height)
                {
                    SetGridSize(MyGrid.Width, newRows);
                }
                return;
            }
            else if (panelRect.Contains(x, y))
            {
                // Click inside panel consumed
                return;
            }
            else
            {
                // Click outside panel closes it
                ShowGridSetup = false;
                return;
            }
        }

        // Board dragging hit test
        if (sessions.Mode != RecordingControllerMode.Playback && State.Mode != GameMode.Scrambling &&
            tileView != null && Animations.Count == 0 && drag == null)
        {
            // Check if mouse is inside the board bounds
            if (x >= tileView.xx && x < tileView.xx + tileView.width &&
                y >= tileView.yy && y < tileView.yy + tileView.height)
            {
                drag = new Drag()
                {
                    startX = x,
                    startY = y,
                    cellX = (int)((x - tileView.xx) * MyGrid.Width / tileView.width),
                    cellY = (int)((y - tileView.yy) * MyGrid.Height / tileView.height),
                };
                moveHighlight = (drag.cellX, drag.cellY);
            }
        }
    }

    private bool TryHandleLensDoubleClick(CrystalWindow windowHandle, int x, int y)
    {
        if (!lensDoubleClick.CompletePress(x, y, Environment.TickCount64)) return false;
        windowHandle.GetSize(out int width, out int height);
        UpdateTileView(width, height);
        if (tileView == null || x < tileView.xx || x >= tileView.xx + tileView.width ||
            y < tileView.yy || y >= tileView.yy + tileView.height ||
            sessions.Mode == RecordingControllerMode.Playback || State.Mode == GameMode.Scrambling ||
            Animations.Count != 0 || popupHost.IsVisible || ShowGridSetup || boardDraft != null ||
            !HasRuntimeLensOperation)
            return false;

        int anchorX = (int)((x - tileView.xx) * MyGrid.Width / tileView.width);
        int anchorY = (int)((y - tileView.yy) * MyGrid.Height / tileView.height);
        bool positioned = lensInteraction.Phase == PortableLensInteractionPhase.Inactive
            ? lensInteraction.BeginPlacement(MyGrid.Width, MyGrid.Height, anchorX, anchorY) &&
              lensInteraction.AcceptPosition()
            : lensInteraction.RepositionAndAccept(anchorX, anchorY);
        if (!positioned) return false;

        drag = null;
        animationLockHoriz = null;
        animationTileOffset = 0;
        moveHighlight = null;
        sessionMessage = $"Lens accepted at {anchorX + 1},{anchorY + 1}; drag between neighboring symbols";
        windowHandle.QueueRedraw();
        return true;
    }
}
