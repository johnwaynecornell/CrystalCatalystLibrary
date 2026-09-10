using System;
using System.Collections.Generic;
using System.IO;
using ClipFlow.Format;
using Xunit;

namespace ClipFlow.Tests;

public class ClipUtilityWindowTests
{
    private class TrackingEndpoint : ClipEndpoint
    {
        public int ReadCount { get; private set; }
        public int WriteCount { get; private set; }
        public string? ReadIdentityToSet { get; set; }
        public string? WrittenIdentity { get; private set; }

        public override void Read(ClipContext context, ClipType type)
        {
            ReadCount++;
            if (type is ClipType.Text text && ReadIdentityToSet != null)
            {
                text.Identity = ReadIdentityToSet;
            }
        }

        public override void Write(ClipContext context, ClipType type)
        {
            WriteCount++;
            if (type is ClipType.Text text)
            {
                WrittenIdentity = text.Identity;
            }
        }
    }

    [Fact]
    public void Copy_Direct_ThenPaste_Direct_TextRoundtrips()
    {
        string expected = $"direct_text_test_{Guid.NewGuid()}";
        using var copyCtx = new TestClipContext();
        var copyType = new ClipType.Text { Identity = expected };

        ClipUtilityWindow.Copy(copyCtx.Context, copyType);

        Assert.Equal(0, copyCtx.Status);

        using var pasteCtx = new TestClipContext();
        var pasteType = new ClipType.Text();

        ClipUtilityWindow.Paste(pasteCtx.Context, pasteType);

        Assert.Equal(0, pasteCtx.Status);
        Assert.Equal(expected, pasteType.Identity);
    }

    [Fact]
    public void Copy_Direct_ThenPaste_Direct_FilesRoundtrips()
    {
        string tempFile = Path.GetFullPath("direct_files_test.tmp");
        File.WriteAllText(tempFile, "sample");

        try
        {
            using var copyCtx = new TestClipContext();
            var copyFiles = new ClipType.Files { Identity = new List<string> { tempFile } };

            ClipUtilityWindow.Copy(copyCtx.Context, copyFiles);

            Assert.Equal(0, copyCtx.Status);

            using var pasteCtx = new TestClipContext();
            var pasteFiles = new ClipType.Files();

            ClipUtilityWindow.Paste(pasteCtx.Context, pasteFiles);

            Assert.Equal(0, pasteCtx.Status);
            Assert.NotNull(pasteFiles.Identity);
            Assert.Contains(tempFile, pasteFiles.Identity);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void Copy_WithEndpoint_ReadsEndpointExactlyOnce()
    {
        string text = $"endpoint_read_once_{Guid.NewGuid()}";
        var endpoint = new TrackingEndpoint { ReadIdentityToSet = text };
        var type = new ClipType.Text();
        using var copyCtx = new TestClipContext();

        ClipUtilityWindow.Copy(copyCtx.Context, type, endpoint);

        Assert.Equal(0, copyCtx.Status);
        Assert.Equal(1, endpoint.ReadCount);
        Assert.Equal(text, type.Identity);

        // Verify it was copied to clipboard and paste retrieves it
        using var pasteCtx = new TestClipContext();
        var pasteType = new ClipType.Text();
        ClipUtilityWindow.Paste(pasteCtx.Context, pasteType);
        Assert.Equal(0, pasteCtx.Status);
        Assert.Equal(text, pasteType.Identity);
    }

    [Fact]
    public void Paste_WithEndpoint_WritesOnlyAfterSuccessfulReceive()
    {
        string text = $"endpoint_write_test_{Guid.NewGuid()}";
        using var copyCtx = new TestClipContext();
        var copyType = new ClipType.Text { Identity = text };
        ClipUtilityWindow.Copy(copyCtx.Context, copyType);
        Assert.Equal(0, copyCtx.Status);

        var endpoint = new TrackingEndpoint();
        var pasteType = new ClipType.Text();
        using var pasteCtx = new TestClipContext();

        ClipUtilityWindow.Paste(pasteCtx.Context, pasteType, endpoint);

        Assert.Equal(0, pasteCtx.Status);
        Assert.Equal(1, endpoint.WriteCount);
        Assert.Equal(text, endpoint.WrittenIdentity);
        Assert.Equal(text, pasteType.Identity);
    }
}
