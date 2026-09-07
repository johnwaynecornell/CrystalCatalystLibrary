using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ClipFlow.Format;
using Xunit;

namespace ClipFlow.Tests;

public class ClipTypeHeaderReflectionTests
{
    [Fact]
    public void AllConcreteClipTypeSubclasses_HaveUsablePublicStaticClipTypeHeader()
    {
        var clipTypeBase = typeof(ClipType);
        var concreteSubclasses = clipTypeBase.Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(clipTypeBase) && !t.IsAbstract)
            .ToList();

        Assert.NotEmpty(concreteSubclasses);

        var declaredCommands = new Dictionary<string, ClipTypeHeader>(StringComparer.OrdinalIgnoreCase);

        foreach (var subType in concreteSubclasses)
        {
            var field = subType.GetField("ClipTypeHeader", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(field);

            var headerObj = field.GetValue(null);
            Assert.NotNull(headerObj);
            Assert.IsType<ClipTypeHeader>(headerObj);

            var header = (ClipTypeHeader)headerObj;
            Assert.False(string.IsNullOrWhiteSpace(header.CommandName), $"CommandName is empty for {subType.FullName}");
            Assert.NotNull(header.Formats);
            Assert.NotEmpty(header.Formats);

            foreach (var format in header.Formats)
            {
                Assert.False(string.IsNullOrWhiteSpace(format), $"Format is empty in {header.CommandName}");
            }

            declaredCommands[header.CommandName] = header;
        }

        // Semantic declarations must at minimum include: text, html, image, files
        Assert.True(declaredCommands.ContainsKey("text"), "Missing 'text' ClipType declaration");
        Assert.True(declaredCommands.ContainsKey("html"), "Missing 'html' ClipType declaration");
        Assert.True(declaredCommands.ContainsKey("image"), "Missing 'image' ClipType declaration");
        Assert.True(declaredCommands.ContainsKey("files"), "Missing 'files' ClipType declaration");

        // Format mapping checks
        var imageHeader = declaredCommands["image"];
        Assert.Contains("image/png", imageHeader.Formats);
        Assert.Contains("image/bmp", imageHeader.Formats);

        var filesHeader = declaredCommands["files"];
        Assert.Contains("text/file-uri", filesHeader.Formats);
    }

    [Fact]
    public void ClipTypeHeader_FormatOrder_DefinesPreferenceOrder()
    {
        // Text format preference
        Assert.Equal("text/plain", ClipType.Text.ClipTypeHeader.Formats[0]);
        Assert.Contains("UTF8_STRING", ClipType.Text.ClipTypeHeader.Formats);
        Assert.Contains("STRING", ClipType.Text.ClipTypeHeader.Formats);
        Assert.Contains("TEXT", ClipType.Text.ClipTypeHeader.Formats);

        // Html format preference
        Assert.Equal("text/html", ClipType.Html.ClipTypeHeader.Formats[0]);
        Assert.Contains("HTML", ClipType.Html.ClipTypeHeader.Formats);
        Assert.Contains("HTML_TEXT", ClipType.Html.ClipTypeHeader.Formats);

        // Image format preference
        Assert.Equal("image/png", ClipType.Image.ClipTypeHeader.Formats[0]);
        Assert.Equal("image/bmp", ClipType.Image.ClipTypeHeader.Formats[1]);

        // Files format preference
        Assert.Equal("text/file-uri", ClipType.Files.ClipTypeHeader.Formats[0]);
    }
}
