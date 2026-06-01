using System;
using System.Collections.Generic;
using System.Text;
using SkiaSharp;
using JWCEssentials;
using CrystalCatalystLibrary.net;
using System.Runtime.InteropServices;

namespace CrystalSkia.net;

public class AnsiSkiaRenderer
{
    public float FontSize { get; set; } = 20;
    public string FontFamily { get; set; } = "Monospace";
    public SKColor DefaultFg { get; set; } = SKColors.White;
    public SKColor DefaultBg { get; set; } = SKColors.Transparent;

    public struct StyledChar
    {
        public char Char;
        public SKColor Fg;
        public SKColor Bg;
        public bool Bold;
        public bool Italic;
        public bool Underline;
        public bool Blink;
        public bool Reverse;
        public bool Crossed;
        public bool Overline;
    }

    public class ParsedContent
    {
        public List<List<StyledChar>> Lines { get; } = new();
        public SKSize PixelSize { get; internal set; }
    }

    public ParsedContent Parse(string str)
    {
        return Parse(Encoding.UTF8.GetBytes(str));
    }
    
    public ParsedContent Parse(byte[] data)
    {
        var content = new ParsedContent();
        var currentLine = new List<StyledChar>();
        var currentFg = DefaultFg;
        var currentBg = DefaultBg;
        var switches = new Dictionary<string, bool>();

        var sniffer = new AnsiEffectSniffer();
        sniffer.Char = c =>
        {
            if (c == '\n')
            {
                content.Lines.Add(currentLine);
                currentLine = new List<StyledChar>();
            }
            else if (c == '\r') { }
            else
            {
                currentLine.Add(new StyledChar
                {
                    Char = c,
                    Fg = currentFg,
                    Bg = currentBg,
                    Bold = switches.GetValueOrDefault("bold"),
                    Italic = switches.GetValueOrDefault("italic"),
                    Underline = switches.GetValueOrDefault("underline"),
                    Blink = switches.GetValueOrDefault("blink"),
                    Reverse = switches.GetValueOrDefault("reverse"),
                    Crossed = switches.GetValueOrDefault("crossed"),
                    Overline = switches.GetValueOrDefault("overline")
                });
            }
        };

        sniffer.Fg = (color, brightness) => currentFg = MapColor(true, color, brightness);
        sniffer.Bg = (color, brightness) => currentBg = MapColor(false, color, brightness);
        sniffer.Reset = () => { currentFg = DefaultFg; currentBg = DefaultBg; switches.Clear(); };
        sniffer.Switch = (name, enable) => switches[name] = enable;

        sniffer.Process(data);
        if (currentLine.Count > 0) content.Lines.Add(currentLine);

        content.PixelSize = Measure(content);
        return content;
    }

    private SKColor MapColor(bool is_forground, string name, Brightness brightness)
    {
        return name.ToLower() switch
        {
            "black" => brightness == Brightness.Normal ? SKColors.Black : SKColors.DarkGray,
            "red" => brightness == Brightness.Normal ? SKColors.Red : SKColors.LightCoral,
            "green" => brightness == Brightness.Normal ? SKColors.Green : SKColors.LightGreen,
            "yellow" => brightness == Brightness.Normal ? SKColors.Yellow : SKColors.LightYellow,
            "blue" => brightness == Brightness.Normal ? SKColors.Blue : SKColors.LightBlue,
            "magenta" => brightness == Brightness.Normal ? SKColors.Magenta : SKColors.Pink,
            "cyan" => brightness == Brightness.Normal ? SKColors.Cyan : SKColors.LightCyan,
            "white" => brightness == Brightness.Normal ? SKColors.White : SKColors.WhiteSmoke,
            "default" => is_forground ? DefaultFg : DefaultBg,
            _ => DefaultFg
        };
    }

    private SKSize Measure(ParsedContent content)
    {
        using var typeface = SKTypeface.FromFamilyName(FontFamily);
        using var font = new SKFont(typeface, FontSize);

        float maxWidth = 0;
        float height = 0;
        float lineSpacing = 5;

        foreach (var line in content.Lines)
        {
            float lineWidth = 0;
            foreach (var sc in line)
            {
                font.Embolden = sc.Bold;
                lineWidth += font.MeasureText(sc.Char.ToString());
            }
            maxWidth = Math.Max(maxWidth, lineWidth);
            height += FontSize + lineSpacing;
        }

        return new SKSize(maxWidth + 40, height + 40); // Add padding (20 on each side)
    }

    public void Render(ParsedContent content, SKCanvas canvas, bool blinkState)
    {
        float x = 20;
        float y = 40;
        float lineSpacing = 5;

        using var paint = new SKPaint { IsAntialias = true };
        using var typeface = SKTypeface.FromFamilyName(FontFamily);
        using var font = new SKFont(typeface, FontSize);

        foreach (var line in content.Lines)
        {
            float curX = x;
            foreach (var sc in line)
            {
                if (sc.Blink && !blinkState) { }
                else
                {
                    SKColor fg = sc.Reverse ? sc.Bg : sc.Fg;
                    SKColor bg = sc.Reverse ? sc.Fg : sc.Bg;
                    if (fg == SKColors.Transparent) fg = DefaultFg;

                    paint.Color = fg;
                    font.Embolden = sc.Bold;
                    font.SkewX = sc.Italic ? -0.25f : 0;

                    float charWidth = font.MeasureText(sc.Char.ToString());

                    if (bg != SKColors.Transparent)
                    {
                        using var bgPaint = new SKPaint { Color = bg };
                        canvas.DrawRect(curX, y - FontSize, charWidth, FontSize + lineSpacing, bgPaint);
                    }

                    canvas.DrawText(sc.Char.ToString(), curX, y, font, paint);

                    if (sc.Underline) canvas.DrawLine(curX, y + 2, curX + charWidth, y + 2, paint);
                    if (sc.Overline) canvas.DrawLine(curX, y - FontSize, curX + charWidth, y - FontSize, paint);
                    if (sc.Crossed) canvas.DrawLine(curX, y - FontSize / 2, curX + charWidth, y - FontSize / 2, paint);
                }
                curX += font.MeasureText(sc.Char.ToString());
            }
            y += FontSize + lineSpacing;
        }
    }

    // Static delegate to prevent GC collection of the callback
    private static readonly PixData.Pix_data_free SafeFreeDelegate = FreeUnmanagedPixels;

    private static bool FreeUnmanagedPixels(IntPtr pixdata)
    {
        if (pixdata != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(pixdata);
        }
        return true;
    }

    public PixData RenderPix(ParsedContent content, bool blinkState, Action<SKBitmap, SKCanvas>? onCanvas = null, Action<SKBitmap, SKCanvas>? postProc = null, string pixFormatDest = "bgra:int8", bool strict = false)
    {
        var width = (int)Math.Ceiling(content.PixelSize.Width);
        var height = (int)Math.Ceiling(content.PixelSize.Height);

        if (width <= 0) width = 1;
        if (height <= 0) height = 1;

        string destFormat = PixFormats.Parse(pixFormatDest).PixFormat;

        if (strict && !SkiaPixFormatMap.IsRenderTargetSupported(destFormat))
        {
            throw new NotSupportedException($"Pixel format '{destFormat}' is not supported as a Skia render target and strict mode is enabled.");
        }

        string renderPixFormat = SkiaPixFormatMap.IsRenderTargetSupported(destFormat)
            ? destFormat
            : SkiaPixFormatMap.GetFallbackRenderTargetFormat(destFormat);

        SKImageInfo info = SkiaPixFormatMap.GetImageInfo(renderPixFormat, width, height);
        int stride = PixFormats.GetBytesPerPixel(renderPixFormat) * width;
        int byteCount = stride * height;
        IntPtr unmanagedPixels = Marshal.AllocHGlobal(byteCount);

        using var bitmap = new SKBitmap();
        bitmap.InstallPixels(info, unmanagedPixels, stride);
        
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(DefaultBg);
        
        onCanvas?.Invoke(bitmap, canvas);
        
        Render(content, canvas, blinkState);
        
        if (postProc != null)
        {
            canvas.Flush();
            postProc(bitmap, canvas);
        }
        
        canvas.Flush();
        
        var pixData = new PixData();
        pixData.width = width;
        pixData.height = height;
        pixData.pix_format = renderPixFormat;

        pixData.pix_data = unmanagedPixels;
        pixData.pix_data_length = (IntPtr)byteCount;
        pixData.pix_data_free = SafeFreeDelegate;

        if (renderPixFormat != destFormat)
        {
            var proxy = Pixels.ConvertPixelsPix(ref pixData, destFormat);
            if (proxy)
            {
                pixData.Dispose();
                return proxy;
            }
        }

        return pixData;
    }
}
