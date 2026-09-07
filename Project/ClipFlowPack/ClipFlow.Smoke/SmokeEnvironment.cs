using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ClipFlow.Smoke;

public record EnvironmentInfo(
    string OS,
    string? Display,
    string? WaylandDisplay,
    bool WlCopyAvailable,
    bool WlPasteAvailable,
    string PersistenceRoute);

public static class SmokeEnvironment
{
    public static EnvironmentInfo Detect()
    {
        string os = RuntimeInformation.OSDescription;
        string? display = Environment.GetEnvironmentVariable("DISPLAY");
        string? waylandDisplay = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");

        bool wlCopy = IsCommandAvailable("wl-copy");
        bool wlPaste = IsCommandAvailable("wl-paste");

        string route;
        if (OperatingSystem.IsWindows())
        {
            route = "Windows native OLE clipboard";
        }
        else if (OperatingSystem.IsLinux())
        {
            if (!string.IsNullOrEmpty(waylandDisplay) && wlCopy && wlPaste)
            {
                route = "Wayland (wl-copy / wl-paste fallback)";
            }
            else if (!string.IsNullOrEmpty(display))
            {
                route = "X11 (CLIPBOARD_MANAGER / Xlib)";
            }
            else
            {
                route = "Unknown / headless Linux";
            }
        }
        else if (OperatingSystem.IsMacOS())
        {
            route = "macOS native clipboard";
        }
        else
        {
            route = "Generic";
        }

        return new EnvironmentInfo(os, display, waylandDisplay, wlCopy, wlPaste, route);
    }

    private static bool IsCommandAvailable(string command)
    {
        if (OperatingSystem.IsWindows()) return false;
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "which",
                ArgumentList = { command },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (proc == null) return false;
            proc.WaitForExit(1000);
            return proc.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
