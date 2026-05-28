using System.Diagnostics;
using System.Runtime.InteropServices;
using CrystalCatalystLibrary.net;
using SkiaSharp;

namespace TestWindow;

public class Window
{
    CrystalWindow wnd;
    private SKBitmap? bitmap;
    private SKCanvas? canvas;
    private SKImageInfo imageInfo;

    public Window()
    {
        int width = 800;
        int height = 600;

        wnd = CrystalWindow.Create(width, height, "test Window");
        wnd.ApplicationRetain();

        wnd.OnDraw = OnDraw;
        wnd.OnResize = OnResize;
        wnd.OnClose = OnClose;

        wnd.OnMouseDown = OnMouseDown;
        wnd.OnClipboardProvideChosen = OnClipboardProvideChosen;

        OnResize(wnd, width, height);
    }

    public void Run()
    {
        wnd.Show(true);
        Application.Run();
    }

    private void OnClipboardProvideChosen(CrystalWindow windowHandle, DataInterchange data, string format)
    {
        if (format == "text/plain" || format == "TEXT")
        {
            string message = "Hello, World! From the clipboard";
            
            byte[] utf8Bytes = System.Text.Encoding.UTF8.GetBytes(message);
            IntPtr _data = Marshal.AllocHGlobal(utf8Bytes.Length);
            Marshal.Copy(utf8Bytes, 0, _data, utf8Bytes.Length);
            IntPtr _size = (IntPtr)utf8Bytes.Length;
            
            data.SelectionSet(format, _data, _size);
        }
    }

    private void OnMouseDown(CrystalWindow windowHandle, int button, int x, int y)
    {
        CrystalMouseButton b = (CrystalMouseButton)button;
        
        Debug.WriteLine($"Mouse down at ({x}, {y}) with button {b}({button})");

        if (b == CrystalMouseButton.Left)
        {
            DataInterchange dataInterchange = DataInterchange.Create();
            dataInterchange.FormatAdd("text/plain");

            windowHandle.ClipboardCopy(dataInterchange);
        }
    }

    private void OnClose(CrystalWindow windowHandle)
    {
        wnd.ApplicationRelease();
    }

    public int Width { get; set; }
    public int Height { get; set; }

    private void OnResize(CrystalWindow windowHandle, int width, int height)
    {
        Width = width;
        Height = height;
        
        canvas?.Dispose();
        bitmap?.Dispose();

        imageInfo = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        bitmap = new SKBitmap(imageInfo);
        canvas = new SKCanvas(bitmap);
    }

    private void OnDraw(CrystalWindow windowHandle)
    {
        if (canvas == null || bitmap == null) return;
        
        canvas.Clear(new SKColor(32, 36, 48, 255));

        using SKPaint textPaint = new()
        {
            Color = SKColors.White,
            IsAntialias = true,
            TextSize = 72,
            Typeface = SKTypeface.Default
        };

        using SKPaint accentPaint = new()
        {
            Color = new SKColor(80, 180, 255, 255),
            IsAntialias = true
        };

        canvas.DrawCircle(400, 300, 190, accentPaint);
        canvas.DrawText("Hello World", 205, 325, textPaint);
        canvas.Flush();

        PixData pix = new PixData();
        pix.pix_format = "RGBA:int8";
        pix.pix_data = bitmap.GetPixels();
        pix.pix_data_length = (IntPtr)bitmap.ByteCount;
        pix.width = Width;
        pix.height = Height;
        pix.pix_data_free = (ref pixdata) => true;
        
        wnd.PresentPix(ref pix);
    }
}