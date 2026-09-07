using System;
using SkiaSharp;
using Xunit;
using Xunit.Abstractions;

namespace ClipFlow.Tests;

public class SkiaFormatCapabilityTest
{
    private readonly ITestOutputHelper _output;

    public SkiaFormatCapabilityTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void CheckEncodingFormats()
    {
        using var bitmap = new SKBitmap(4, 4, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var image = SKImage.FromBitmap(bitmap);

        foreach (SKEncodedImageFormat format in Enum.GetValues(typeof(SKEncodedImageFormat)))
        {
            using var data = image.Encode(format, 100);
            _output.WriteLine($"Format {format}: {(data != null ? "Supported (bytes=" + data.Size + ")" : "NOT SUPPORTED")}");
        }
    }
}
