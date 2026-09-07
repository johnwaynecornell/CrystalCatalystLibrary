using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClipFlow.Format;
using Xunit;

namespace ClipFlow.Tests;

public class DirectoryEndpointTests : IDisposable
{
    private readonly string _tempDirectory;

    public DirectoryEndpointTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "clipflow-dir-test-" + Guid.NewGuid().ToString("N"));
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

    [Fact]
    public void Read_Files_ReturnsAllFilesInDirectory()
    {
        string file1 = Path.Combine(_tempDirectory, "alpha.txt");
        string file2 = Path.Combine(_tempDirectory, "beta.doc");
        string file3 = Path.Combine(_tempDirectory, "gamma.png");

        File.WriteAllText(file1, "1");
        File.WriteAllText(file2, "2");
        File.WriteAllText(file3, "3");

        var endpoint = new ClipEndpoint.Directory(_tempDirectory);
        var files = new ClipType.Files();
        using var testCtx = new TestClipContext();

        endpoint.Read(testCtx.Context, files);

        Assert.Equal(0, testCtx.Status);
        Assert.NotNull(files.Identity);

        var expected = new[] { file1, file2, file3 }.OrderBy(x => x).ToList();
        var actual = files.Identity.OrderBy(x => x).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Read_Files_RelativeDirectoryPath_ReturnsNormalizedFullPaths()
    {
        string originalCwd = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = _tempDirectory;
            string subDir = Path.Combine(_tempDirectory, "mysub");
            Directory.CreateDirectory(subDir);
            string file1 = Path.Combine(subDir, "one.txt");
            string file2 = Path.Combine(subDir, "two.txt");
            File.WriteAllText(file1, "1");
            File.WriteAllText(file2, "2");

            var endpoint = new ClipEndpoint.Directory("./mysub");
            var files = new ClipType.Files();
            using var testCtx = new TestClipContext();

            endpoint.Read(testCtx.Context, files);

            Assert.Equal(0, testCtx.Status);
            Assert.NotNull(files.Identity);
            Assert.Equal(2, files.Identity.Count);
            foreach (var path in files.Identity)
            {
                Assert.True(Path.IsPathRooted(path));
                Assert.True(File.Exists(path));
            }
        }
        finally
        {
            Environment.CurrentDirectory = originalCwd;
        }
    }

    [Fact]
    public void Read_Files_WildcardAsterisk_MatchesFilteredFilesAndUsesResolvedDirectoryPath()
    {
        // Regression test: Ensures Directory.Read checks existence of resolved directoryPath,
        // not the literal wildcard path (<tempdir>/*.txt)
        string file1 = Path.Combine(_tempDirectory, "one.txt");
        string file2 = Path.Combine(_tempDirectory, "two.txt");
        string file3 = Path.Combine(_tempDirectory, "image.png");

        File.WriteAllText(file1, "content one");
        File.WriteAllText(file2, "content two");
        File.WriteAllText(file3, "content png");

        string wildcardPath = Path.Combine(_tempDirectory, "*.txt");
        var endpoint = new ClipEndpoint.Directory(wildcardPath);
        var files = new ClipType.Files();
        using var testCtx = new TestClipContext();

        endpoint.Read(testCtx.Context, files);

        Assert.Equal(0, testCtx.Status);
        Assert.NotNull(files.Identity);

        var expected = new[] { file1, file2 }.OrderBy(x => x).ToList();
        var actual = files.Identity.OrderBy(x => x).ToList();

        Assert.Equal(expected, actual);
        Assert.DoesNotContain(file3, files.Identity);
    }

    [Fact]
    public void Read_Files_WildcardQuestionMark_MatchesFilteredFiles()
    {
        string file1 = Path.Combine(_tempDirectory, "one.txt");
        string file2 = Path.Combine(_tempDirectory, "two.txt");
        string file3 = Path.Combine(_tempDirectory, "ten.txt");

        File.WriteAllText(file1, "content one");
        File.WriteAllText(file2, "content two");
        File.WriteAllText(file3, "content ten");

        string wildcardPath = Path.Combine(_tempDirectory, "t?o.txt");
        var endpoint = new ClipEndpoint.Directory(wildcardPath);
        var files = new ClipType.Files();
        using var testCtx = new TestClipContext();

        endpoint.Read(testCtx.Context, files);

        Assert.Equal(0, testCtx.Status);
        Assert.NotNull(files.Identity);

        Assert.Single(files.Identity);
        Assert.Equal(file2, files.Identity[0]);
    }

    [Fact]
    public void Read_Files_NonExistentDirectory_SetsStatusAndError()
    {
        string nonExistentPath = Path.Combine(_tempDirectory, "does_not_exist", "*.txt");
        var endpoint = new ClipEndpoint.Directory(nonExistentPath);
        var files = new ClipType.Files();
        using var testCtx = new TestClipContext();

        endpoint.Read(testCtx.Context, files);

        Assert.NotEqual(0, testCtx.Status);
        Assert.Contains("Directory not found", testCtx.ErrorText, StringComparison.OrdinalIgnoreCase);
        Assert.Null(files.Identity);
    }

    [Fact]
    public void Write_Files_CreatesDestinationDirectoryAndCopiesFilesWithContentsPreserved()
    {
        string sourceDir = Path.Combine(_tempDirectory, "source");
        Directory.CreateDirectory(sourceDir);

        string srcFile1 = Path.Combine(sourceDir, "source1.txt");
        string srcFile2 = Path.Combine(sourceDir, "source2.json");
        const string content1 = "Unique payload for file 1";
        const string content2 = "{\"key\": \"value 2\"}";

        File.WriteAllText(srcFile1, content1);
        File.WriteAllText(srcFile2, content2);

        string destDir = Path.Combine(_tempDirectory, "destination_new");
        Assert.False(Directory.Exists(destDir));

        var endpoint = new ClipEndpoint.Directory(destDir);
        var files = new ClipType.Files
        {
            Identity = new List<string> { srcFile1, srcFile2 }
        };
        using var testCtx = new TestClipContext();

        endpoint.Write(testCtx.Context, files);

        Assert.Equal(0, testCtx.Status);
        Assert.True(Directory.Exists(destDir));

        string destFile1 = Path.Combine(destDir, "source1.txt");
        string destFile2 = Path.Combine(destDir, "source2.json");

        Assert.True(File.Exists(destFile1));
        Assert.True(File.Exists(destFile2));
        Assert.Equal(content1, File.ReadAllText(destFile1));
        Assert.Equal(content2, File.ReadAllText(destFile2));
    }

    [Fact]
    public void Write_Files_ExpandsDirectoriesRecursively()
    {
        string sourceDir = Path.Combine(_tempDirectory, "source_tree");
        string subDir = Path.Combine(sourceDir, "subdir");
        Directory.CreateDirectory(subDir);

        string rootFile = Path.Combine(sourceDir, "root.txt");
        string subFile = Path.Combine(subDir, "nested.txt");
        File.WriteAllText(rootFile, "root content");
        File.WriteAllText(subFile, "nested content");

        string destDir = Path.Combine(_tempDirectory, "destination_tree");
        var endpoint = new ClipEndpoint.Directory(destDir);
        var files = new ClipType.Files
        {
            Identity = new List<string> { sourceDir }
        };
        using var testCtx = new TestClipContext();

        endpoint.Write(testCtx.Context, files);

        Assert.Equal(0, testCtx.Status);
        Assert.True(Directory.Exists(destDir));

        string destRoot = Path.Combine(destDir, "source_tree", "root.txt");
        string destNested = Path.Combine(destDir, "source_tree", "subdir", "nested.txt");

        Assert.True(File.Exists(destRoot));
        Assert.True(File.Exists(destNested));
        Assert.Equal("root content", File.ReadAllText(destRoot));
        Assert.Equal("nested content", File.ReadAllText(destNested));
    }

    [Fact]
    public void UnsupportedCombinations_SetStatusAndError()
    {
        var endpoint = new ClipEndpoint.Directory(_tempDirectory);

        // Write unsupported types
        {
            using var ctx = new TestClipContext();
            endpoint.Write(ctx.Context, new ClipType.Text { Identity = "hello" });
            Assert.NotEqual(0, ctx.Status);
            Assert.Contains("directory", ctx.ErrorText, StringComparison.OrdinalIgnoreCase);
        }

        {
            using var ctx = new TestClipContext();
            endpoint.Write(ctx.Context, new ClipType.Html { Identity = "<p>hello</p>" });
            Assert.NotEqual(0, ctx.Status);
            Assert.Contains("directory", ctx.ErrorText, StringComparison.OrdinalIgnoreCase);
        }

        {
            using var ctx = new TestClipContext();
            endpoint.Write(ctx.Context, new ClipType.Image());
            Assert.NotEqual(0, ctx.Status);
            Assert.Contains("directory", ctx.ErrorText, StringComparison.OrdinalIgnoreCase);
        }

        // Read unsupported types
        {
            using var ctx = new TestClipContext();
            endpoint.Read(ctx.Context, new ClipType.Text());
            Assert.NotEqual(0, ctx.Status);
            Assert.Contains("directory", ctx.ErrorText, StringComparison.OrdinalIgnoreCase);
        }

        {
            using var ctx = new TestClipContext();
            endpoint.Read(ctx.Context, new ClipType.Html());
            Assert.NotEqual(0, ctx.Status);
            Assert.Contains("directory", ctx.ErrorText, StringComparison.OrdinalIgnoreCase);
        }

        {
            using var ctx = new TestClipContext();
            endpoint.Read(ctx.Context, new ClipType.Image());
            Assert.NotEqual(0, ctx.Status);
            Assert.Contains("directory", ctx.ErrorText, StringComparison.OrdinalIgnoreCase);
        }
    }
}
