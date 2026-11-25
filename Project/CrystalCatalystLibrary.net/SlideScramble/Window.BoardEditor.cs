using SkiaSharp;

namespace SlideScramble;

public partial class Window
{
    private Tile[,]? boardDraft;
    private (int X, int Y)? editorSelection;
    private readonly Stack<((int X, int Y) First, (int X, int Y) Second)> editorUndo = new();
    private double editorOpenedAt;
    private string editorStatus = "";

    private static readonly string[] editorButtons = { "Undo", "Save Challenge", "Commit & Play", "Cancel" };
    private const float EditorToolbarWidth = 616;

    private void OpenBoardEditor()
    {
        if (IsShuffleLogic)
        {
            sessionMessage = "Board editing is unavailable in ShuffleLogic; use Shuffle-only challenges";
            return;
        }
        if (sessions.Mode == RecordingControllerMode.Recording || State.Mode == GameMode.Scrambling ||
            drag != null || Animations.Count != 0 || activeSlider != ActiveSlider.None)
        {
            sessionMessage = "Stop recording and finish the current move before editing";
            showMedia = true;
            return;
        }

        interaction.SetFocus(null);
        boardDraft = (Tile[,])MyGrid.Tiles.Clone();
        editorOpenedAt = wnd.uptimeSeconds();
        editorStatus = "Select two tiles to swap. Commit starts a fresh run.";
        wnd.QueueRedraw();
    }

    private void CloseBoardEditor()
    {
        double now = wnd.uptimeSeconds();
        State.ExcludePause(now - editorOpenedAt);
        lastPlaybackTick = now;
        boardDraft = null;
        editorSelection = null;
        editorUndo.Clear();
        wnd.QueueRedraw();
    }

    private void CommitBoard()
    {
        var controller = new SessionRecordingController(MyGrid, activeRules);
        Array.Copy(boardDraft!, MyGrid.Tiles, MyGrid.Tiles.Length);
        sessions = controller;
        savedSession = null;
        goal = null;
        State.Reset();
        moveHighlight = null;
        animationLockHoriz = null;
        animationOrdinal = 0;
        animationTileOffset = 0;
        ShowGridSetup = false;
        showMedia = false;
        sessionMessage = "";
        CloseBoardEditor();
    }

    private void SaveBoardChallenge()
    {
        try
        {
            var draft = boardDraft!;
            var cells = new int[draft.GetLength(0), draft.GetLength(1)];
            for (int y = 0; y < MyGrid.Height; y++)
                for (int x = 0; x < MyGrid.Width; x++)
                {
                    var tile = draft[y, x];
                    cells[y, x] = tile.OriginY * MyGrid.Width + tile.OriginX;
                }

            var recording = new SessionRecording(cells, Array.Empty<RecordedMove>());
            editorStatus = $"Saved to Challenges: {ChallengeCatalog.SaveEditedBoard(recording)}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            editorStatus = $"Could not save challenge: {ex.Message}";
        }
    }

    private static float EditorScale(int width) => Math.Min(1f, Math.Max(1, width - 16) / EditorToolbarWidth);

    private static SKRect EditorButton(int index)
    {
        float left = 142 + index * 116;
        return new SKRect(left, 7, left + 110, 35);
    }

    private void EditBoardClick(int x, int y)
    {
        wnd.GetSize(out int width, out int height);
        UpdateTileView(width, height);
        float scale = EditorScale(width);
        float toolbarLeft = (width - EditorToolbarWidth * scale) / 2;
        for (int i = 0; i < editorButtons.Length; i++)
        {
            if (!EditorButton(i).Contains((x - toolbarLeft) / scale, (y - 10) / scale)) continue;
            switch (i)
            {
                case 0:
                    if (editorUndo.TryPop(out var swap))
                    {
                        SwapDraftTiles(swap.First, swap.Second);
                        editorSelection = null;
                    }
                    break;
                case 1: SaveBoardChallenge(); break;
                case 2: CommitBoard(); break;
                case 3: CloseBoardEditor(); break;
            }
            return;
        }

        var view = tileView!;
        if (x < view.xx || y < view.yy || x >= view.xx + view.width || y >= view.yy + view.height) return;
        var cell = (
            X: Math.Min(MyGrid.Width - 1, (int)((x - view.xx) * MyGrid.Width / view.width)),
            Y: Math.Min(MyGrid.Height - 1, (int)((y - view.yy) * MyGrid.Height / view.height)));
        if (editorSelection is { } first)
        {
            if (first != cell)
            {
                SwapDraftTiles(first, cell);
                editorUndo.Push((first, cell));
            }
            editorSelection = null;
        }
        else
        {
            editorSelection = cell;
        }
    }

    private void SwapDraftTiles((int X, int Y) first, (int X, int Y) second)
    {
        var draft = boardDraft!;
        (draft[first.Y, first.X], draft[second.Y, second.X]) =
            (draft[second.Y, second.X], draft[first.Y, first.X]);
    }

    private void DrawBoardEditor(SKCanvas canvas, int width)
    {
        var view = tileView!;
        if (editorSelection is { } selected)
        {
            float tileWidth = view.width / MyGrid.Width;
            float tileHeight = view.height / MyGrid.Height;
            float x = view.xx + selected.X * tileWidth;
            float y = view.yy + selected.Y * tileHeight;
            float inset = Math.Min(5, Math.Min(tileWidth, tileHeight) / 4);
            canvas.DrawRoundRect(new SKRect(x + inset, y + inset,
                x + tileWidth - inset, y + tileHeight - inset), 6, 6, activeLinePaint);
        }

        float scale = EditorScale(width);
        canvas.Save();
        canvas.Translate((width - EditorToolbarWidth * scale) / 2, 10);
        canvas.Scale(scale);
        var toolbar = new SKRect(0, 0, EditorToolbarWidth, 42);
        canvas.DrawRoundRect(toolbar, 12, 12, hudBarPaint);
        canvas.DrawRoundRect(toolbar, 12, 12, hudChipBorderPaint);
        canvas.DrawText("Board Editor", 14, 26, hudBoldFont, hudTextPaint);
        for (int i = 0; i < editorButtons.Length; i++)
        {
            var button = EditorButton(i);
            var label = editorButtons[i];
            canvas.DrawRoundRect(button, 7, 7, hudChipPaint);
            var textPaint = i == 0 && editorUndo.Count == 0 ? hudMutedPaint : i == 2 ? hudAccentPaint : hudTextPaint;
            canvas.DrawText(label, button.MidX - footerFont.MeasureText(label) / 2,
                button.Top + 19, footerFont, textPaint);
        }
        canvas.Restore();

        canvas.Save();
        float statusY = view.yy + view.height + 26;
        canvas.ClipRect(new SKRect(8, statusY - 16, width - 8, statusY + 4));
        float statusX = Math.Max(8, (width - footerFont.MeasureText(editorStatus)) / 2);
        canvas.DrawText(editorStatus, statusX, statusY, footerFont, hudTextPaint);
        canvas.Restore();
    }
}
