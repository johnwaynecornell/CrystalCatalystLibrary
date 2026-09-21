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
    public static EnvironmentInfo Detect(string executable, TimeSpan timeout)
    {
        string os = RuntimeInformation.OSDescription;
        string? display = Environment.GetEnvironmentVariable("DISPLAY");
        string? waylandDisplay = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");

        bool wlCopy = IsCommandAvailable("wl-copy");
        bool wlPaste = IsCommandAvailable("wl-paste");

        // Ask the native dispatcher; environment variables alone cannot tell us
        // which backend the loaded native library actually uses.
        var probe = ProcessRunner.Run(executable, new[] { "-diag", "show", "avail" }, timeout: timeout);
        const string marker = "Clipboard backend: ";
        string? reported = probe.StdErr.Split('\n')
            .FirstOrDefault(line => line.Contains(marker, StringComparison.Ordinal));
        string route = reported == null
            ? $"Unreported by native library (probe exit {probe.ExitCode}, timedOut={probe.TimedOut})"
            : reported[(reported.IndexOf(marker, StringComparison.Ordinal) + marker.Length)..].Trim();
        if (reported != null && (probe.ExitCode != 0 || probe.TimedOut))
            route += $" (probe failed: exit {probe.ExitCode}, timedOut={probe.TimedOut})";

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
