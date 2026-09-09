using System.Diagnostics;

namespace ClipFlow.Smoke;

public class Program
{
    public static int Main(string[] args)
    {
        string? clipFlowPath = null;
        string? caseFilter = null;
        bool keepTemp = false;
        bool verbose = false;
        bool diag = false;
        TimeSpan timeout = TimeSpan.FromSeconds(10);

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--clipflow" && i + 1 < args.Length)
            {
                clipFlowPath = args[++i];
            }
            else if (args[i] == "--case" && i + 1 < args.Length)
            {
                caseFilter = args[++i];
            }
            else if (args[i] == "--keep-temp")
            {
                keepTemp = true;
            }
            else if (args[i] == "--verbose" || args[i] == "-v")
            {
                verbose = true;
            }
            else if (args[i] == "--diag")
            {
                diag = true;
            }
            else if (args[i] == "--timeout" && i + 1 < args.Length)
            {
                if (double.TryParse(args[++i], out double sec))
                {
                    timeout = TimeSpan.FromSeconds(sec);
                }
            }
            else if (args[i] == "--help" || args[i] == "-h")
            {
                PrintHelp();
                return 0;
            }
        }

        ProcessRunner.EnableDiagnostics = diag;

        string resolvedExe = ResolveClipFlowExecutable(clipFlowPath);
        if (!File.Exists(resolvedExe))
        {
            Console.Error.WriteLine($"Error: ClipFlow executable not found at '{resolvedExe}'");
            Console.Error.WriteLine("Please build ClipFlow first or provide path via --clipflow <path>");
            return 1;
        }

        var env = SmokeEnvironment.Detect();

        Console.WriteLine("ClipFlow executable smoke");
        Console.WriteLine("=========================");
        Console.WriteLine();
        Console.WriteLine("Executable:");
        Console.WriteLine($"  {resolvedExe}");
        Console.WriteLine();
        Console.WriteLine("Environment:");
        Console.WriteLine($"  OS: {env.OS}");
        Console.WriteLine($"  DISPLAY: {env.Display ?? "(none)"}");
        Console.WriteLine($"  WAYLAND_DISPLAY: {env.WaylandDisplay ?? "(none)"}");
        Console.WriteLine($"  wl-copy: {(env.WlCopyAvailable ? "available" : "not found")}");
        Console.WriteLine($"  wl-paste: {(env.WlPasteAvailable ? "available" : "not found")}");
        Console.WriteLine($"  Persistence route: {env.PersistenceRoute}");
        Console.WriteLine($"  Diagnostics: {(diag ? "enabled" : "disabled")}");
        Console.WriteLine();

        using var fixture = new SmokeFixture(keepTemp);
        if (keepTemp)
        {
            Console.WriteLine($"Workspace preserved at: {fixture.TempDirectory}");
            Console.WriteLine();
        }

        var allCases = SmokeCases.All;
        var selectedCases = new List<TestCase>();

        foreach (var c in allCases)
        {
            if (string.IsNullOrEmpty(caseFilter) || c.Name.Contains(caseFilter, StringComparison.OrdinalIgnoreCase))
            {
                selectedCases.Add(c);
            }
        }

        if (selectedCases.Count == 0)
        {
            Console.WriteLine($"No test cases matched filter: '{caseFilter}'");
            return 1;
        }

        int passed = 0;
        int failed = 0;

        foreach (var testCase in selectedCases)
        {
            var result = testCase.Runner(resolvedExe, fixture, timeout);
            if (result.Passed)
            {
                passed++;
                Console.WriteLine($"[PASS] {result.Name} ({result.Duration.TotalMilliseconds:F0}ms)");
                Console.WriteLine($"       {result.Details}");
                if (verbose)
                {
                    foreach (var run in result.Runs)
                    {
                        Console.WriteLine($"       > {run.Description} (exit: {run.ExitCode})");
                        if (!string.IsNullOrEmpty(run.StdOut)) Console.WriteLine($"         stdout: {run.StdOut.Trim()}");
                        if (!string.IsNullOrEmpty(run.StdErr)) Console.WriteLine($"         stderr: {run.StdErr.Trim()}");
                    }
                }
            }
            else
            {
                failed++;
                Console.WriteLine($"[FAIL] {result.Name} ({result.Duration.TotalMilliseconds:F0}ms)");
                Console.WriteLine($"       {result.Details}");
                foreach (var run in result.Runs)
                {
                    Console.WriteLine($"       > {run.Description} (exit: {run.ExitCode}, timedOut: {run.TimedOut})");
                    if (!string.IsNullOrEmpty(run.StdOut))
                    {
                        Console.WriteLine("         stdout:");
                        foreach (var line in run.StdOut.Trim().Split('\n'))
                        {
                            Console.WriteLine($"           {line}");
                        }
                    }
                    if (!string.IsNullOrEmpty(run.StdErr))
                    {
                        Console.WriteLine("         stderr:");
                        foreach (var line in run.StdErr.Trim().Split('\n'))
                        {
                            Console.WriteLine($"           {line}");
                        }
                    }
                }
            }
            Console.WriteLine();
        }

        Console.WriteLine($"Result: {passed} passed, {failed} failed, {selectedCases.Count} total");
        return failed == 0 ? 0 : 1;
    }

    private static string ResolveClipFlowExecutable(string? explicitPath)
    {
        if (!string.IsNullOrEmpty(explicitPath))
        {
            return Path.GetFullPath(explicitPath);
        }

        string exeName = OperatingSystem.IsWindows() ? "ClipFlow.exe" : "ClipFlow";
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;

        string[] candidates = new[]
        {
            Path.Combine(baseDir, exeName),
            Path.Combine(baseDir, "../../../../ClipFlow/bin/Debug/net10.0", exeName),
            Path.Combine(baseDir, "../../../../ClipFlow/bin/Release/net10.0", exeName),
            Path.Combine(baseDir, "../../../ClipFlow/bin/Debug/net10.0", exeName),
            Path.Combine(baseDir, "../../../ClipFlow/bin/Release/net10.0", exeName),
            Path.Combine(Directory.GetCurrentDirectory(), "Project/ClipFlowPack/ClipFlow/bin/Debug/net10.0", exeName),
            Path.Combine(Directory.GetCurrentDirectory(), "Project/ClipFlowPack/ClipFlow/bin/Release/net10.0", exeName),
            Path.Combine(Directory.GetCurrentDirectory(), "ClipFlow/bin/Debug/net10.0", exeName),
            Path.Combine(Directory.GetCurrentDirectory(), "bin", exeName)
        };

        foreach (var c in candidates)
        {
            string full = Path.GetFullPath(c);
            if (File.Exists(full))
            {
                return full;
            }
        }

        // Fallback default
        return Path.GetFullPath(candidates[1]);
    }

    private static void PrintHelp()
    {
        Console.WriteLine("ClipFlow.Smoke - Black-box process-level test runner for ClipFlow");
        Console.WriteLine();
        Console.WriteLine("Usage: ClipFlow.Smoke [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --clipflow <path>   Explicit path to the ClipFlow executable");
        Console.WriteLine("  --case <filter>     Filter test cases by name substring (e.g. image, text, files)");
        Console.WriteLine("  --keep-temp         Preserve temporary test fixtures directory");
        Console.WriteLine("  --verbose, -v       Show stdout/stderr for passed tests as well");
        Console.WriteLine("  --diag              Pass -diag to every ClipFlow process");
        Console.WriteLine("  --timeout <sec>     Execution timeout per process (default: 10s)");
        Console.WriteLine("  --help, -h          Show this help message");
    }
}
