using System.Drawing;
using CrystalCatalystLibrary.net;
using CrystalSkia.net;
using SkiaSharp;

namespace SlideScramble;

public partial class Window
{
    private ChallengeGenerator? challengeGenerator;
    private int[,]? generationStart;
    private List<RecordedMove>? generatedMoves;
    private Queue<OperationPlan>? pendingGeneratedOperations;
    private SessionRecording? generatedChallengeSolution;
    private SessionRecording? verifiedBoardSolution;
    private int challengeScale = 1;
    private int SingleScrambleMoves => 2 * (MyGrid.Width + MyGrid.Height);

    private void ShowChallengeGenerator()
    {
        if (!CanChangeBoard)
        {
            showMedia = true;
            sessionMessage = "Stop recording or playback and finish the current move first";
            return;
        }

        var control = new ChallengeGenerator(this, challengeScale);
        challengeGenerator = control;
        wnd.GetSize(out int width, out int height);
        var window = new RectangleF(0, 0, width, height);
        popupHost.Show(control, window);
        popupHost.SetLayout(window, ChallengeGenerator.CenterIn(window));
        wnd.QueueRedraw();
    }

    private void DrawChallengeGenerator(SKCanvas canvas, int width, int height)
    {
        var window = new RectangleF(0, 0, width, height);
        popupHost.SetLayout(window, ChallengeGenerator.CenterIn(window));
        popupHost.OnDraw(canvas);
    }

    private void BeginGeneratedChallenge(int multiplier)
    {
        popupHost.Accept();
        if (HasClassicMathRules)
        {
            challengeScale = multiplier;
            pendingGeneratedOperations = null;
            generatedChallengeSolution = null;
            ShowGridSetup = false;
            MyGrid.Reset();
            sessions = new SessionRecordingController(MyGrid, activeRules.WithUIEnabled(sessions.RuntimeUIEnabled));
            savedSession = null;
            goal = null;
            moveHighlight = null;
            sessionMessage = "";
            generationStart = new int[MyGrid.Height, MyGrid.Width];
            for (int y = 0; y < MyGrid.Height; y++)
                for (int x = 0; x < MyGrid.Width; x++)
                    generationStart[y, x] = y * MyGrid.Width + x;
            ScrambleCount = SingleScrambleMoves * multiplier;
            generatedMoves = new List<RecordedMove>(ScrambleCount);
            State.StartScrambling();
            return;
        }

        BeginRuleAwareChallenge(multiplier, preserveBoard: false);
    }

    private void BeginRuleAwareChallenge(int multiplier, bool preserveBoard)
    {
        if (!CanChangeBoard)
        {
            sessionMessage = "Finish the current activity before generating a challenge";
            return;
        }

        try
        {
            var rules = activeRules.WithUIEnabled(sessions.RuntimeUIEnabled);
            var evaluator = new OperationRuleEvaluator(rules.Registry);
            int operationCount = checked(SingleScrambleMoves * multiplier);
            int[,] start = CaptureBoardCells();
            bool keepBoard = preserveBoard && ShuffleChallengeFactory.HasVerifiedStart(start, verifiedBoardSolution, rules);
            var challenge = keepBoard
                ? ShuffleChallengeFactory.GenerateRuleAware(start, verifiedBoardSolution, operationCount, rules,
                    evaluator, rand)
                : ShuffleChallengeFactory.GenerateRuleAware(MyGrid.Width, MyGrid.Height, operationCount, rules,
                    evaluator, rand);

            if (!preserveBoard) challengeScale = multiplier;
            ShowGridSetup = false;
            if (!keepBoard)
            {
                MyGrid.Reset();
                sessions = new SessionRecordingController(MyGrid, rules);
                savedSession = null;
            }
            goal = null;
            moveHighlight = null;
            generationStart = null;
            generatedMoves = null;
            pendingGeneratedOperations = new Queue<OperationPlan>(challenge.ScramblePlans);
            generatedChallengeSolution = challenge.Solution;
            ScrambleCount = challenge.ScramblePlans.Sum(plan => plan.Expansion.Count);
            animationLockHoriz = null;
            animationTileOffset = 0;
            lensInteraction.Cancel();
            State.StartScrambling();
            sessionMessage = preserveBoard && !keepBoard
                ? "Starting from identity because the previous board had no verified solution under these rules"
                : "Generating a verified challenge…";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or OverflowException)
        {
            showMedia = true;
            sessionMessage = ex.Message;
        }
    }

    private void TrackSolveMove(int[,] before, IOperationIntent intent)
    {
        var rules = activeRules.WithUIEnabled(sessions.RuntimeUIEnabled);
        verifiedBoardSolution = ShuffleChallengeFactory.TrackSolveMove(before, CaptureBoardCells(), intent,
            verifiedBoardSolution, rules);
    }

    private RecordedMove NextScrambleMove()
    {
        bool column = rand.Next(2) == 0;
        int ordinal = rand.Next(column ? MyGrid.Width : MyGrid.Height);
        int length = column ? MyGrid.Height : MyGrid.Width;
        int delta = rand.Next(length) - length / 2;
        if (delta == 0) delta = 1;
        var axis = column ? RecordedMoveAxis.Column : RecordedMoveAxis.Row;
        var move = new RecordedMove(axis, ordinal, delta);
        if (generatedMoves == null || ScrambleCount != 1) return move;

        // If the last move would solve the board, a move on the other axis cannot have the same effect.
        var finalTiles = (Tile[,])MyGrid.Tiles.Clone();
        if (column) LogicalMoves.MoveColumn(finalTiles, ordinal, delta);
        else LogicalMoves.MoveRow(finalTiles, ordinal, delta);
        for (int y = 0; y < MyGrid.Height; y++)
            for (int x = 0; x < MyGrid.Width; x++)
                if (finalTiles[y, x].OriginX != x || finalTiles[y, x].OriginY != y) return move;
        return new RecordedMove(column ? RecordedMoveAxis.Row : RecordedMoveAxis.Column, 0, 1);
    }

    private void FinishGeneratedChallenge()
    {
        if (generatedChallengeSolution is { } generatedSolution)
        {
            generatedChallengeSolution = null;
            pendingGeneratedOperations = null;
            generatedMoves = null;
            int[,] currentCells = OperationBoardSnapshot.FromGrid(MyGrid).Cells;
            if (!ChallengeBoardsEqual(currentCells, generatedSolution.InitialCells))
            {
                sessionMessage =
                    "The animated scramble did not match its verified challenge; no solution was attached";
                return;
            }

            verifiedBoardSolution = generatedSolution;
            sessions.LoadPlayback(generatedSolution);
            sessions.StopPlayback();
            savedSession = generatedSolution;
            goal = generatedSolution.GetVerifiedGoalMoveCost();
            sessionMessage = "Challenge ready. Save or replay the solution here.";
            return;
        }

        if (generatedMoves == null) return;
        pendingGeneratedOperations = null;
        var solution = new SessionRecording(generationStart!,
            generatedMoves.Select(move => RecordedOperation.CreateSlide(move)),
            activeRules.WithUIEnabled(sessions.RuntimeUIEnabled)).Reverse();
        generationStart = null;
        generatedMoves = null;
        sessions.LoadPlayback(solution);
        sessions.StopPlayback();
        savedSession = solution;
        goal = solution.GetVerifiedGoal();
        sessionMessage = "Challenge ready. Save or replay the solution here.";
    }

    private static bool ChallengeBoardsEqual(int[,] left, int[,] right)
    {
        if (left.GetLength(0) != right.GetLength(0) || left.GetLength(1) != right.GetLength(1)) return false;
        for (int y = 0; y < left.GetLength(0); y++)
        for (int x = 0; x < left.GetLength(1); x++)
            if (left[y, x] != right[y, x])
                return false;
        return true;
    }

    private sealed class ChallengeGenerator(Window owner, int value) : ICrystalSkia
    {
        private const float Width = 360;
        private const float Height = 172;
        private const float TrackLeft = 36;
        private const float TrackRight = Width - 36;
        private const float TrackY = 90;
        private static readonly SKRect CancelButton = new(160, 124, 235, 154);
        private static readonly SKRect GenerateButton = new(243, 124, 338, 154);
        private int multiplier = value;
        private bool dragging;

        public RectangleF Bounds { get; set; } = new(0, 0, Width, Height);

        public static RectangleF CenterIn(RectangleF area)
        {
            float scale = Math.Min(1, Math.Min(Math.Max(1, area.Width - 20) / Width,
                Math.Max(1, area.Height - 20) / Height));
            return new RectangleF(area.X + (area.Width - Width * scale) / 2,
                area.Y + (area.Height - Height * scale) / 2, Width * scale, Height * scale);
        }

        public void SetFocus(bool focused) => dragging = false;

        public void OnIdle(double timeSeconds) { }
        public void TextInput(string text) { }

        public void KeyDown(KeyCode key, bool shift, bool control)
        {
            if (control) return;
            switch (key)
            {
                case KeyCode.Left: multiplier = Math.Max(1, multiplier - 1); break;
                case KeyCode.Right: multiplier = Math.Min(10, multiplier + 1); break;
                case KeyCode.Enter:
                case KeyCode.KP_Enter: owner.BeginGeneratedChallenge(multiplier); break;
            }
        }

        private SKPoint Local(PointF point) => new(point.X * Width / Bounds.Width, point.Y * Height / Bounds.Height);

        private void SetSlider(float x) =>
            multiplier = 1 + (int)Math.Round(9 * Math.Clamp((x - TrackLeft) / (TrackRight - TrackLeft), 0, 1));

        public void PointerDown(PointF position, CrystalMouseButton button)
        {
            if (button != CrystalMouseButton.Left) return;
            var point = Local(position);
            if (GenerateButton.Contains(point.X, point.Y)) owner.BeginGeneratedChallenge(multiplier);
            else if (CancelButton.Contains(point.X, point.Y)) owner.popupHost.Cancel();
            else if (Math.Abs(point.Y - TrackY) <= 14)
            {
                dragging = true;
                SetSlider(point.X);
            }
        }

        public void PointerMove(PointF position)
        {
            if (dragging) SetSlider(Local(position).X);
        }

        public void PointerUp(PointF position, CrystalMouseButton button)
        {
            if (button != CrystalMouseButton.Left || !dragging) return;
            SetSlider(Local(position).X);
            dragging = false;
        }

        public void OnDraw(SKCanvas canvas)
        {
            canvas.Save();
            canvas.Translate(Bounds.X, Bounds.Y);
            canvas.Scale(Bounds.Width / Width, Bounds.Height / Height);
            canvas.DrawRoundRect(new SKRect(0, 0, Width, Height), 14, 14, owner.hudBarPaint);
            canvas.DrawText("Generate Challenge", 22, 34, owner.panelTitleFont, owner.hudTextPaint);
            canvas.DrawText($"{multiplier}× scramble  ·  {multiplier * owner.SingleScrambleMoves} operations",
                22, 62, owner.hudFont, owner.hudTextPaint);

            float thumb = TrackLeft + (TrackRight - TrackLeft) * (multiplier - 1) / 9;
            canvas.DrawLine(TrackLeft, TrackY, TrackRight, TrackY, owner.sliderTrackPaint);
            canvas.DrawLine(TrackLeft, TrackY, thumb, TrackY, owner.sliderFillPaint);
            canvas.DrawCircle(thumb, TrackY, 7, owner.sliderThumbPaint);
            canvas.DrawText("1", TrackLeft - 4, 112, owner.footerFont, owner.hudMutedPaint);
            canvas.DrawText("10", TrackRight - 8, 112, owner.footerFont, owner.hudMutedPaint);
            DrawButton(canvas, CancelButton, "Cancel");
            DrawButton(canvas, GenerateButton, "Generate");
            canvas.Restore();
        }

        private void DrawButton(SKCanvas canvas, SKRect bounds, string label)
        {
            canvas.DrawRoundRect(bounds, 6, 6, owner.hudChipPaint);
            canvas.DrawText(label, bounds.MidX - owner.hudFont.MeasureText(label) / 2,
                bounds.Top + 20, owner.hudFont, owner.hudTextPaint);
        }
    }
}