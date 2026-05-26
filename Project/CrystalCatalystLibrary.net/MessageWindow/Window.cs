using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using CrystalCatalystLibrary.net;
using JWCEssentials;
using SkiaSharp;

namespace MessageWindow;

public class Window
{
    private CrystalWindow wnd;
    private SKBitmap? bitmap;
    private SKCanvas? canvas;
    private SKImageInfo imageInfo;
    private byte[] inputData;
    private List<string> buttonConfigs;
    private string? outputImagePath;

    private List<List<StyledChar>> lines = new();
    private List<StyledChar> currentLine = new();
    
    private SKColor currentFg = SKColors.White;
    private SKColor currentBg = SKColors.Transparent;
    private Dictionary<string, bool> switches = new();
    
    private bool _blinkState = true;
    private long _lastBlinkToggle = 0;
    private const long BlinkIntervalMs = 500;

    private struct StyledChar
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

    private class Button
    {
        public string Text = "";
        public string Action = "";
        public SKRect Rect;
        public bool IsHovered;
    }

    private List<Button> buttons = new();

    public Window(byte[] inputData, List<string> buttonConfigs, string? outputImagePath)
    {
        this.inputData = inputData;
        this.buttonConfigs = buttonConfigs;
        this.outputImagePath = outputImagePath;

        ParseButtons();
        ParseAnsi();

        int width = 800;
        int height = 600;

        wnd = CrystalWindow.Create(width, height, "MessageWindow");
        wnd.ApplicationRetain();

        wnd.OnDraw = OnDraw;
        wnd.OnResize = OnResize;
        wnd.OnClose = OnClose;
        wnd.OnIdle = OnIdle;
        wnd.OnMouseDown = OnMouseDown;
        wnd.OnMouseMove = OnMouseMove;

        OnResize(wnd, width, height);
    }

    private void ParseButtons()
    {
        foreach (var config in buttonConfigs)
        {
            var parts = config.Split('|');
            if (parts.Length >= 2)
            {
                buttons.Add(new Button { Text = parts[0], Action = parts[1] });
            }
            else
            {
                buttons.Add(new Button { Text = config, Action = config });
            }
        }
    }

    private void ParseAnsi()
    {
        var sniffer = new AnsiEffectSniffer();
        
        sniffer.Char = c =>
        {
            if (c == '\n')
            {
                lines.Add(currentLine);
                currentLine = new List<StyledChar>();
            }
            else if (c == '\r')
            {
                // Ignore or handle
            }
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
        
        sniffer.Reset = () =>
        {
            currentFg = SKColors.White;
            currentBg = SKColors.Transparent;
            switches.Clear();
        };
        sniffer.Switch = (name, enable) => switches[name] = enable;

        sniffer.Process(inputData);
        if (currentLine.Count > 0)
        {
            lines.Add(currentLine);
        }
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
            "default" => is_forground ? SKColors.White : SKColors.Transparent,
            _ => SKColors.White
        };
    }

    public void Run()
    {
        if (outputImagePath != null)
        {
            SaveToImage(outputImagePath);
            return;
        }

        wnd.Show(true);
        Application.Run();
    }

    private void SaveToImage(string path)
    {
        OnDraw(wnd);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(path);
        data.SaveTo(stream);
        Console.WriteLine($"Saved output to {path}");
    }

    private void OnClose(CrystalWindow windowHandle)
    {
        wnd.ApplicationRelease();
    }

    private void OnResize(CrystalWindow windowHandle, int width, int height)
    {
        canvas?.Dispose();
        bitmap?.Dispose();

        imageInfo = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        bitmap = new SKBitmap(imageInfo);
        canvas = new SKCanvas(bitmap);
        
        LayoutButtons(width, height);
    }

    private void LayoutButtons(int width, int height)
    {
        if (buttons.Count == 0) return;

        float buttonWidth = 150;
        float buttonHeight = 40;
        float padding = 20;
        float totalWidth = buttons.Count * buttonWidth + (buttons.Count - 1) * padding;
        float startX = (width - totalWidth) / 2;
        float y = height - buttonHeight - padding;

        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].Rect = new SKRect(startX, y, startX + buttonWidth, y + buttonHeight);
            startX += buttonWidth + padding;
        }
    }

    private void OnIdle(CrystalWindow windowHandle)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (now - _lastBlinkToggle > BlinkIntervalMs)
        {
            _blinkState = !_blinkState;
            _lastBlinkToggle = now;
            wnd.QueueRedraw();
        }
    }

    private void OnMouseMove(CrystalWindow windowHandle, int x, int y)
    {
        bool changed = false;
        foreach (var btn in buttons)
        {
            bool hovered = btn.Rect.Contains(x, y);
            if (btn.IsHovered != hovered)
            {
                btn.IsHovered = hovered;
                changed = true;
            }
        }
        if (changed) wnd.QueueRedraw();
    }

    private void OnMouseDown(CrystalWindow windowHandle, int button, int x, int y)
    {
        if ((CrystalMouseButton)button == CrystalMouseButton.Left)
        {
            foreach (var btn in buttons)
            {
                if (btn.Rect.Contains(x, y))
                {
                    Console.WriteLine(btn.Action);
                    wnd.Close();
                    return;
                }
            }
        }
    }

    private void OnDraw(CrystalWindow windowHandle)
    {
        if (canvas == null || bitmap == null) return;
        
        canvas.Clear(new SKColor(32, 36, 48, 255));

        float x = 20;
        float y = 40;
        float fontSize = 20;

        using SKPaint textPaint = new SKPaint { IsAntialias = true };
        using SKTypeface typeface = SKTypeface.FromFamilyName("Monospace");
        using SKFont font = new SKFont(typeface, fontSize);

        foreach (var line in lines)
        {
            float curX = x;
            foreach (var sc in line)
            {
                if (sc.Blink && !_blinkState)
                {
                    // Skip drawing char, but still advance X
                }
                else
                {
                    SKColor fg = sc.Reverse ? sc.Bg : sc.Fg;
                    SKColor bg = sc.Reverse ? sc.Fg : sc.Bg;

                    if (fg == SKColors.Transparent) fg = SKColors.White; // Default if reversed transparent

                    textPaint.Color = fg;
                    font.Embolden = sc.Bold;
                    font.SkewX = sc.Italic ? -0.25f : 0;
                    
                    float charWidth = font.MeasureText(sc.Char.ToString());

                    if (bg != SKColors.Transparent)
                    {
                        using SKPaint bgPaint = new SKPaint { Color = bg };
                        canvas.DrawRect(curX, y - fontSize, charWidth, fontSize + 5, bgPaint);
                    }

                    canvas.DrawText(sc.Char.ToString(), curX, y, font, textPaint);
                    
                    if (sc.Underline)
                    {
                        canvas.DrawLine(curX, y + 2, curX + charWidth, y + 2, textPaint);
                    }
                    if (sc.Overline)
                    {
                        canvas.DrawLine(curX, y - fontSize, curX + charWidth, y - fontSize, textPaint);
                    }
                    if (sc.Crossed)
                    {
                        canvas.DrawLine(curX, y - fontSize / 2, curX + charWidth, y - fontSize / 2, textPaint);
                    }
                }
                curX += font.MeasureText(sc.Char.ToString());
            }
            y += fontSize + 5;
        }

        DrawButtons();
        canvas.Flush();

        IntPtr pixels = bitmap.GetPixels();
        wnd.PresentImage("RGBA:int8", pixels, (IntPtr)bitmap.ByteCount, imageInfo.Width, imageInfo.Height);
    }

    private void DrawButtons()
    {
        if (canvas == null) return;
        
        using SKPaint btnPaint = new SKPaint { IsAntialias = true };
        using SKPaint textPaint = new SKPaint { IsAntialias = true, Color = SKColors.White };
        using SKFont btnFont = new SKFont(SKTypeface.Default, 18);

        foreach (var btn in buttons)
        {
            btnPaint.Color = btn.IsHovered ? new SKColor(100, 200, 255) : new SKColor(60, 120, 180);
            canvas.DrawRoundRect(btn.Rect, 5, 5, btnPaint);
            
            float textWidth = btnFont.MeasureText(btn.Text);
            canvas.DrawText(btn.Text, btn.Rect.MidX - textWidth / 2, btn.Rect.MidY + 6, btnFont, textPaint);
        }
    }
}
