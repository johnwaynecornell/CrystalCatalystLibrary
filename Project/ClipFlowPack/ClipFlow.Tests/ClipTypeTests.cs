using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using ClipFlow.Format;
using SkiaSharp;
using Xunit;

namespace ClipFlow.Tests;

public class ClipTypeTests
{
    private static SKImage CreateTestImage(int width = 4, int height = 4)
    {
        using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                bitmap.SetPixel(x, y, new SKColor((byte)(x * 50), (byte)(y * 50), 200, 255));
            }
        }
        return SKImage.FromBitmap(bitmap);
    }

    #region Text Tests

    [Fact]
    public void Text_Provide_WithValidIdentity_DecodesBackToOriginalString()
    {
        const string message = "ClipFlow Text Payload with unicode: 🚀";
        var textType = new ClipType.Text { Identity = message };
        using var testCtx = new TestClipContext();

        byte[]? bytes = textType.Provide(testCtx.Context, null!, "text/plain");

        Assert.NotNull(bytes);
        Assert.Equal(0, testCtx.Status);
        string decoded = Encoding.UTF8.GetString(bytes);
        Assert.Equal(message, decoded);
    }

    [Fact]
    public void Text_Provide_WithNullIdentity_ReturnsNullAndSetsError()
    {
        var textType = new ClipType.Text { Identity = null };
        using var testCtx = new TestClipContext();

        byte[]? bytes = textType.Provide(testCtx.Context, null!, "text/plain");

        Assert.Null(bytes);
        Assert.NotEqual(0, testCtx.Status);
        Assert.Contains("Identity is null", testCtx.ErrorText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Text_Receive_ExtractsUTF8String()
    {
        const string original = "Received text content";
        byte[] bytes = Encoding.UTF8.GetBytes(original);
        IntPtr ptr = Marshal.AllocHGlobal(bytes.Length);

        try
        {
            Marshal.Copy(bytes, 0, ptr, bytes.Length);

            var textType = new ClipType.Text();
            using var testCtx = new TestClipContext();

            textType.Receive(testCtx.Context, null!, "text/plain", ptr, (IntPtr)bytes.Length);

            Assert.Equal(original, textType.Identity);
            Assert.Equal(0, testCtx.Status);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    #endregion

    #region HTML Tests

    [Fact]
    public void Html_Provide_WithValidIdentity_DecodesBackToOriginalHtml()
    {
        const string htmlContent = "<div class=\"note\"><p>ClipFlow HTML</p></div>";
        var htmlType = new ClipType.Html { Identity = htmlContent };
        using var testCtx = new TestClipContext();

        byte[]? bytes = htmlType.Provide(testCtx.Context, null!, "text/html");

        Assert.NotNull(bytes);
        Assert.Equal(0, testCtx.Status);
        string decoded = Encoding.UTF8.GetString(bytes);
        Assert.Equal(htmlContent, decoded);
    }

    [Fact]
    public void Html_Provide_WithNullIdentity_ReturnsNullAndSetsError()
    {
        var htmlType = new ClipType.Html { Identity = null };
        using var testCtx = new TestClipContext();

        byte[]? bytes = htmlType.Provide(testCtx.Context, null!, "text/html");

        Assert.Null(bytes);
        Assert.NotEqual(0, testCtx.Status);
        Assert.Contains("Identity is null", testCtx.ErrorText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Html_Receive_ExtractsUTF8Html()
    {
        const string originalHtml = "<span>Formatted HTML content</span>";
        byte[] bytes = Encoding.UTF8.GetBytes(originalHtml);
        IntPtr ptr = Marshal.AllocHGlobal(bytes.Length);

        try
        {
            Marshal.Copy(bytes, 0, ptr, bytes.Length);

            var htmlType = new ClipType.Html();
            using var testCtx = new TestClipContext();

            htmlType.Receive(testCtx.Context, null!, "text/html", ptr, (IntPtr)bytes.Length);

            Assert.Equal(originalHtml, htmlType.Identity);
            Assert.Equal(0, testCtx.Status);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    #endregion

    #region Image Tests

    [Fact]
    public void Image_Provide_Png_ReturnsValidEncodedBytesWithPreservedDimensions()
    {
        using var originalImage = CreateTestImage(4, 4);
        var imageType = new ClipType.Image { Identity = originalImage };

        using var testCtx = new TestClipContext();
        byte[]? pngBytes = imageType.Provide(testCtx.Context, null!, "image/png");

        Assert.NotNull(pngBytes);
        Assert.NotEmpty(pngBytes);
        Assert.Equal(0, testCtx.Status);

        using var decoded = SKImage.FromEncodedData(pngBytes);
        Assert.NotNull(decoded);
        Assert.Equal(4, decoded.Width);
        Assert.Equal(4, decoded.Height);
    }

    [Fact]
    public void Image_Provide_Bmp_HandlesRuntimeSupportGracefully()
    {
        using var originalImage = CreateTestImage(4, 4);
        var imageType = new ClipType.Image { Identity = originalImage };

        using var testCtx = new TestClipContext();
        byte[]? bmpBytes = imageType.Provide(testCtx.Context, null!, "image/bmp");

        if (bmpBytes != null)
        {
            // If runtime supports BMP encoding
            Assert.Equal(0, testCtx.Status);
            using var decoded = SKImage.FromEncodedData(bmpBytes);
            Assert.NotNull(decoded);
            Assert.Equal(4, decoded.Width);
            Assert.Equal(4, decoded.Height);
        }
        else
        {
            // If runtime (e.g. Linux libSkiaSharp) does not support BMP encoding
            Assert.NotEqual(0, testCtx.Status);
            Assert.Contains("Failed to encode image as BMP", testCtx.ErrorText);
        }
    }

    [Fact]
    public void Image_Provide_NullIdentity_ReturnsNullAndSetsError()
    {
        var imageType = new ClipType.Image { Identity = null };
        using var testCtx = new TestClipContext();

        byte[]? bytes = imageType.Provide(testCtx.Context, null!, "image/png");

        Assert.Null(bytes);
        Assert.NotEqual(0, testCtx.Status);
        Assert.Contains("Identity is null", testCtx.ErrorText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Image_Receive_Png_ExtractsSKImageWithMatchingDimensions()
    {
        using var originalImage = CreateTestImage(4, 4);
        using var encodedData = originalImage.Encode(SKEncodedImageFormat.Png, 100);
        byte[] bytes = encodedData.ToArray();

        IntPtr ptr = Marshal.AllocHGlobal(bytes.Length);
        try
        {
            Marshal.Copy(bytes, 0, ptr, bytes.Length);

            var imageType = new ClipType.Image();
            using var testCtx = new TestClipContext();

            imageType.Receive(testCtx.Context, null!, "image/png", ptr, (IntPtr)bytes.Length);

            Assert.Equal(0, testCtx.Status);
            Assert.NotNull(imageType.Identity);
            Assert.Equal(4, imageType.Identity.Width);
            Assert.Equal(4, imageType.Identity.Height);

            imageType.Identity.Dispose();
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    [Fact]
    public void Image_Receive_Bmp_ExtractsSKImageWithMatchingDimensions()
    {
        byte[] bytes = BmpTestHelper.CreateMinimal24BppBmp(4, 4);

        IntPtr ptr = Marshal.AllocHGlobal(bytes.Length);
        try
        {
            Marshal.Copy(bytes, 0, ptr, bytes.Length);

            var imageType = new ClipType.Image();
            using var testCtx = new TestClipContext();

            imageType.Receive(testCtx.Context, null!, "image/bmp", ptr, (IntPtr)bytes.Length);

            Assert.Equal(0, testCtx.Status);
            Assert.NotNull(imageType.Identity);
            Assert.Equal(4, imageType.Identity.Width);
            Assert.Equal(4, imageType.Identity.Height);

            imageType.Identity.Dispose();
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    [Fact]
    public void Image_Receive_InvalidData_SetsStatusAndError()
    {
        byte[] invalidBytes = new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55 };
        IntPtr ptr = Marshal.AllocHGlobal(invalidBytes.Length);

        try
        {
            Marshal.Copy(invalidBytes, 0, ptr, invalidBytes.Length);

            var imageType = new ClipType.Image();
            using var testCtx = new TestClipContext();

            imageType.Receive(testCtx.Context, null!, "image/png", ptr, (IntPtr)invalidBytes.Length);

            Assert.NotEqual(0, testCtx.Status);
            Assert.Contains("Unable to decode clipboard image", testCtx.ErrorText, StringComparison.OrdinalIgnoreCase);
            Assert.Null(imageType.Identity);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    #endregion

    #region Files Tests

    [Fact]
    public void Files_Provide_WithValidList_DecodesBackToLines()
    {
        var originalList = new List<string> { "a.txt", "b.txt" };
        var filesType = new ClipType.Files { Identity = originalList };
        using var testCtx = new TestClipContext();

        byte[]? bytes = filesType.Provide(testCtx.Context, null!, "text/file-uri");

        Assert.NotNull(bytes);
        Assert.Equal(0, testCtx.Status);

        string decoded = Encoding.UTF8.GetString(bytes);
        string expected = $"a.txt{Environment.NewLine}b.txt{Environment.NewLine}";
        Assert.Equal(expected, decoded);
    }

    [Fact]
    public void Files_Provide_WithNullIdentity_ReturnsNullAndSetsError()
    {
        var filesType = new ClipType.Files { Identity = null };
        using var testCtx = new TestClipContext();

        byte[]? bytes = filesType.Provide(testCtx.Context, null!, "text/file-uri");

        Assert.Null(bytes);
        Assert.NotEqual(0, testCtx.Status);
        Assert.Contains("Identity is null", testCtx.ErrorText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Files_Receive_ExplicitByteLength_ProtectsDataPlusSizeInvariantWithoutNullTerminationDependency()
    {
        // Construct payload "a.txt\nb.txt" followed by intentional trailing garbage bytes in buffer
        string validPayload = $"a.txt{Environment.NewLine}b.txt";
        byte[] validBytes = Encoding.UTF8.GetBytes(validPayload);

        // Allocate buffer larger than validBytes with non-null garbage at the end
        byte[] buffer = new byte[validBytes.Length + 16];
        Array.Copy(validBytes, buffer, validBytes.Length);
        for (int i = validBytes.Length; i < buffer.Length; i++)
        {
            buffer[i] = (byte)'X'; // Non-zero garbage to ensure null-termination is not relied on
        }

        IntPtr ptr = Marshal.AllocHGlobal(buffer.Length);

        try
        {
            Marshal.Copy(buffer, 0, ptr, buffer.Length);

            var filesType = new ClipType.Files();
            using var testCtx = new TestClipContext();

            // Pass exact validBytes.Length as size
            filesType.Receive(testCtx.Context, null!, "text/file-uri", ptr, (IntPtr)validBytes.Length);

            Assert.Equal(0, testCtx.Status);
            Assert.NotNull(filesType.Identity);
            Assert.Equal(new List<string> { Path.GetFullPath("a.txt"), Path.GetFullPath("b.txt") }, filesType.Identity);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    [Fact]
    public void Files_Receive_NormalizesUris_Spaces_Unicode_MultipleEntries()
    {
        string payload = string.Join("\r\n", new[]
        {
            "# Comment line in uri-list",
            "file:///tmp/my%20test%20file.txt",
            "   ",
            "file://localhost/tmp/unicode_%C3%A9cole.txt",
            "/tmp/plain_path.txt"
        });

        byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);
        IntPtr ptr = Marshal.AllocHGlobal(payloadBytes.Length);

        try
        {
            Marshal.Copy(payloadBytes, 0, ptr, payloadBytes.Length);

            var filesType = new ClipType.Files();
            using var testCtx = new TestClipContext();

            filesType.Receive(testCtx.Context, null!, "text/file-uri", ptr, (IntPtr)payloadBytes.Length);

            Assert.Equal(0, testCtx.Status);
            Assert.NotNull(filesType.Identity);
            Assert.Equal(3, filesType.Identity.Count);

            string expected1 = Path.GetFullPath("/tmp/my test file.txt");
            string expected2 = Path.GetFullPath("/tmp/unicode_école.txt");
            string expected3 = Path.GetFullPath("/tmp/plain_path.txt");

            Assert.Equal(expected1, filesType.Identity[0]);
            Assert.Equal(expected2, filesType.Identity[1]);
            Assert.Equal(expected3, filesType.Identity[2]);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    [Fact]
    public void Files_RoundTrip_ProvideAndReceive_PreservesNormalizedLocalPaths()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "clipflow-roundtrip-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            string file1 = Path.Combine(tempDir, "first file with spaces.txt");
            string file2 = Path.Combine(tempDir, "second_é_file.txt");
            File.WriteAllText(file1, "1");
            File.WriteAllText(file2, "2");

            var originalPaths = new List<string> { Path.GetFullPath(file1), Path.GetFullPath(file2) };
            var provideFiles = new ClipType.Files { Identity = originalPaths };

            using var provideCtx = new TestClipContext();
            byte[]? providedBytes = provideFiles.Provide(provideCtx.Context, null!, "text/file-uri");

            Assert.NotNull(providedBytes);
            Assert.Equal(0, provideCtx.Status);

            IntPtr ptr = Marshal.AllocHGlobal(providedBytes.Length);
            try
            {
                Marshal.Copy(providedBytes, 0, ptr, providedBytes.Length);

                var receiveFiles = new ClipType.Files();
                using var receiveCtx = new TestClipContext();
                receiveFiles.Receive(receiveCtx.Context, null!, "text/file-uri", ptr, (IntPtr)providedBytes.Length);

                Assert.Equal(0, receiveCtx.Status);
                Assert.NotNull(receiveFiles.Identity);
                Assert.Equal(originalPaths, receiveFiles.Identity);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Theory]
    [InlineData("file:///home/user/document.txt", "/home/user/document.txt")]
    [InlineData("file://localhost/home/user/document.txt", "/home/user/document.txt")]
    [InlineData("file:///home/user/my%20file.txt", "/home/user/my file.txt")]
    [InlineData("file:///home/user/caf%C3%A9.txt", "/home/user/café.txt")]
    [InlineData("/home/user/already_local.txt", "/home/user/already_local.txt")]
    public void Files_NormalizePathOrUri_HandlesVariousSchemes(string input, string expectedLocalPath)
    {
        string result = ClipType.Files.NormalizePathOrUri(input);
        string expected = Path.GetFullPath(expectedLocalPath);
        Assert.Equal(expected, result);
    }

    #endregion
}
