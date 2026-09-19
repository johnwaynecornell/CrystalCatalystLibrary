using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClipFlow.Format;
using FluentCommandLine;
using Xunit;

namespace ClipFlow.Tests;

[Collection("ClipboardTests")]
public class PathEndpointTests : IDisposable
{
    private readonly string _tempDirectory;

    public PathEndpointTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "clipflow-path-test-" + Guid.NewGuid().ToString("N"));
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
    public void Read_Files_ExactFile_ReturnsSingleNormalizedFilePath()
    {
        string filePath = Path.Combine(_tempDirectory, "single_file.txt");
        File.WriteAllText(filePath, "test exact file content");

        var endpoint = ClipEndpoint.path(filePath);
        var files = new ClipType.Files();
        using var testCtx = new TestClipContext();

        endpoint.Read(testCtx.Context, files);

        Assert.Equal(0, testCtx.Status);
        Assert.NotNull(files.Identity);
        Assert.Single(files.Identity);
        Assert.Equal(Path.GetFullPath(filePath), files.Identity[0]);
    }

    [Fact]
    public void Read_Files_ExactDirectory_ReturnsSingleDirectoryEntry()
    {
        string subDir = Path.Combine(_tempDirectory, "target_sub");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "inner1.txt"), "inner 1");
        File.WriteAllText(Path.Combine(subDir, "inner2.txt"), "inner 2");

        var endpoint = ClipEndpoint.path(subDir);
        var files = new ClipType.Files();
        using var testCtx = new TestClipContext();

        endpoint.Read(testCtx.Context, files);

        Assert.Equal(0, testCtx.Status);
        Assert.NotNull(files.Identity);
        // Desired semantics: copy that one directory as a single filesystem entry, not its expanded children
        Assert.Single(files.Identity);
        Assert.Equal(Path.GetFullPath(subDir), files.Identity[0]);
    }

    [Fact]
    public void Read_Files_DirectoryWildcardAsterisk_ReturnsFilesAndDirectories()
    {
        string file1 = Path.Combine(_tempDirectory, "child_file.txt");
        string subDir = Path.Combine(_tempDirectory, "child_dir");
        File.WriteAllText(file1, "file content");
        Directory.CreateDirectory(subDir);

        string wildcardPath = Path.Combine(_tempDirectory, "*");
        var endpoint = ClipEndpoint.path(wildcardPath);
        var files = new ClipType.Files();
        using var testCtx = new TestClipContext();

        endpoint.Read(testCtx.Context, files);

        Assert.Equal(0, testCtx.Status);
        Assert.NotNull(files.Identity);
        Assert.Equal(2, files.Identity.Count);

        var expected = new[] { file1, subDir }.Select(Path.GetFullPath).OrderBy(x => x).ToList();
        var actual = files.Identity.OrderBy(x => x).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Read_Files_PrefixWildcard_ReturnsMatchingFilesAndDirectories()
    {
        string matchFile = Path.Combine(_tempDirectory, "foo_item.txt");
        string matchDir = Path.Combine(_tempDirectory, "foo_folder");
        string nonMatchFile = Path.Combine(_tempDirectory, "bar_item.txt");
        string nonMatchDir = Path.Combine(_tempDirectory, "bar_folder");

        File.WriteAllText(matchFile, "foo file");
        Directory.CreateDirectory(matchDir);
        File.WriteAllText(nonMatchFile, "bar file");
        Directory.CreateDirectory(nonMatchDir);

        string wildcardPath = Path.Combine(_tempDirectory, "foo*");
        var endpoint = ClipEndpoint.path(wildcardPath);
        var files = new ClipType.Files();
        using var testCtx = new TestClipContext();

        endpoint.Read(testCtx.Context, files);

        Assert.Equal(0, testCtx.Status);
        Assert.NotNull(files.Identity);
        Assert.Equal(2, files.Identity.Count);

        var expected = new[] { matchFile, matchDir }.Select(Path.GetFullPath).OrderBy(x => x).ToList();
        var actual = files.Identity.OrderBy(x => x).ToList();
        Assert.Equal(expected, actual);
        Assert.DoesNotContain(Path.GetFullPath(nonMatchFile), files.Identity);
        Assert.DoesNotContain(Path.GetFullPath(nonMatchDir), files.Identity);
    }

    [Fact]
    public void Read_Files_WildcardNoMatches_SetsCleanValidationErrorAndNonzeroStatus()
    {
        string wildcardPath = Path.Combine(_tempDirectory, "no-match*");
        var endpoint = ClipEndpoint.path(wildcardPath);
        var files = new ClipType.Files();
        using var testCtx = new TestClipContext();

        endpoint.Read(testCtx.Context, files);

        Assert.NotEqual(0, testCtx.Status);
        Assert.Null(files.Identity);
        Assert.Contains($"No filesystem entries matched: {wildcardPath}", testCtx.ErrorText);
    }

    [Fact]
    public void Read_Files_NonExistentPathWithoutWildcard_SetsPathNotFoundAndNonzeroStatus()
    {
        string nonExistentPath = Path.Combine(_tempDirectory, "does_not_exist.txt");
        var endpoint = ClipEndpoint.path(nonExistentPath);
        var files = new ClipType.Files();
        using var testCtx = new TestClipContext();

        endpoint.Read(testCtx.Context, files);

        Assert.NotEqual(0, testCtx.Status);
        Assert.Null(files.Identity);
        Assert.Contains("Path not found", testCtx.ErrorText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Read_Files_NonExistentParentDirectoryWithWildcard_SetsPathNotFoundAndNonzeroStatus()
    {
        string nonExistentPath = Path.Combine(_tempDirectory, "missing_dir", "*.txt");
        var endpoint = ClipEndpoint.path(nonExistentPath);
        var files = new ClipType.Files();
        using var testCtx = new TestClipContext();

        endpoint.Read(testCtx.Context, files);

        Assert.NotEqual(0, testCtx.Status);
        Assert.Null(files.Identity);
        Assert.Contains("Path not found", testCtx.ErrorText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Read_Files_RelativePaths_ReturnsNormalizedFullPaths()
    {
        string originalCwd = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = _tempDirectory;
            string subDir = Path.Combine(_tempDirectory, "mysub");
            Directory.CreateDirectory(subDir);
            string file1 = Path.Combine(subDir, "one.txt");
            string nestedDir = Path.Combine(subDir, "nested");
            File.WriteAllText(file1, "1");
            Directory.CreateDirectory(nestedDir);

            // 1. Relative exact directory
            {
                var endpoint = ClipEndpoint.path("./mysub");
                var files = new ClipType.Files();
                using var testCtx = new TestClipContext();

                endpoint.Read(testCtx.Context, files);

                Assert.Equal(0, testCtx.Status);
                Assert.NotNull(files.Identity);
                Assert.Single(files.Identity);
                Assert.Equal(Path.GetFullPath(subDir), files.Identity[0]);
            }

            // 2. Relative wildcard
            {
                var endpoint = ClipEndpoint.path("./mysub/*");
                var files = new ClipType.Files();
                using var testCtx = new TestClipContext();

                endpoint.Read(testCtx.Context, files);

                Assert.Equal(0, testCtx.Status);
                Assert.NotNull(files.Identity);
                Assert.Equal(2, files.Identity.Count);
                foreach (var path in files.Identity)
                {
                    Assert.True(Path.IsPathRooted(path));
                }
            }
        }
        finally
        {
            Environment.CurrentDirectory = originalCwd;
        }
    }

    [Fact]
    public void Copy_Files_WildcardNoMatch_AbortsBeforeClipboardPersistence()
    {
        // First set known text to clipboard
        const string initialText = "preserved_clipboard_content";
        using var initialCopyCtx = new TestClipContext();
        ClipUtilityWindow.Copy(initialCopyCtx.Context, new ClipType.Text { Identity = initialText });
        Assert.Equal(0, initialCopyCtx.Status);

        // Attempt to copy with a non-matching wildcard path
        string nonMatchingWildcard = Path.Combine(_tempDirectory, "ghost_pattern*");
        using var failedCopyCtx = new TestClipContext();
        var filesType = new ClipType.Files();

        ClipUtilityWindow.Copy(failedCopyCtx.Context, filesType, ClipEndpoint.path(nonMatchingWildcard));

        // Context must report error and nonzero status
        Assert.NotEqual(0, failedCopyCtx.Status);
        Assert.Contains($"No filesystem entries matched: {nonMatchingWildcard}", failedCopyCtx.ErrorText);
        Assert.Null(filesType.Identity);

        // Verify clipboard persistence was not reached and existing clipboard was unmodified
        using var pasteCtx = new TestClipContext();
        var pasteType = new ClipType.Text();
        ClipUtilityWindow.Paste(pasteCtx.Context, pasteType);
        Assert.Equal(0, pasteCtx.Status);
        Assert.Equal(initialText, pasteType.Identity);
    }

    [Fact]
    public void FluentCommandLine_Recognizes_Path_Endpoint()
    {
        string filePath = Path.Combine(_tempDirectory, "cli_test.txt");
        File.WriteAllText(filePath, "cli test");

        var env = new FluentEnvironment();
        env.AddModule<ClipFlow_Fluent>();
        env.ServeTypes = new[] { typeof(ClipCommand) };

        var pathArgs = new List<string> { "copy", "files", "path", filePath };
        int pathIndex = 0;
        var pathResult = env.ParseOne(pathArgs, ref pathIndex);
        Assert.NotNull(pathResult);
        Assert.IsAssignableFrom<ClipCommand>(pathResult.Result);
        Assert.Equal(4, pathIndex);
    }

    [Fact]
    public void Write_Files_CreatesDestinationDirectoryAndCopiesEntries()
    {
        string sourceDir = Path.Combine(_tempDirectory, "source");
        Directory.CreateDirectory(sourceDir);

        string srcFile = Path.Combine(sourceDir, "source.txt");
        const string content = "File content in path write test";
        File.WriteAllText(srcFile, content);

        string destDir = Path.Combine(_tempDirectory, "dest_path_write");
        var endpoint = ClipEndpoint.path(destDir);
        var files = new ClipType.Files
        {
            Identity = new List<string> { srcFile }
        };
        using var testCtx = new TestClipContext();

        endpoint.Write(testCtx.Context, files);

        Assert.Equal(0, testCtx.Status);
        Assert.True(Directory.Exists(destDir));
        string destFile = Path.Combine(destDir, "source.txt");
        Assert.True(File.Exists(destFile));
        Assert.Equal(content, File.ReadAllText(destFile));
    }

    [Fact]
    public void UnsupportedCombinations_SetStatusAndError()
    {
        var endpoint = ClipEndpoint.path(_tempDirectory);

        // Write unsupported types
        {
            using var ctx = new TestClipContext();
            endpoint.Write(ctx.Context, new ClipType.Text { Identity = "hello" });
            Assert.NotEqual(0, ctx.Status);
            Assert.Contains("path", ctx.ErrorText, StringComparison.OrdinalIgnoreCase);
        }

        {
            using var ctx = new TestClipContext();
            endpoint.Write(ctx.Context, new ClipType.Html { Identity = "<p>hello</p>" });
            Assert.NotEqual(0, ctx.Status);
            Assert.Contains("path", ctx.ErrorText, StringComparison.OrdinalIgnoreCase);
        }

        {
            using var ctx = new TestClipContext();
            endpoint.Write(ctx.Context, new ClipType.Image());
            Assert.NotEqual(0, ctx.Status);
            Assert.Contains("path", ctx.ErrorText, StringComparison.OrdinalIgnoreCase);
        }

        // Read unsupported types
        {
            using var ctx = new TestClipContext();
            endpoint.Read(ctx.Context, new ClipType.Text());
            Assert.NotEqual(0, ctx.Status);
            Assert.Contains("path", ctx.ErrorText, StringComparison.OrdinalIgnoreCase);
        }

        {
            using var ctx = new TestClipContext();
            endpoint.Read(ctx.Context, new ClipType.Html());
            Assert.NotEqual(0, ctx.Status);
            Assert.Contains("path", ctx.ErrorText, StringComparison.OrdinalIgnoreCase);
        }

        {
            using var ctx = new TestClipContext();
            endpoint.Read(ctx.Context, new ClipType.Image());
            Assert.NotEqual(0, ctx.Status);
            Assert.Contains("path", ctx.ErrorText, StringComparison.OrdinalIgnoreCase);
        }
    }
}
