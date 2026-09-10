using System.Diagnostics;
using SkiaSharp;

namespace ClipFlow.Smoke;

public record CaseResult(
    string Name,
    bool Passed,
    string Details,
    List<RunResult> Runs,
    TimeSpan Duration);

public record TestCase(
    string Name,
    Func<string, SmokeFixture, TimeSpan, CaseResult> Runner);

public static class SmokeCases
{
    public static List<TestCase> All = new()
    {
        new("text/string -> file", Case1_TextStringToFile),
        new("text/file -> console", Case2_TextFileToConsole),
        new("console/stdin -> text clipboard", Case3_ConsoleStdinToTextClipboard),
        new("html/string -> file", Case4_HtmlStringToFile),
        new("files/directory -> file list", Case5_FilesDirectoryToFileList),
        new("files/file-list -> console", Case6_FilesFileListToConsole),
        new("directory wildcard", Case7_DirectoryWildcard),
        new("image advertisement", Case8_ImageAdvertisement),
        new("image full persistence round trip", Case9_ImageFullPersistenceRoundTrip),
        new("files/console relative stdin -> file", Case10_FilesConsoleRelativeStdinToFile),
        new("files/file-list relative entries -> file", Case11_FilesFileListRelativeEntriesToFile),
        new("files/console invalid path error", Case12_FilesConsoleInvalidPathError),
        new("files/directory expansion -> directory", Case13_FilesDirectoryExpansionToDirectory),
        new("files/path exact-file -> console", Case14_FilesPathExactFileToConsole),
        new("files advertisement and special chars roundtrip", Case15_FilesAdvertisementAndSpecialCharsRoundTrip)
    };

    public static CaseResult Case1_TextStringToFile(string exe, SmokeFixture fixture, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        var runs = new List<RunResult>();
        string expectedText = "Hello 🌍 — ClipFlow persistence test!\nLine 2 with spaces   \nLine 3: 漢字 & éàü";
        string resultFile = fixture.GetPath("case1_result.txt");

        var copyRun = ProcessRunner.Run(exe, new[] { "copy", "text", "string", expectedText }, timeout: timeout);
        runs.Add(copyRun);

        if (copyRun.ExitCode != 0 || copyRun.TimedOut)
        {
            sw.Stop();
            return new CaseResult("text/string -> file", false, $"Copy failed (exit {copyRun.ExitCode}, timedOut={copyRun.TimedOut})", runs, sw.Elapsed);
        }

        var pasteRun = ProcessRunner.Run(exe, new[] { "paste", "text", "file", resultFile }, timeout: timeout);
        runs.Add(pasteRun);

        sw.Stop();
        if (pasteRun.ExitCode != 0 || pasteRun.TimedOut)
        {
            return new CaseResult("text/string -> file", false, $"Paste failed (exit {pasteRun.ExitCode}, timedOut={pasteRun.TimedOut})", runs, sw.Elapsed);
        }

        if (!File.Exists(resultFile))
        {
            return new CaseResult("text/string -> file", false, "Result file was not created", runs, sw.Elapsed);
        }

        string actualText = File.ReadAllText(resultFile);
        if (actualText != expectedText)
        {
            return new CaseResult("text/string -> file", false, $"Content mismatch. Expected length {expectedText.Length}, got {actualText.Length}", runs, sw.Elapsed);
        }

        return new CaseResult("text/string -> file", true, "Exact payload match across independent processes", runs, sw.Elapsed);
    }

    public static CaseResult Case2_TextFileToConsole(string exe, SmokeFixture fixture, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        var runs = new List<RunResult>();
        string expectedText = "Deterministic text file\nLine 2: 🚀 Fast clipboard\nLine 3: End of file.";
        string srcFile = fixture.CreateTextFile("case2_source.txt", expectedText);

        var copyRun = ProcessRunner.Run(exe, new[] { "copy", "text", "file", srcFile }, timeout: timeout);
        runs.Add(copyRun);

        if (copyRun.ExitCode != 0 || copyRun.TimedOut)
        {
            sw.Stop();
            return new CaseResult("text/file -> console", false, $"Copy failed (exit {copyRun.ExitCode}, timedOut={copyRun.TimedOut})", runs, sw.Elapsed);
        }

        var pasteRun = ProcessRunner.Run(exe, new[] { "paste", "text", "console" }, timeout: timeout);
        runs.Add(pasteRun);

        sw.Stop();
        if (pasteRun.ExitCode != 0 || pasteRun.TimedOut)
        {
            return new CaseResult("text/file -> console", false, $"Paste failed (exit {pasteRun.ExitCode}, timedOut={pasteRun.TimedOut})", runs, sw.Elapsed);
        }

        string stdout = pasteRun.StdOut.Replace("\r\n", "\n");
        string normalizedExpected = expectedText.Replace("\r\n", "\n");

        if (!stdout.Contains(normalizedExpected))
        {
            return new CaseResult("text/file -> console", false, $"Console output did not contain expected text. Got: {stdout}", runs, sw.Elapsed);
        }

        return new CaseResult("text/file -> console", true, "Console output contains exact file text", runs, sw.Elapsed);
    }

    public static CaseResult Case3_ConsoleStdinToTextClipboard(string exe, SmokeFixture fixture, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        var runs = new List<RunResult>();
        string stdinText = "Stdin piped data\nMulti-line stream content\nSpecial: àéïôù";
        string resultFile = fixture.GetPath("case3_result.txt");

        var copyRun = ProcessRunner.Run(exe, new[] { "copy", "text", "console" }, stdin: stdinText, timeout: timeout);
        runs.Add(copyRun);

        if (copyRun.ExitCode != 0 || copyRun.TimedOut)
        {
            sw.Stop();
            return new CaseResult("console/stdin -> text clipboard", false, $"Copy from stdin failed (exit {copyRun.ExitCode}, timedOut={copyRun.TimedOut})", runs, sw.Elapsed);
        }

        var pasteRun = ProcessRunner.Run(exe, new[] { "paste", "text", "file", resultFile }, timeout: timeout);
        runs.Add(pasteRun);

        sw.Stop();
        if (pasteRun.ExitCode != 0 || pasteRun.TimedOut)
        {
            return new CaseResult("console/stdin -> text clipboard", false, $"Paste failed (exit {pasteRun.ExitCode}, timedOut={pasteRun.TimedOut})", runs, sw.Elapsed);
        }

        if (!File.Exists(resultFile))
        {
            return new CaseResult("console/stdin -> text clipboard", false, "Result file was not created", runs, sw.Elapsed);
        }

        string actualText = File.ReadAllText(resultFile);
        if (actualText != stdinText)
        {
            return new CaseResult("console/stdin -> text clipboard", false, $"Content mismatch. Expected length {stdinText.Length}, got {actualText.Length}", runs, sw.Elapsed);
        }

        return new CaseResult("console/stdin -> text clipboard", true, "Exact stdin payload persisted and retrieved", runs, sw.Elapsed);
    }

    public static CaseResult Case4_HtmlStringToFile(string exe, SmokeFixture fixture, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        var runs = new List<RunResult>();
        string htmlContent = "<!DOCTYPE html><html><body><h1>ClipFlow Title</h1><p>Smoke HTML with <b>formatting</b> &amp; entities.</p></body></html>";
        string resultFile = fixture.GetPath("case4_result.html");

        var copyRun = ProcessRunner.Run(exe, new[] { "copy", "html", "string", htmlContent }, timeout: timeout);
        runs.Add(copyRun);

        if (copyRun.ExitCode != 0 || copyRun.TimedOut)
        {
            sw.Stop();
            return new CaseResult("html/string -> file", false, $"Copy failed (exit {copyRun.ExitCode}, timedOut={copyRun.TimedOut})", runs, sw.Elapsed);
        }

        var pasteRun = ProcessRunner.Run(exe, new[] { "paste", "html", "file", resultFile }, timeout: timeout);
        runs.Add(pasteRun);

        sw.Stop();
        if (pasteRun.ExitCode != 0 || pasteRun.TimedOut)
        {
            return new CaseResult("html/string -> file", false, $"Paste failed (exit {pasteRun.ExitCode}, timedOut={pasteRun.TimedOut})", runs, sw.Elapsed);
        }

        if (!File.Exists(resultFile))
        {
            return new CaseResult("html/string -> file", false, "Result file was not created", runs, sw.Elapsed);
        }

        string actualHtml = File.ReadAllText(resultFile);
        if (actualHtml != htmlContent)
        {
            return new CaseResult("html/string -> file", false, $"HTML content mismatch. Expected: {htmlContent}, Got: {actualHtml}", runs, sw.Elapsed);
        }

        return new CaseResult("html/string -> file", true, "Exact HTML payload preserved", runs, sw.Elapsed);
    }

    public static CaseResult Case5_FilesDirectoryToFileList(string exe, SmokeFixture fixture, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        var runs = new List<RunResult>();
        string srcDir = fixture.GetPath("case5_src");
        Directory.CreateDirectory(srcDir);

        string f1 = Path.Combine(srcDir, "file_a.txt");
        string f2 = Path.Combine(srcDir, "file_b.json");
        string f3 = Path.Combine(srcDir, "file_c.csv");
        File.WriteAllText(f1, "a");
        File.WriteAllText(f2, "{}");
        File.WriteAllText(f3, "c,1");

        string resultFile = fixture.GetPath("case5_list.txt");

        var copyRun = ProcessRunner.Run(exe, new[] { "copy", "files", "directory", srcDir }, timeout: timeout);
        runs.Add(copyRun);

        if (copyRun.ExitCode != 0 || copyRun.TimedOut)
        {
            sw.Stop();
            return new CaseResult("files/directory -> file list", false, $"Copy directory failed (exit {copyRun.ExitCode}, timedOut={copyRun.TimedOut})", runs, sw.Elapsed);
        }

        var pasteRun = ProcessRunner.Run(exe, new[] { "paste", "files", "file", resultFile }, timeout: timeout);
        runs.Add(pasteRun);

        sw.Stop();
        if (pasteRun.ExitCode != 0 || pasteRun.TimedOut)
        {
            return new CaseResult("files/directory -> file list", false, $"Paste file list failed (exit {pasteRun.ExitCode}, timedOut={pasteRun.TimedOut})", runs, sw.Elapsed);
        }

        if (!File.Exists(resultFile))
        {
            return new CaseResult("files/directory -> file list", false, "Result file was not created", runs, sw.Elapsed);
        }

        var lines = File.ReadAllLines(resultFile)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l))
            .Select(l => Path.GetFileName(l.TrimEnd('/', '\\')))
            .ToHashSet();

        var expectedNames = new HashSet<string> { "file_a.txt", "file_b.json", "file_c.csv" };
        if (!expectedNames.IsSubsetOf(lines))
        {
            return new CaseResult("files/directory -> file list", false, $"File list mismatch. Expected {string.Join(", ", expectedNames)}, got {string.Join(", ", lines)}", runs, sw.Elapsed);
        }

        return new CaseResult("files/directory -> file list", true, "Normalized file list contains all directory files", runs, sw.Elapsed);
    }

    public static CaseResult Case6_FilesFileListToConsole(string exe, SmokeFixture fixture, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        var runs = new List<RunResult>();
        string f1 = fixture.CreateTextFile("case6_alpha.txt", "alpha");
        string f2 = fixture.CreateTextFile("case6_beta.txt", "beta");

        string listInput = fixture.CreateTextFile("case6_input_list.txt", $"{f1}\n{f2}\n");

        var copyRun = ProcessRunner.Run(exe, new[] { "copy", "files", "file", listInput }, timeout: timeout);
        runs.Add(copyRun);

        if (copyRun.ExitCode != 0 || copyRun.TimedOut)
        {
            sw.Stop();
            return new CaseResult("files/file-list -> console", false, $"Copy file list failed (exit {copyRun.ExitCode}, timedOut={copyRun.TimedOut})", runs, sw.Elapsed);
        }

        var pasteRun = ProcessRunner.Run(exe, new[] { "paste", "files", "console" }, timeout: timeout);
        runs.Add(pasteRun);

        sw.Stop();
        if (pasteRun.ExitCode != 0 || pasteRun.TimedOut)
        {
            return new CaseResult("files/file-list -> console", false, $"Paste files console failed (exit {pasteRun.ExitCode}, timedOut={pasteRun.TimedOut})", runs, sw.Elapsed);
        }

        string stdout = pasteRun.StdOut;
        if (!stdout.Contains("case6_alpha.txt") || !stdout.Contains("case6_beta.txt"))
        {
            return new CaseResult("files/file-list -> console", false, $"Console output did not contain expected files. Got: {stdout}", runs, sw.Elapsed);
        }

        return new CaseResult("files/file-list -> console", true, "Console output contains expected file entries", runs, sw.Elapsed);
    }

    public static CaseResult Case7_DirectoryWildcard(string exe, SmokeFixture fixture, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        var runs = new List<RunResult>();
        string wildcardDir = fixture.GetPath("case7_wildcard");
        Directory.CreateDirectory(wildcardDir);

        File.WriteAllText(Path.Combine(wildcardDir, "one.txt"), "1");
        File.WriteAllText(Path.Combine(wildcardDir, "two.txt"), "2");
        File.WriteAllText(Path.Combine(wildcardDir, "image.png"), "png_bytes");

        string wildcardPattern = Path.Combine(wildcardDir, "*.txt");
        string resultFile = fixture.GetPath("case7_result.txt");

        var copyRun = ProcessRunner.Run(exe, new[] { "copy", "files", "directory", wildcardPattern }, timeout: timeout);
        runs.Add(copyRun);

        if (copyRun.ExitCode != 0 || copyRun.TimedOut)
        {
            sw.Stop();
            return new CaseResult("directory wildcard", false, $"Copy wildcard failed (exit {copyRun.ExitCode}, timedOut={copyRun.TimedOut})", runs, sw.Elapsed);
        }

        var pasteRun = ProcessRunner.Run(exe, new[] { "paste", "files", "file", resultFile }, timeout: timeout);
        runs.Add(pasteRun);

        sw.Stop();
        if (pasteRun.ExitCode != 0 || pasteRun.TimedOut)
        {
            return new CaseResult("directory wildcard", false, $"Paste wildcard failed (exit {pasteRun.ExitCode}, timedOut={pasteRun.TimedOut})", runs, sw.Elapsed);
        }

        if (!File.Exists(resultFile))
        {
            return new CaseResult("directory wildcard", false, "Result file was not created", runs, sw.Elapsed);
        }

        string content = File.ReadAllText(resultFile);
        if (!content.Contains("one.txt") || !content.Contains("two.txt"))
        {
            return new CaseResult("directory wildcard", false, $"Result missing matching text files. Got: {content}", runs, sw.Elapsed);
        }

        if (content.Contains("image.png"))
        {
            return new CaseResult("directory wildcard", false, $"Result incorrectly included non-matching file image.png. Got: {content}", runs, sw.Elapsed);
        }

        return new CaseResult("directory wildcard", true, "Only matching files included by wildcard pattern", runs, sw.Elapsed);
    }

    public static CaseResult Case8_ImageAdvertisement(string exe, SmokeFixture fixture, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        var runs = new List<RunResult>();
        string imgPath = fixture.GetPath("case8_img.png");

        using (var bmp = CreateDeterministicBitmap(8, 8))
        using (var image = SKImage.FromBitmap(bmp))
        using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
        using (var stream = File.OpenWrite(imgPath))
        {
            data.SaveTo(stream);
        }

        var copyRun = ProcessRunner.Run(exe, new[] { "copy", "image", "file", imgPath }, timeout: timeout);
        runs.Add(copyRun);

        if (copyRun.ExitCode != 0 || copyRun.TimedOut)
        {
            sw.Stop();
            return new CaseResult("image advertisement", false, $"Copy image failed (exit {copyRun.ExitCode}, timedOut={copyRun.TimedOut})", runs, sw.Elapsed);
        }

        var availRun = ProcessRunner.Run(exe, new[] { "show", "avail" }, timeout: timeout);
        runs.Add(availRun);

        sw.Stop();
        if (availRun.ExitCode != 0 || availRun.TimedOut)
        {
            return new CaseResult("image advertisement", false, $"Show avail failed (exit {availRun.ExitCode}, timedOut={availRun.TimedOut})", runs, sw.Elapsed);
        }

        if (!availRun.StdOut.Contains("image"))
        {
            return new CaseResult("image advertisement", false, $"Show avail did not report image format. StdOut: {availRun.StdOut}, StdErr: {availRun.StdErr}", runs, sw.Elapsed);
        }

        return new CaseResult("image advertisement", true, "show avail correctly reports image format", runs, sw.Elapsed);
    }

    public static CaseResult Case9_ImageFullPersistenceRoundTrip(string exe, SmokeFixture fixture, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        var runs = new List<RunResult>();
        string srcImgPath = fixture.GetPath("case9_src.png");
        string dstImgPath = fixture.GetPath("case9_dst.png");

        using var srcBmp = CreateDeterministicBitmap(8, 8);
        using (var image = SKImage.FromBitmap(srcBmp))
        using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
        using (var stream = File.OpenWrite(srcImgPath))
        {
            data.SaveTo(stream);
        }

        var copyRun = ProcessRunner.Run(exe, new[] { "copy", "image", "file", srcImgPath }, timeout: timeout);
        runs.Add(copyRun);

        if (copyRun.ExitCode != 0 || copyRun.TimedOut)
        {
            sw.Stop();
            return new CaseResult("image full persistence round trip", false, $"Copy image failed (exit {copyRun.ExitCode}, timedOut={copyRun.TimedOut})", runs, sw.Elapsed);
        }

        var pasteRun = ProcessRunner.Run(exe, new[] { "paste", "image", "file", dstImgPath }, timeout: timeout);
        runs.Add(pasteRun);

        sw.Stop();
        if (pasteRun.ExitCode != 0 || pasteRun.TimedOut)
        {
            return new CaseResult("image full persistence round trip", false, $"Paste image failed (exit {pasteRun.ExitCode}, timedOut={pasteRun.TimedOut}, stderr={pasteRun.StdErr})", runs, sw.Elapsed);
        }

        if (!File.Exists(dstImgPath))
        {
            return new CaseResult("image full persistence round trip", false, $"Destination image file was not created ({dstImgPath}). StdErr: {pasteRun.StdErr}", runs, sw.Elapsed);
        }

        var fileInfo = new FileInfo(dstImgPath);
        if (fileInfo.Length == 0)
        {
            return new CaseResult("image full persistence round trip", false, "Destination image file is empty", runs, sw.Elapsed);
        }

        using var dstBmp = SKBitmap.Decode(dstImgPath);
        if (dstBmp == null)
        {
            return new CaseResult("image full persistence round trip", false, "SkiaSharp failed to decode destination image file", runs, sw.Elapsed);
        }

        if (dstBmp.Width != srcBmp.Width || dstBmp.Height != srcBmp.Height)
        {
            return new CaseResult("image full persistence round trip", false, $"Image dimensions mismatch: expected {srcBmp.Width}x{srcBmp.Height}, got {dstBmp.Width}x{dstBmp.Height}", runs, sw.Elapsed);
        }

        for (int y = 0; y < srcBmp.Height; y++)
        {
            for (int x = 0; x < srcBmp.Width; x++)
            {
                var pSrc = srcBmp.GetPixel(x, y);
                var pDst = dstBmp.GetPixel(x, y);
                if (pSrc != pDst)
                {
                    return new CaseResult("image full persistence round trip", false, $"Pixel mismatch at ({x},{y}): expected {pSrc}, got {pDst}", runs, sw.Elapsed);
                }
            }
        }

        return new CaseResult("image full persistence round trip", true, "Exact decoded pixel match across independent ClipFlow processes", runs, sw.Elapsed);
    }

    private static SKBitmap CreateDeterministicBitmap(int width, int height)
    {
        var bmp = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                byte r = (byte)((x * 35) % 256);
                byte g = (byte)((y * 45) % 256);
                byte b = (byte)(((x + y) * 55) % 256);
                bmp.SetPixel(x, y, new SKColor(r, g, b, 255));
            }
        }
        return bmp;
    }

    public static CaseResult Case10_FilesConsoleRelativeStdinToFile(string exe, SmokeFixture fixture, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        var runs = new List<RunResult>();
        string workDir = fixture.GetPath("case10_workdir");
        Directory.CreateDirectory(workDir);

        string subDir = Path.Combine(workDir, "sub");
        Directory.CreateDirectory(subDir);

        string fileA = Path.Combine(workDir, "file_a.txt");
        string fileB = Path.Combine(subDir, "file_b.txt");
        File.WriteAllText(fileA, "content A");
        File.WriteAllText(fileB, "content B");

        string stdinInput = $"file_a.txt\nsub/file_b.txt\n";
        string resultFile = fixture.GetPath("case10_result.txt");

        var copyRun = ProcessRunner.Run(
            exe,
            new[] { "copy", "files", "console" },
            stdin: stdinInput,
            timeout: timeout,
            workingDir: workDir);
        runs.Add(copyRun);

        if (copyRun.ExitCode != 0 || copyRun.TimedOut)
        {
            sw.Stop();
            return new CaseResult("files/console relative stdin -> file", false, $"Copy from relative stdin failed (exit {copyRun.ExitCode}, timedOut={copyRun.TimedOut}, stderr={copyRun.StdErr})", runs, sw.Elapsed);
        }

        var pasteRun = ProcessRunner.Run(
            exe,
            new[] { "paste", "files", "file", resultFile },
            timeout: timeout);
        runs.Add(pasteRun);

        sw.Stop();
        if (pasteRun.ExitCode != 0 || pasteRun.TimedOut)
        {
            return new CaseResult("files/console relative stdin -> file", false, $"Paste files to file failed (exit {pasteRun.ExitCode}, timedOut={pasteRun.TimedOut}, stderr={pasteRun.StdErr})", runs, sw.Elapsed);
        }

        if (!File.Exists(resultFile))
        {
            return new CaseResult("files/console relative stdin -> file", false, "Result file was not created", runs, sw.Elapsed);
        }

        var lines = File.ReadAllLines(resultFile)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l))
            .ToList();

        if (lines.Count != 2)
        {
            return new CaseResult("files/console relative stdin -> file", false, $"Expected 2 lines, got {lines.Count}", runs, sw.Elapsed);
        }

        string expectedA = Path.GetFullPath(fileA);
        string expectedB = Path.GetFullPath(fileB);

        if (lines[0] != expectedA || lines[1] != expectedB)
        {
            return new CaseResult("files/console relative stdin -> file", false, $"Path mismatch. Expected [{expectedA}, {expectedB}], got [{lines[0]}, {lines[1]}]", runs, sw.Elapsed);
        }

        if (!File.Exists(lines[0]) || !File.Exists(lines[1]))
        {
            return new CaseResult("files/console relative stdin -> file", false, "Result file paths do not exist on disk", runs, sw.Elapsed);
        }

        return new CaseResult("files/console relative stdin -> file", true, "Relative stdin paths resolved to full existing paths against CWD", runs, sw.Elapsed);
    }

    public static CaseResult Case11_FilesFileListRelativeEntriesToFile(string exe, SmokeFixture fixture, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        var runs = new List<RunResult>();
        string workDir = fixture.GetPath("case11_workdir");
        Directory.CreateDirectory(workDir);

        string nestedDir = Path.Combine(workDir, "nested");
        Directory.CreateDirectory(nestedDir);

        string file1 = Path.Combine(workDir, "doc1.txt");
        string file2 = Path.Combine(nestedDir, "doc2.txt");
        File.WriteAllText(file1, "doc 1");
        File.WriteAllText(file2, "doc 2");

        string listFile = Path.Combine(workDir, "list.txt");
        File.WriteAllText(listFile, $"doc1.txt\n\nnested/doc2.txt\n   \n");

        string resultFile = fixture.GetPath("case11_result.txt");

        var copyRun = ProcessRunner.Run(
            exe,
            new[] { "copy", "files", "file", listFile },
            timeout: timeout,
            workingDir: workDir);
        runs.Add(copyRun);

        if (copyRun.ExitCode != 0 || copyRun.TimedOut)
        {
            sw.Stop();
            return new CaseResult("files/file-list relative entries -> file", false, $"Copy from list file failed (exit {copyRun.ExitCode}, timedOut={copyRun.TimedOut}, stderr={copyRun.StdErr})", runs, sw.Elapsed);
        }

        var pasteRun = ProcessRunner.Run(
            exe,
            new[] { "paste", "files", "file", resultFile },
            timeout: timeout);
        runs.Add(pasteRun);

        sw.Stop();
        if (pasteRun.ExitCode != 0 || pasteRun.TimedOut)
        {
            return new CaseResult("files/file-list relative entries -> file", false, $"Paste to file failed (exit {pasteRun.ExitCode}, timedOut={pasteRun.TimedOut}, stderr={pasteRun.StdErr})", runs, sw.Elapsed);
        }

        if (!File.Exists(resultFile))
        {
            return new CaseResult("files/file-list relative entries -> file", false, "Result file was not created", runs, sw.Elapsed);
        }

        var lines = File.ReadAllLines(resultFile)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l))
            .ToList();

        string expected1 = Path.GetFullPath(file1);
        string expected2 = Path.GetFullPath(file2);

        if (lines.Count != 2 || lines[0] != expected1 || lines[1] != expected2)
        {
            return new CaseResult("files/file-list relative entries -> file", false, $"Pasted paths mismatch. Got: {string.Join(", ", lines)}", runs, sw.Elapsed);
        }

        return new CaseResult("files/file-list relative entries -> file", true, "Relative list-file entries resolved against CWD and pasted cleanly", runs, sw.Elapsed);
    }

    public static CaseResult Case12_FilesConsoleInvalidPathError(string exe, SmokeFixture fixture, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        var runs = new List<RunResult>();
        string workDir = fixture.GetPath("case12_workdir");
        Directory.CreateDirectory(workDir);

        string validFile = Path.Combine(workDir, "real.txt");
        File.WriteAllText(validFile, "real");

        string stdinInput = $"real.txt\nmissing_file_xyz.txt\n";

        var copyRun = ProcessRunner.Run(
            exe,
            new[] { "copy", "files", "console" },
            stdin: stdinInput,
            timeout: timeout,
            workingDir: workDir);
        runs.Add(copyRun);
        sw.Stop();

        if (copyRun.ExitCode == 0)
        {
            return new CaseResult("files/console invalid path error", false, $"Expected non-zero exit code for invalid path, got exit 0. StdOut: {copyRun.StdOut}, StdErr: {copyRun.StdErr}", runs, sw.Elapsed);
        }

        if (!copyRun.StdErr.Contains("Path not found", StringComparison.OrdinalIgnoreCase))
        {
            return new CaseResult("files/console invalid path error", false, $"Expected 'Path not found' diagnostic in stderr, got: {copyRun.StdErr}", runs, sw.Elapsed);
        }

        return new CaseResult("files/console invalid path error", true, "Non-existent path produced non-zero exit code and diagnostic in stderr", runs, sw.Elapsed);
    }

    public static CaseResult Case13_FilesDirectoryExpansionToDirectory(string exe, SmokeFixture fixture, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        var runs = new List<RunResult>();
        string workDir = fixture.GetPath("case13_workdir");
        Directory.CreateDirectory(workDir);

        string srcTree = Path.Combine(workDir, "source_tree");
        string srcSub = Path.Combine(srcTree, "nested_sub");
        Directory.CreateDirectory(srcSub);

        string rootFile = Path.Combine(srcTree, "root.txt");
        string nestedFile = Path.Combine(srcSub, "nested.txt");
        string standaloneFile = Path.Combine(workDir, "standalone.txt");

        const string rootContent = "Root file content in source tree";
        const string nestedContent = "Nested file content in deep subfolder";
        const string standaloneContent = "Standalone extra file content";

        File.WriteAllText(rootFile, rootContent);
        File.WriteAllText(nestedFile, nestedContent);
        File.WriteAllText(standaloneFile, standaloneContent);

        string targetDir = fixture.GetPath("case13_target");

        string stdinInput = $"source_tree\nstandalone.txt\n";

        var copyRun = ProcessRunner.Run(
            exe,
            new[] { "copy", "files", "console" },
            stdin: stdinInput,
            timeout: timeout,
            workingDir: workDir);
        runs.Add(copyRun);

        if (copyRun.ExitCode != 0 || copyRun.TimedOut)
        {
            sw.Stop();
            return new CaseResult("files/directory expansion -> directory", false, $"Copy failed (exit {copyRun.ExitCode}, timedOut={copyRun.TimedOut}, stderr={copyRun.StdErr})", runs, sw.Elapsed);
        }

        var pasteRun = ProcessRunner.Run(
            exe,
            new[] { "paste", "files", "directory", targetDir },
            timeout: timeout);
        runs.Add(pasteRun);

        sw.Stop();
        if (pasteRun.ExitCode != 0 || pasteRun.TimedOut)
        {
            return new CaseResult("files/directory expansion -> directory", false, $"Paste to directory failed (exit {pasteRun.ExitCode}, timedOut={pasteRun.TimedOut}, stderr={pasteRun.StdErr})", runs, sw.Elapsed);
        }

        if (!Directory.Exists(targetDir))
        {
            return new CaseResult("files/directory expansion -> directory", false, "Target directory was not created", runs, sw.Elapsed);
        }

        string destRootFile = Path.Combine(targetDir, "source_tree", "root.txt");
        string destNestedFile = Path.Combine(targetDir, "source_tree", "nested_sub", "nested.txt");
        string destStandaloneFile = Path.Combine(targetDir, "standalone.txt");

        if (!File.Exists(destRootFile))
        {
            return new CaseResult("files/directory expansion -> directory", false, $"Expected expanded file at {destRootFile}", runs, sw.Elapsed);
        }
        if (!File.Exists(destNestedFile))
        {
            return new CaseResult("files/directory expansion -> directory", false, $"Expected expanded nested file at {destNestedFile}", runs, sw.Elapsed);
        }
        if (!File.Exists(destStandaloneFile))
        {
            return new CaseResult("files/directory expansion -> directory", false, $"Expected standalone file at {destStandaloneFile}", runs, sw.Elapsed);
        }

        if (File.ReadAllText(destRootFile) != rootContent ||
            File.ReadAllText(destNestedFile) != nestedContent ||
            File.ReadAllText(destStandaloneFile) != standaloneContent)
        {
            return new CaseResult("files/directory expansion -> directory", false, "Content mismatch in expanded files", runs, sw.Elapsed);
        }

        return new CaseResult("files/directory expansion -> directory", true, "Directory tree and standalone files expanded and copied recursively with contents preserved", runs, sw.Elapsed);
    }

    public static CaseResult Case14_FilesPathExactFileToConsole(string exe, SmokeFixture fixture, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        var runs = new List<RunResult>();
        string targetFile = fixture.CreateTextFile("case14_exact.txt", "exact file content for path test");
        string normalizedExpected = Path.GetFullPath(targetFile);

        var copyRun = ProcessRunner.Run(exe, new[] { "copy", "files", "path", targetFile }, timeout: timeout);
        runs.Add(copyRun);

        if (copyRun.ExitCode != 0 || copyRun.TimedOut)
        {
            sw.Stop();
            return new CaseResult("files/path exact-file -> console", false, $"Copy exact file via path failed (exit {copyRun.ExitCode}, timedOut={copyRun.TimedOut})", runs, sw.Elapsed);
        }

        var pasteRun = ProcessRunner.Run(exe, new[] { "paste", "files", "console" }, timeout: timeout);
        runs.Add(pasteRun);

        sw.Stop();
        if (pasteRun.ExitCode != 0 || pasteRun.TimedOut)
        {
            return new CaseResult("files/path exact-file -> console", false, $"Paste files console failed (exit {pasteRun.ExitCode}, timedOut={pasteRun.TimedOut})", runs, sw.Elapsed);
        }

        string stdout = pasteRun.StdOut.Trim();
        var lines = stdout.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToList();

        if (lines.Count != 1 || lines[0] != normalizedExpected)
        {
            return new CaseResult("files/path exact-file -> console", false, $"Expected exactly 1 line matching '{normalizedExpected}', got {lines.Count} lines: {stdout}", runs, sw.Elapsed);
        }

        return new CaseResult("files/path exact-file -> console", true, "Exact file copied via path endpoint and pasted to console as normalized absolute path", runs, sw.Elapsed);
    }

    public static CaseResult Case15_FilesAdvertisementAndSpecialCharsRoundTrip(string exe, SmokeFixture fixture, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        var runs = new List<RunResult>();

        string fileName = "clip flow # ünicode %.txt";
        string targetFile = fixture.CreateTextFile(fileName, "strong fixture content for special char uri encoding test");
        string normalizedExpected = Path.GetFullPath(targetFile);

        // 1. Copy files path <fixture-file>
        var copyRun = ProcessRunner.Run(exe, new[] { "copy", "files", "path", targetFile }, timeout: timeout);
        runs.Add(copyRun);

        if (copyRun.ExitCode != 0 || copyRun.TimedOut)
        {
            sw.Stop();
            return new CaseResult("files advertisement and special chars roundtrip", false, $"Copy exact file via path failed (exit {copyRun.ExitCode}, timedOut={copyRun.TimedOut}, stderr={copyRun.StdErr})", runs, sw.Elapsed);
        }

        // 2. Show avail: must contain "files" and must NOT contain "text"
        var availRun = ProcessRunner.Run(exe, new[] { "show", "avail" }, timeout: timeout);
        runs.Add(availRun);

        if (availRun.ExitCode != 0 || availRun.TimedOut)
        {
            sw.Stop();
            return new CaseResult("files advertisement and special chars roundtrip", false, $"Show avail failed (exit {availRun.ExitCode}, timedOut={availRun.TimedOut}, stderr={availRun.StdErr})", runs, sw.Elapsed);
        }

        var availLines = availRun.StdOut.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToList();

        if (!availLines.Contains("files"))
        {
            sw.Stop();
            return new CaseResult("files advertisement and special chars roundtrip", false, $"Show avail did not report 'files'. StdOut: {availRun.StdOut}, StdErr: {availRun.StdErr}", runs, sw.Elapsed);
        }

        if (availLines.Contains("text"))
        {
            sw.Stop();
            return new CaseResult("files advertisement and special chars roundtrip", false, $"Show avail incorrectly reported 'text' alongside 'files'. StdOut: {availRun.StdOut}", runs, sw.Elapsed);
        }

        // 3. Paste files console: must return original normalized path
        var pasteRun = ProcessRunner.Run(exe, new[] { "paste", "files", "console" }, timeout: timeout);
        runs.Add(pasteRun);

        sw.Stop();
        if (pasteRun.ExitCode != 0 || pasteRun.TimedOut)
        {
            return new CaseResult("files advertisement and special chars roundtrip", false, $"Paste files console failed (exit {pasteRun.ExitCode}, timedOut={pasteRun.TimedOut}, stderr={pasteRun.StdErr})", runs, sw.Elapsed);
        }

        string stdout = pasteRun.StdOut.Trim();
        var pasteLines = stdout.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToList();

        if (pasteLines.Count != 1 || pasteLines[0] != normalizedExpected)
        {
            return new CaseResult("files advertisement and special chars roundtrip", false, $"Expected exactly 1 line matching '{normalizedExpected}', got {pasteLines.Count} lines: {stdout}", runs, sw.Elapsed);
        }

        return new CaseResult("files advertisement and special chars roundtrip", true, "Files advertised exclusively (without plain text fallback) and special character filename roundtrips perfectly", runs, sw.Elapsed);
    }
}
