using System.Diagnostics;

namespace ClipFlow.Smoke;

public record RunResult(
    string Description,
    int ExitCode,
    string StdOut,
    string StdErr,
    TimeSpan Duration,
    bool TimedOut);

public static class ProcessRunner
{
    public static RunResult Run(
        string executablePath,
        IEnumerable<string> arguments,
        string? stdin = null,
        TimeSpan? timeout = null,
        string? workingDir = null)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(10);
        var stopwatch = Stopwatch.StartNew();

        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = stdin != null,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8,
            CreateNoWindow = true,
            WorkingDirectory = workingDir ?? Directory.GetCurrentDirectory()
        };

        if (stdin != null)
        {
            startInfo.StandardInputEncoding = new System.Text.UTF8Encoding(false);
        }

        foreach (var arg in arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }

        string cmdDesc = $"{Path.GetFileName(executablePath)} {string.Join(" ", arguments)}";

        using var process = new Process { StartInfo = startInfo };

        var stdoutTask = new TaskCompletionSource<string>();
        var stderrTask = new TaskCompletionSource<string>();

        try
        {
            process.Start();

            var stdoutReadTask = process.StandardOutput.ReadToEndAsync();
            var stderrReadTask = process.StandardError.ReadToEndAsync();

            if (stdin != null)
            {
                using var writer = process.StandardInput;
                writer.Write(stdin);
                writer.Flush();
            }

            bool exited = process.WaitForExit((int)effectiveTimeout.TotalMilliseconds);
            stopwatch.Stop();

            if (!exited)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Ignore error killing dead process
                }

                return new RunResult(
                    cmdDesc,
                    ExitCode: -1,
                    StdOut: stdoutReadTask.IsCompleted ? stdoutReadTask.Result : "",
                    StdErr: stderrReadTask.IsCompleted ? stderrReadTask.Result : "Timed out after " + effectiveTimeout.TotalSeconds + "s",
                    Duration: stopwatch.Elapsed,
                    TimedOut: true);
            }

            Task.WaitAll(new Task[] { stdoutReadTask, stderrReadTask }, TimeSpan.FromSeconds(2));

            string stdout = stdoutReadTask.IsCompleted ? stdoutReadTask.Result : "";
            string stderr = stderrReadTask.IsCompleted ? stderrReadTask.Result : "";

            return new RunResult(
                cmdDesc,
                ExitCode: process.ExitCode,
                StdOut: stdout,
                StdErr: stderr,
                Duration: stopwatch.Elapsed,
                TimedOut: false);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new RunResult(
                cmdDesc,
                ExitCode: -1,
                StdOut: "",
                StdErr: ex.ToString(),
                Duration: stopwatch.Elapsed,
                TimedOut: false);
        }
    }
}
