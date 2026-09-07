using ClipFlow.Format;
using Xunit;

namespace ClipFlow.Tests;

public class StringEndpointTests
{
    [Fact]
    public void Read_Text_SetsIdentityAndZeroStatus()
    {
        const string content = "Sample plain text string";
        var endpoint = new ClipEndpoint.String(content);
        var text = new ClipType.Text();
        using var testCtx = new TestClipContext();

        endpoint.Read(testCtx.Context, text);

        Assert.Equal(content, text.Identity);
        Assert.Equal(0, testCtx.Status);
        Assert.Empty(testCtx.ErrorText);
    }

    [Fact]
    public void Read_Html_SetsIdentityAndZeroStatus()
    {
        const string content = "<html><body><h1>Hello</h1></body></html>";
        var endpoint = new ClipEndpoint.String(content);
        var html = new ClipType.Html();
        using var testCtx = new TestClipContext();

        endpoint.Read(testCtx.Context, html);

        Assert.Equal(content, html.Identity);
        Assert.Equal(0, testCtx.Status);
        Assert.Empty(testCtx.ErrorText);
    }

    [Fact]
    public void Read_Image_SetsNonZeroStatusAndErrorMessage()
    {
        var endpoint = new ClipEndpoint.String("some_image_data");
        var image = new ClipType.Image();
        using var testCtx = new TestClipContext();

        endpoint.Read(testCtx.Context, image);

        Assert.NotEqual(0, testCtx.Status);
        Assert.Contains("does not support image", testCtx.ErrorText, StringComparison.OrdinalIgnoreCase);
        Assert.Null(image.Identity);
    }

    [Fact]
    public void Read_Files_SetsNonZeroStatusAndErrorMessage()
    {
        var endpoint = new ClipEndpoint.String("file1.txt\nfile2.txt");
        var files = new ClipType.Files();
        using var testCtx = new TestClipContext();

        endpoint.Read(testCtx.Context, files);

        Assert.NotEqual(0, testCtx.Status);
        Assert.Contains("does not support Files", testCtx.ErrorText, StringComparison.OrdinalIgnoreCase);
        Assert.Null(files.Identity);
    }

    [Fact]
    public void Write_Text_SetsNonZeroStatusAndErrorMessage()
    {
        var endpoint = new ClipEndpoint.String("dummy");
        var text = new ClipType.Text { Identity = "hello" };
        using var testCtx = new TestClipContext();

        endpoint.Write(testCtx.Context, text);

        Assert.NotEqual(0, testCtx.Status);
        Assert.Contains("input only", testCtx.ErrorText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Write_Html_SetsNonZeroStatusAndErrorMessage()
    {
        var endpoint = new ClipEndpoint.String("dummy");
        var html = new ClipType.Html { Identity = "<b>hello</b>" };
        using var testCtx = new TestClipContext();

        endpoint.Write(testCtx.Context, html);

        Assert.NotEqual(0, testCtx.Status);
        Assert.Contains("input only", testCtx.ErrorText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Write_Image_SetsNonZeroStatusAndErrorMessage()
    {
        var endpoint = new ClipEndpoint.String("dummy");
        var image = new ClipType.Image();
        using var testCtx = new TestClipContext();

        endpoint.Write(testCtx.Context, image);

        Assert.NotEqual(0, testCtx.Status);
        Assert.Contains("input only", testCtx.ErrorText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Write_Files_SetsNonZeroStatusAndErrorMessage()
    {
        var endpoint = new ClipEndpoint.String("dummy");
        var files = new ClipType.Files { Identity = new List<string> { "a.txt" } };
        using var testCtx = new TestClipContext();

        endpoint.Write(testCtx.Context, files);

        Assert.NotEqual(0, testCtx.Status);
        Assert.Contains("input only", testCtx.ErrorText, StringComparison.OrdinalIgnoreCase);
    }
}
