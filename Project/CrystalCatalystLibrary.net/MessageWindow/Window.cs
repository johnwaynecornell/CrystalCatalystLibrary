using System;
using System.Collections.Generic;
using CrystalCatalystLibrary.net;
using SkiaSharp;
using CrystalSkia.net;

namespace MessageWindow;

public class Window
{
    private CrystalWindow wnd;
    private SKBitmap? bitmap;
    private SKCanvas? canvas;
    private SKImageInfo imageInfo;
    private List<string> buttonConfigs;
    private string? outputImagePath;

    private AnsiSkiaRenderer renderer = new();
    private AnsiSkiaRenderer.ParsedContent content;
    
    private bool _blinkState = true;
    private long _lastBlinkToggle = 0;
    private const long BlinkIntervalMs = 500;

    private class Button
    {
        public string Text = "";
        public string Action = "";
        public SKRect Rect;
        public bool IsHovered;
    }

    private List<Button> buttons = new();

    public Window(AnsiSkiaRenderer.ParsedContent content, List<string> buttonConfigs, string? outputImagePath)
    {
        this.content = content;
        this.buttonConfigs = buttonConfigs;
        this.outputImagePath = outputImagePath;

        ParseButtons();

        int width = (int)Math.Ceiling(content.PixelSize.Width);
        int height = (int)Math.Ceiling(content.PixelSize.Height);

        // Adjust width if buttons need more space
        float buttonWidth = 150;
        float padding = 20;
        float totalButtonsWidth = buttons.Count * buttonWidth + (buttons.Count + 1) * padding;
        if (totalButtonsWidth > width) width = (int)Math.Ceiling(totalButtonsWidth);

        // Adjust height for buttons
        if (buttons.Count > 0)
        {
            height += 60; // Extra space for buttons (button height 40 + padding 20)
        }

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
            buttons.Add(new Button { 
                Text = parts[0], 
                Action = parts.Length >= 2 ? parts[1] : parts[0] 
            });
        }
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
        using var stream = System.IO.File.OpenWrite(path);
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

        renderer.Render(content, canvas, _blinkState);

        DrawButtons();
        canvas.Flush();

        IntPtr pixels = bitmap.GetPixels();
        wnd.PresentImage("rgba:int8", pixels, (IntPtr)bitmap.ByteCount, imageInfo.Width, imageInfo.Height);
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
