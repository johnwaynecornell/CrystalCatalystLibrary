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
        string inputData = $"alpha.txt{Environment.NewLine}beta.txt{Environment.NewLine}gamma.txt{Environment.NewLine}";
        using var testCtx = new TestClipContext(inputData);
        var endpoint = new ClipEndpoint.Console();
        var files = new ClipType.Files();

        endpoint.Read(testCtx.Context, files);

        Assert.NotNull(files.Identity);
        Assert.Equal(new List<string> { "alpha.txt", "beta.txt", "gamma.txt" }, files.Identity);
        Assert.Equal(0, testCtx.Status);
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
