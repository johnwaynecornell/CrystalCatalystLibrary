using System;
using System.Collections.Generic;
using System.Text;
using SkiaSharp;
using JWCEssentials;
using CrystalCatalystLibrary.net;
using System.Runtime.InteropServices;

namespace CrystalSkia.net;

/// <summary>
/// Renders ANSI-escaped text to Skia canvases or <see cref="PixData"/>.
/// </summary>
public class AnsiSkiaRenderer
{
    /// <summary>Font size in pixels.</summary>
    public float FontSize { get; set; } = 20;
    /// <summary>Font family name.</summary>
    public string FontFamily { get; set; } = "DejaVu Sans Mono";
    /// <summary>Default foreground color.</summary>
    public SKColor DefaultFg { get; set; } = SKColors.White;
    /// <summary>Default background color.</summary>
    public SKColor DefaultBg { get; set; } = SKColors.Transparent;

    public Dictionary<String, SKTypeface> TypefaceCache = new();

    public SKTypeface GetTypeface(string family)
    {
        if (!TypefaceCache.TryGetValue(family, out var typeface))
        {
            typeface = SKTypeface.FromFamilyName(family);
            TypefaceCache[family] = typeface;
        }
        return typeface;
    }
    
    public bool HasFontFamilyAndGlyph(String family, String glyph)
    {
        using (SKTypeface typeface = GetTypeface(family))
        {
            // If the names do not match, the exact font family wasn't found
            // and SkiaSharp used a fallback instead.
            bool isAvailable = typeface.FamilyName.Equals(family, StringComparison.OrdinalIgnoreCase);
    
            if (isAvailable)
            {
                using var font = new SKFont(typeface, 16f);

                // Check if the font family has the glyph for a specific string or codepoint
                return font.ContainsGlyphs(glyph); 
            }
        }

        return false;
    }

    private static readonly string[] FallbackFamilies =
    {
        // Good broad text/math Unicode fallbacks
        "DejaVu Sans Mono",
        "Noto Sans Mono",
        "Noto Sans",
        "Noto Serif",
        "Noto Sans Symbols",
        "Noto Sans Symbols 2",
        "STIX Two Math",
        "Cambria Math",

        // Emoji fallbacks last
        "Noto Color Emoji",
        "Segoe UI Emoji",
        "Apple Color Emoji",
        "DejaVu Sans",
    };
    
    public Dictionary<string, string> FamilyCache = new Dictionary<string, string>();

    public string FontFamilyForGlyph(string glyph)
    {
        if (!FamilyCache.TryGetValue(glyph, out string family))
        {
            family = FontFamily;

            if (glyph == " " || HasFontFamilyAndGlyph(FontFamily, glyph))
            {
                family = FontFamily;
            }
            else
            {
                foreach (string fam in FallbackFamilies)
                {
                    if (HasFontFamilyAndGlyph(fam, glyph))
                    {
                        family = fam;
                        break;
                    }
                }
            }

            FamilyCache[glyph] = family;
        }

        return family;
    }
    
    private readonly Dictionary<(SKTypeface face, float size, bool bold, bool italic), SKFont> FontCache = new();
    
    public SKFont GetFont(SKTypeface face, float size, bool bold, bool italic)
    {
        if (!FontCache.TryGetValue((face, size, bold, italic), out var font))
        {
            font = new SKFont(face, size);// { Bold = bold, Oblique = italic };
            font.Embolden = bold;
            font.SkewX = italic ? -0.25f : 0;

            FontCache[(face, size, bold, italic)] = font;
        }

        return font;
    }
    
    /// <summary>
    /// Represents a single character with its ANSI styling.
    /// </summary>
    public struct StyledChar
    {
        public string Glyph;
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

    /// <summary>
    /// Parsed representation of ANSI-encoded content.
    /// </summary>
    public class ParsedContent
    {
        /// <summary>Rows of styled characters.</summary>
        public List<List<StyledChar>> Lines { get; } = new();
        /// <summary>Total size of the rendered content in pixels.</summary>
        public SKSize PixelSize { get; internal set; }
    }

    /// <summary>
    /// Parses a string containing ANSI escape sequences.
    /// </summary>
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
        sniffer.Glyph = c =>
        {
            if (c == "\n")
            {
                content.Lines.Add(currentLine);
                currentLine = new List<StyledChar>();
            }
            else if (c == "\r") { }
            else
            {
                currentLine.Add(new StyledChar
                {
                    Glyph = c,
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
        float maxWidth = 0;
        float height = 0;
        float lineSpacing = 5;

        foreach (var line in content.Lines)
        {
            float lineWidth = 0;
            foreach (var sc in line)
            {
                var typeface = GetTypeface(FontFamilyForGlyph(sc.Glyph));
                var font = GetFont(typeface, FontSize, sc.Bold, sc.Italic);
                
                lineWidth += font.MeasureText(sc.Glyph.ToString());
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

        foreach (var line in content.Lines)
        {
            float curX = x;
            foreach (var sc in line)
            {
                var typeface = GetTypeface(FontFamilyForGlyph(sc.Glyph));
                var font = GetFont(typeface, FontSize, sc.Bold, sc.Italic);
                
                if (sc.Blink && !blinkState) { }
                else
                {
                    SKColor fg, bg;

                    fg = sc.Reverse ? sc.Bg : sc.Fg;
                    bg = sc.Reverse ? sc.Fg : sc.Bg;
                    
                    if (fg == SKColors.Transparent)
                        fg = new SKColor((byte)( 0xFF - bg.Red), (byte)(0xFF - bg.Green), (byte)(0xFF - bg.Blue), bg.Alpha);
                    
                    paint.Color = fg;
                    
                    float charWidth = font.MeasureText(sc.Glyph.ToString());

                    if (bg != SKColors.Transparent)
                    {
                        using var bgPaint = new SKPaint { Color = bg };
                        canvas.DrawRect(curX, y - FontSize, charWidth, FontSize + lineSpacing, bgPaint);
                    }

                    canvas.DrawText(sc.Glyph.ToString(), curX, y, font, paint);

                    if (sc.Underline) canvas.DrawLine(curX, y + 2, curX + charWidth, y + 2, paint);
                    if (sc.Overline) canvas.DrawLine(curX, y - FontSize, curX + charWidth, y - FontSize, paint);
                    if (sc.Crossed) canvas.DrawLine(curX, y - FontSize / 2, curX + charWidth, y - FontSize / 2, paint);
                }
                curX += font.MeasureText(sc.Glyph.ToString());
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

    /// <summary>
    /// Renders parsed content into a <see cref="PixData"/> buffer.
    /// </summary>
    public PixData RenderPix(ParsedContent content, bool blinkState, Action<SKBitmap, SKCanvas>? onCanvas = null, Action<SKBitmap, SKCanvas>? postProc = null, string? pixFormatDest = null, bool strict = false, SKAlphaType? alphaType = null)
    {
        pixFormatDest ??= "bgra:int8";
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

        SKImageInfo info = SkiaPixFormatMap.GetImageInfo(renderPixFormat, width, height, alphaType);
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
