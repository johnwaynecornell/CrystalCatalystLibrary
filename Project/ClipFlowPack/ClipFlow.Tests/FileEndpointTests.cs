using System;
using System.Collections.Generic;
using System.IO;
using ClipFlow.Format;
using SkiaSharp;
using Xunit;

namespace ClipFlow.Tests;

public class FileEndpointTests : IDisposable
{
    private readonly string _tempDirectory;

    public FileEndpointTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "clipflow-file-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

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

    [Fact]
    public void WriteAndRead_Text_PreservesExactContent()
    {
        string filePath = Path.Combine(_tempDirectory, "test.txt");
        const string expectedText = "Hello File Endpoint!\nLine 2\nLine 3 with unicode: 🚀";

        var endpoint = new ClipEndpoint.File(filePath);
        var writeType = new ClipType.Text { Identity = expectedText };
        using var writeCtx = new TestClipContext();

        endpoint.Write(writeCtx.Context, writeType);

        Assert.Equal(0, writeCtx.Status);
        Assert.True(System.IO.File.Exists(filePath));
        Assert.Equal(expectedText, System.IO.File.ReadAllText(filePath));

        var readType = new ClipType.Text();
        using var readCtx = new TestClipContext();
        endpoint.Read(readCtx.Context, readType);

        Assert.Equal(0, readCtx.Status);
        Assert.Equal(expectedText, readType.Identity);
    }

    [Fact]
    public void WriteAndRead_Html_PreservesExactContent()
    {
        string filePath = Path.Combine(_tempDirectory, "test.html");
        const string expectedHtml = "<!DOCTYPE html><html><body><p>HTML Test</p></body></html>";

        var endpoint = new ClipEndpoint.File(filePath);
        var writeType = new ClipType.Html { Identity = expectedHtml };
        using var writeCtx = new TestClipContext();

        endpoint.Write(writeCtx.Context, writeType);

        Assert.Equal(0, writeCtx.Status);
        Assert.True(System.IO.File.Exists(filePath));
        Assert.Equal(expectedHtml, System.IO.File.ReadAllText(filePath));

        var readType = new ClipType.Html();
        using var readCtx = new TestClipContext();
        endpoint.Read(readCtx.Context, readType);

        Assert.Equal(0, readCtx.Status);
        Assert.Equal(expectedHtml, readType.Identity);
    }

    [Fact]
    public void WriteAndRead_Files_RoundTripsLineList()
    {
        string f1 = Path.Combine(_tempDirectory, "alpha.txt");
        string f2 = Path.Combine(_tempDirectory, "beta.doc");
        string f3 = Path.Combine(_tempDirectory, "gamma.png");
        File.WriteAllText(f1, "1");
        File.WriteAllText(f2, "2");
        File.WriteAllText(f3, "3");

        string listFile = Path.Combine(_tempDirectory, "files.list");
        var expectedFiles = new List<string> { f1, f2, f3 };

        var endpoint = new ClipEndpoint.File(listFile);
        var writeType = new ClipType.Files { Identity = expectedFiles };
        using var writeCtx = new TestClipContext();

        endpoint.Write(writeCtx.Context, writeType);

        Assert.Equal(0, writeCtx.Status);
        Assert.True(System.IO.File.Exists(listFile));

        var readType = new ClipType.Files();
        using var readCtx = new TestClipContext();
        endpoint.Read(readCtx.Context, readType);

        Assert.Equal(0, readCtx.Status);
        Assert.Equal(expectedFiles, readType.Identity);
    }

    [Fact]
    public void Read_Files_RelativePaths_ResolveAgainstCurrentWorkingDirectory()
    {
        string originalCwd = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = _tempDirectory;
            string subDir = Path.Combine(_tempDirectory, "sub");
            Directory.CreateDirectory(subDir);

            File.WriteAllText(Path.Combine(_tempDirectory, "item1.txt"), "1");
            File.WriteAllText(Path.Combine(subDir, "item2.txt"), "2");

            string otherDir = Path.Combine(Path.GetTempPath(), "clipflow-listdir-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(otherDir);

            try
            {
                // The list file is stored in otherDir, but contains paths relative to CWD (_tempDirectory)
                string listFile = Path.Combine(otherDir, "list.txt");
                File.WriteAllText(listFile, $"item1.txt{Environment.NewLine}sub/item2.txt{Environment.NewLine}");

                var endpoint = new ClipEndpoint.File(listFile);
                var readType = new ClipType.Files();
                using var readCtx = new TestClipContext();

                endpoint.Read(readCtx.Context, readType);

                Assert.Equal(0, readCtx.Status);
                Assert.NotNull(readType.Identity);
                Assert.Equal(2, readType.Identity.Count);
                Assert.Equal(Path.GetFullPath("item1.txt"), readType.Identity[0]);
                Assert.Equal(Path.GetFullPath("sub/item2.txt"), readType.Identity[1]);
                Assert.True(File.Exists(readType.Identity[0]));
                Assert.True(File.Exists(readType.Identity[1]));
            }
            finally
            {
                if (Directory.Exists(otherDir))
                {
                    try { Directory.Delete(otherDir, true); } catch { }
                }
            }
        }
        finally
        {
            Environment.CurrentDirectory = originalCwd;
        }
    }

    [Fact]
    public void Read_Files_InvalidPathInList_SetsStatusAndDiagnostic()
    {
        string f1 = Path.Combine(_tempDirectory, "valid.txt");
        File.WriteAllText(f1, "valid");

        string listFile = Path.Combine(_tempDirectory, "bad.list");
        File.WriteAllText(listFile, $"{f1}{Environment.NewLine}{Path.Combine(_tempDirectory, "nonexistent.txt")}{Environment.NewLine}");

        var endpoint = new ClipEndpoint.File(listFile);
        var readType = new ClipType.Files();
        using var readCtx = new TestClipContext();

        endpoint.Read(readCtx.Context, readType);

        Assert.NotEqual(0, readCtx.Status);
        Assert.Contains("Path not found", readCtx.ErrorText, StringComparison.OrdinalIgnoreCase);
        Assert.Null(readType.Identity);
    }

    [Fact]
    public void Read_Files_MissingListFile_SetsStatusAndDiagnostic()
    {
        string listFile = Path.Combine(_tempDirectory, "does_not_exist.list");

        var endpoint = new ClipEndpoint.File(listFile);
        var readType = new ClipType.Files();
        using var readCtx = new TestClipContext();

        endpoint.Read(readCtx.Context, readType);

        Assert.NotEqual(0, readCtx.Status);
        Assert.Contains("File not found", readCtx.ErrorText, StringComparison.OrdinalIgnoreCase);
        Assert.Null(readType.Identity);
    }

    [Fact]
    public void Read_Files_BlankLinesInList_AreIgnored()
    {
        string f1 = Path.Combine(_tempDirectory, "one.txt");
        string f2 = Path.Combine(_tempDirectory, "two.txt");
        File.WriteAllText(f1, "1");
        File.WriteAllText(f2, "2");

        string listFile = Path.Combine(_tempDirectory, "blank.list");
        File.WriteAllText(listFile, $"{Environment.NewLine}   {Environment.NewLine}{f1}{Environment.NewLine}\t{Environment.NewLine}{f2}{Environment.NewLine}   {Environment.NewLine}");

        var endpoint = new ClipEndpoint.File(listFile);
        var readType = new ClipType.Files();
        using var readCtx = new TestClipContext();

        endpoint.Read(readCtx.Context, readType);

        Assert.Equal(0, readCtx.Status);
        Assert.NotNull(readType.Identity);
        Assert.Equal(new List<string> { f1, f2 }, readType.Identity);
    }

    [Fact]
    public void Read_Files_AcceptsBothFilesAndDirectories()
    {
        string file = Path.Combine(_tempDirectory, "file.txt");
        string subDir = Path.Combine(_tempDirectory, "subfolder");
        File.WriteAllText(file, "file");
        Directory.CreateDirectory(subDir);

        string listFile = Path.Combine(_tempDirectory, "mixed.list");
        File.WriteAllText(listFile, $"{file}{Environment.NewLine}{subDir}{Environment.NewLine}");

        var endpoint = new ClipEndpoint.File(listFile);
        var readType = new ClipType.Files();
        using var readCtx = new TestClipContext();

        endpoint.Read(readCtx.Context, readType);

        Assert.Equal(0, readCtx.Status);
        Assert.NotNull(readType.Identity);
        Assert.Equal(new List<string> { file, subDir }, readType.Identity);
    }

    [Fact]
    public void WriteAndRead_PngImage_SucceedsAndPreservesDimensions()
    {
        string filePath = Path.Combine(_tempDirectory, "output.png");
        using var originalImage = CreateTestImage(4, 4);

        var endpoint = new ClipEndpoint.File(filePath);
        var writeType = new ClipType.Image { Identity = originalImage };
        using var writeCtx = new TestClipContext();

        endpoint.Write(writeCtx.Context, writeType);

        Assert.Equal(0, writeCtx.Status);
        Assert.True(System.IO.File.Exists(filePath));
        Assert.True(new FileInfo(filePath).Length > 0);

        // Verify Skia can decode the written file
        using var decodedImage = SKImage.FromEncodedData(filePath);
        Assert.NotNull(decodedImage);
        Assert.Equal(4, decodedImage.Width);
        Assert.Equal(4, decodedImage.Height);

        // Verify File.Read(Image) reads back into ClipType.Image.Identity
        var readType = new ClipType.Image();
        using var readCtx = new TestClipContext();
        endpoint.Read(readCtx.Context, readType);

        Assert.Equal(0, readCtx.Status);
        Assert.NotNull(readType.Identity);
        Assert.Equal(4, readType.Identity.Width);
        Assert.Equal(4, readType.Identity.Height);

        readType.Identity.Dispose();
    }

    [Fact]
    public void Read_BmpImage_SucceedsAndPreservesDimensions()
    {
        string filePath = Path.Combine(_tempDirectory, "input.bmp");
        byte[] bmpBytes = BmpTestHelper.CreateMinimal24BppBmp(4, 4);
        System.IO.File.WriteAllBytes(filePath, bmpBytes);

        var endpoint = new ClipEndpoint.File(filePath);
        var readType = new ClipType.Image();
        using var readCtx = new TestClipContext();

        endpoint.Read(readCtx.Context, readType);

        Assert.Equal(0, readCtx.Status);
        Assert.NotNull(readType.Identity);
        Assert.Equal(4, readType.Identity.Width);
        Assert.Equal(4, readType.Identity.Height);

        readType.Identity.Dispose();
    }

    [Fact]
    public void Write_BmpImage_HandlesRuntimeCapabilityGracefully()
    {
        string filePath = Path.Combine(_tempDirectory, "output.bmp");
        using var originalImage = CreateTestImage(4, 4);

        var endpoint = new ClipEndpoint.File(filePath);
        var writeType = new ClipType.Image { Identity = originalImage };
        using var writeCtx = new TestClipContext();

        endpoint.Write(writeCtx.Context, writeType);

        // On Linux SkiaSharp runtime where BMP encode is unsupported, it sets error/status 1 gracefully without crash
        if (writeCtx.Status == 0)
        {
            Assert.True(System.IO.File.Exists(filePath));
            Assert.True(new FileInfo(filePath).Length > 0);
            using var decodedImage = SKImage.FromEncodedData(filePath);
            Assert.NotNull(decodedImage);
            Assert.Equal(4, decodedImage.Width);
            Assert.Equal(4, decodedImage.Height);
        }
        else
        {
            Assert.Contains("Failed to encode image", writeCtx.ErrorText);
        }
    }

    [Fact]
    public void Read_Image_NonExistentFile_SetsStatusAndError()
    {
        string filePath = Path.Combine(_tempDirectory, "missing.png");
        var endpoint = new ClipEndpoint.File(filePath);
        var image = new ClipType.Image();
        using var testCtx = new TestClipContext();

        endpoint.Read(testCtx.Context, image);

        Assert.NotEqual(0, testCtx.Status);
        Assert.Contains("Image file not found", testCtx.ErrorText, StringComparison.OrdinalIgnoreCase);
        Assert.Null(image.Identity);
    }

    [Fact]
    public void Write_Image_NullIdentity_SetsStatusAndError()
    {
        string filePath = Path.Combine(_tempDirectory, "null_image.png");
        var endpoint = new ClipEndpoint.File(filePath);
        var image = new ClipType.Image { Identity = null };
        using var testCtx = new TestClipContext();

        endpoint.Write(testCtx.Context, image);

        Assert.NotEqual(0, testCtx.Status);
        Assert.Contains("Image data is not available", testCtx.ErrorText, StringComparison.OrdinalIgnoreCase);
        Assert.False(System.IO.File.Exists(filePath));
    }
}
