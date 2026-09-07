using System;
using System.Collections.Generic;
using ClipFlow.Format;
using Xunit;

namespace ClipFlow.Tests;

public class ConsoleEndpointTests
{
    [Fact]
    public void Read_Text_ReadsCompleteInputToIdentity()
    {
        const string inputData = "First line\nSecond line\nThird line";
        using var testCtx = new TestClipContext(inputData);
        var endpoint = new ClipEndpoint.Console();
        var text = new ClipType.Text();

        endpoint.Read(testCtx.Context, text);

        Assert.Equal(inputData, text.Identity);
        Assert.Equal(0, testCtx.Status);
    }

    [Fact]
    public void Read_Html_ReadsCompleteInputToIdentity()
    {
        const string inputData = "<html>\n<body>\n<h1>Title</h1>\n</body>\n</html>";
        using var testCtx = new TestClipContext(inputData);
        var endpoint = new ClipEndpoint.Console();
        var html = new ClipType.Html();

        endpoint.Read(testCtx.Context, html);

        Assert.Equal(inputData, html.Identity);
        Assert.Equal(0, testCtx.Status);
    }

    [Fact]
    public void Read_Files_ReadsLinesToListIdentity()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "clipflow-console-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        string originalCwd = Environment.CurrentDirectory;

        try
        {
            Environment.CurrentDirectory = tempDir;
            string subDir = Path.Combine(tempDir, "sub");
            Directory.CreateDirectory(subDir);

            File.WriteAllText(Path.Combine(tempDir, "a.txt"), "a");
            File.WriteAllText(Path.Combine(subDir, "b.txt"), "b");

            string inputData = $"a.txt{Environment.NewLine}sub/b.txt{Environment.NewLine}";
            using var testCtx = new TestClipContext(inputData);
            var endpoint = new ClipEndpoint.Console();
            var files = new ClipType.Files();

            endpoint.Read(testCtx.Context, files);

            Assert.NotNull(files.Identity);
            Assert.Equal(2, files.Identity.Count);
            Assert.Equal(Path.GetFullPath("a.txt"), files.Identity[0]);
            Assert.Equal(Path.GetFullPath("sub/b.txt"), files.Identity[1]);
            Assert.True(File.Exists(files.Identity[0]));
            Assert.True(File.Exists(files.Identity[1]));
            Assert.Equal(0, testCtx.Status);
        }
        finally
        {
            Environment.CurrentDirectory = originalCwd;
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Fact]
    public void Read_Files_InvalidPath_SetsStatusAndDiagnostic()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "clipflow-console-inv-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        string originalCwd = Environment.CurrentDirectory;

        try
        {
            Environment.CurrentDirectory = tempDir;
            File.WriteAllText(Path.Combine(tempDir, "a.txt"), "a");

            string inputData = $"a.txt{Environment.NewLine}does-not-exist.txt{Environment.NewLine}";
            using var testCtx = new TestClipContext(inputData);
            var endpoint = new ClipEndpoint.Console();
            var files = new ClipType.Files();

            endpoint.Read(testCtx.Context, files);

            Assert.NotEqual(0, testCtx.Status);
            Assert.Contains("Path not found", testCtx.ErrorText, StringComparison.OrdinalIgnoreCase);
            Assert.Null(files.Identity);
        }
        finally
        {
            Environment.CurrentDirectory = originalCwd;
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Fact]
    public void Read_Files_BlankAndWhitespaceLines_AreIgnored()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "clipflow-console-blank-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        string originalCwd = Environment.CurrentDirectory;

        try
        {
            Environment.CurrentDirectory = tempDir;
            File.WriteAllText(Path.Combine(tempDir, "a.txt"), "a");
            File.WriteAllText(Path.Combine(tempDir, "b.txt"), "b");

            string inputData = $"{Environment.NewLine}   {Environment.NewLine}a.txt{Environment.NewLine}\t{Environment.NewLine}b.txt{Environment.NewLine}  {Environment.NewLine}";
            using var testCtx = new TestClipContext(inputData);
            var endpoint = new ClipEndpoint.Console();
            var files = new ClipType.Files();

            endpoint.Read(testCtx.Context, files);

            Assert.Equal(0, testCtx.Status);
            Assert.NotNull(files.Identity);
            Assert.Equal(2, files.Identity.Count);
            Assert.Equal(Path.GetFullPath("a.txt"), files.Identity[0]);
            Assert.Equal(Path.GetFullPath("b.txt"), files.Identity[1]);
        }
        finally
        {
            Environment.CurrentDirectory = originalCwd;
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Fact]
    public void Read_Files_AcceptsBothFilesAndDirectories()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "clipflow-console-mixed-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        string originalCwd = Environment.CurrentDirectory;

        try
        {
            Environment.CurrentDirectory = tempDir;
            string subDir = Path.Combine(tempDir, "subfolder");
            Directory.CreateDirectory(subDir);
            File.WriteAllText(Path.Combine(tempDir, "a.txt"), "a");

            string inputData = $"a.txt{Environment.NewLine}subfolder{Environment.NewLine}";
            using var testCtx = new TestClipContext(inputData);
            var endpoint = new ClipEndpoint.Console();
            var files = new ClipType.Files();

            endpoint.Read(testCtx.Context, files);

            Assert.Equal(0, testCtx.Status);
            Assert.NotNull(files.Identity);
            Assert.Equal(2, files.Identity.Count);
            Assert.True(File.Exists(files.Identity[0]));
            Assert.True(Directory.Exists(files.Identity[1]));
        }
        finally
        {
            Environment.CurrentDirectory = originalCwd;
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Fact]
    public void Read_Image_SetsStatusAndError()
    {
        using var testCtx = new TestClipContext();
        var endpoint = new ClipEndpoint.Console();
        var image = new ClipType.Image();

        endpoint.Read(testCtx.Context, image);

        Assert.NotEqual(0, testCtx.Status);
        Assert.Contains("Image is not compatible with Console", testCtx.ErrorText, StringComparison.OrdinalIgnoreCase);
        Assert.Null(image.Identity);
    }

    [Fact]
    public void Write_Text_WritesExactContentToOutput()
    {
        const string payload = "Exact console text output\nwith multiple lines.";
        using var testCtx = new TestClipContext();
        var endpoint = new ClipEndpoint.Console();
        var text = new ClipType.Text { Identity = payload };

        endpoint.Write(testCtx.Context, text);

        Assert.Equal(payload, testCtx.OutputText);
        Assert.Equal(0, testCtx.Status);
    }

    [Fact]
    public void Write_Html_WritesExactContentToOutput()
    {
        const string payload = "<div class=\"test\">HTML console output</div>";
        using var testCtx = new TestClipContext();
        var endpoint = new ClipEndpoint.Console();
        var html = new ClipType.Html { Identity = payload };

        endpoint.Write(testCtx.Context, html);

        Assert.Equal(payload, testCtx.OutputText);
        Assert.Equal(0, testCtx.Status);
    }

    [Fact]
    public void Write_Files_WritesOneFilePerLineToOutput()
    {
        var fileList = new List<string> { "first.txt", "second.doc", "third.png" };
        using var testCtx = new TestClipContext();
        var endpoint = new ClipEndpoint.Console();
        var files = new ClipType.Files { Identity = fileList };

        endpoint.Write(testCtx.Context, files);

        string expectedOutput = $"first.txt{Environment.NewLine}second.doc{Environment.NewLine}third.png{Environment.NewLine}";
        Assert.Equal(expectedOutput, testCtx.OutputText);
        Assert.Equal(0, testCtx.Status);
    }
}
