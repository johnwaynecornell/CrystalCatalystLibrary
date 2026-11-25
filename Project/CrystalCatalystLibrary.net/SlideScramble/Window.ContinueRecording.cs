using System.Drawing;
using CrystalCatalystLibrary.net;
using CrystalSkia.net;
using SkiaSharp;

namespace SlideScramble;

public partial class Window
{
    private void ShowContinueRecordingDialog()
    {
        if (sessions.Mode != RecordingControllerMode.Playback)
            throw new InvalidOperationException("Use Moves to inspect a recording before continuing.");

        int position = sessions.Position;
        if (!sessions.IsOperationBoundary(position))
            throw new InvalidOperationException("Continue recording only at a complete logical-operation boundary.");
        sessions.SetAutoplay(false);
        int discardCount = sessions.MoveCount - position;
        var controller = sessions;
        var dialog = new ContinueRecordingDialog(this, position, discardCount,
            () => ConfirmContinueRecording(controller, position));
        continueRecordingDialog = dialog;

        wnd.GetSize(out int width, out int height);
        var area = new RectangleF(0, 0, width, height);
        popupHost.Show(dialog, area);
        popupHost.SetLayout(area, ContinueRecordingDialog.CenterIn(area));
        wnd.QueueRedraw();
    }

    private void DrawContinueRecordingDialog(SKCanvas canvas, int width, int height)
    {
        var area = new RectangleF(0, 0, width, height);
        popupHost.SetLayout(area, ContinueRecordingDialog.CenterIn(area));
        popupHost.OnDraw(canvas);
    }

    private sealed class ContinueRecordingDialog(
        Window owner,
        int keepCount,
        int discardCount,
        Action confirm) : ICrystalSkia
    {
        private const float Width = 400;
        private const float Height = 180;
        private static readonly SKRect CancelButton = new(178, 132, 272, 164);
        private static readonly SKRect ConfirmButton = new(280, 132, 382, 164);

        public RectangleF Bounds { get; set; } = new(0, 0, Width, Height);

        public static RectangleF CenterIn(RectangleF area)
        {
            float scale = Math.Min(1, Math.Min(Math.Max(1, area.Width - 20) / Width,
                Math.Max(1, area.Height - 20) / Height));
            return new RectangleF(area.X + (area.Width - Width * scale) / 2,
                area.Y + (area.Height - Height * scale) / 2, Width * scale, Height * scale);
        }

        public void SetFocus(bool focused)
        {
        }

        public void OnIdle(double timeSeconds)
        {
        }

        public void TextInput(string text)
        {
        }

        public void KeyDown(KeyCode key, bool shift, bool control)
        {
            if (control) return;
            if (key is KeyCode.Enter or KeyCode.KP_Enter) confirm();
            else if (key == KeyCode.Escape) owner.popupHost.Cancel();
        }

        private SKPoint Local(PointF point) =>
            new(point.X * Width / Bounds.Width, point.Y * Height / Bounds.Height);

        public void PointerDown(PointF position, CrystalMouseButton button)
        {
            if (button != CrystalMouseButton.Left) return;
            var point = Local(position);
            if (ConfirmButton.Contains(point.X, point.Y)) confirm();
            else if (CancelButton.Contains(point.X, point.Y)) owner.popupHost.Cancel();
        }

        public void PointerMove(PointF position)
        {
        }

        public void PointerUp(PointF position, CrystalMouseButton button)
        {
        }

        public void OnDraw(SKCanvas canvas)
        {
            canvas.Save();
            canvas.Translate(Bounds.X, Bounds.Y);
            canvas.Scale(Bounds.Width / Width, Bounds.Height / Height);
            canvas.DrawRoundRect(new SKRect(0, 0, Width, Height), 14, 14, owner.hudBarPaint);
            canvas.DrawText("Continue Recording?", 20, 34, owner.panelTitleFont, owner.hudTextPaint);
            canvas.DrawText($"Keep the first {keepCount} primitive moves.", 20, 67, owner.hudFont, owner.hudTextPaint);
            canvas.DrawText($"Discard {discardCount} later primitive moves from this working copy.", 20, 93,
                owner.footerFont, owner.hudMutedPaint);
            canvas.DrawText("The original file is unchanged until you Save.", 20, 115,
                owner.footerFont, owner.hudMutedPaint);
            DrawButton(canvas, CancelButton, "Cancel");
            DrawButton(canvas, ConfirmButton, "Confirm");
            canvas.Restore();
        }

        private void DrawButton(SKCanvas canvas, SKRect bounds, string label)
        {
            canvas.DrawRoundRect(bounds, 6, 6, owner.hudChipPaint);
            canvas.DrawText(label, bounds.MidX - owner.hudFont.MeasureText(label) / 2,
                bounds.Top + 21, owner.hudFont, owner.hudTextPaint);
        }
    }
}