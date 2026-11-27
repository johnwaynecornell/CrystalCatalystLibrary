using System.Runtime.InteropServices;
using System.Text;
using CrystalCatalystLibrary.net;

namespace SlideScramble;

public partial class Window
{
    private const string ClipboardTextFormat = "text/plain";
    private DataInterchange? clipboardData;
    private byte[]? clipboardBytes;
    private string? clipboardError;

    private void CopyBoard() =>
        ShowBoardMessage(ClipboardJson.Copy("Board", ClipboardJson.Board(MyGrid.Tiles), WriteClipboardText));

    private void CopyLens() =>
        ShowBoardMessage(ClipboardJson.Copy("Lens", ClipboardJson.Lens(MyGrid.Tiles, lensInteraction), WriteClipboardText));

    private string? WriteClipboardText(string text)
    {
        if (clipboardData == null)
        {
            clipboardData = DataInterchange.Create();
            clipboardData.FormatAdd(ClipboardTextFormat);
        }

        var previousBytes = clipboardBytes;
        clipboardBytes = Encoding.UTF8.GetBytes(text);
        clipboardError = null;
        try
        {
            // Windows can materialize the text immediately. X11 serves it from this
            // window on demand, even when no clipboard manager is running.
            if (OperatingSystem.IsWindows())
                wnd.ClipboardCopyPersist(clipboardData);
            else
                wnd.ClipboardCopy(clipboardData);
            return clipboardError;
        }
        catch
        {
            clipboardBytes = previousBytes;
            throw;
        }
        finally
        {
            if (clipboardError != null) clipboardBytes = previousBytes;
        }
    }

    private void ProvideClipboardText(CrystalWindow window, DataInterchange data, string format)
    {
        if (data != clipboardData || format != ClipboardTextFormat || clipboardBytes is not { } bytes) return;

        try
        {
            var buffer = Marshal.AllocHGlobal(bytes.Length);
            try
            {
                Marshal.Copy(bytes, 0, buffer, bytes.Length);
                data.SelectionSet(format, buffer, (IntPtr)bytes.Length);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        catch (Exception ex)
        {
            clipboardError = ex.Message;
            ShowBoardMessage($"Could not provide clipboard text: {ex.Message}");
        }
    }
}
