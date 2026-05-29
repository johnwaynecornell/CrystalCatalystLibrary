using System;
using System.IO;
using System.Text;
using System.Threading;
using SkiaSharp;
using Svg.Skia;
using CrystalSkia.net;
using CrystalCatalystLibrary.net;
using SvgIconAnimation;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        CrystalCatalystLibrary.net.Application.Init(args);

        var cursor_svg = new Svg.Skia.SKSvg();
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(SvgSrc.svgCatalystCrystal)))
        {
            cursor_svg.Load(stream);
        }

        var cursor_renderer = new SvgSkiaRenderer();
        //cursor_renderer.Size = new SKSize(64, 64);
        cursor_renderer.Crop = true; // Use Size
        PixData cursor_pixdata = cursor_renderer.RenderPix(cursor_svg);

        var icon_svg = new Svg.Skia.SKSvg();
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(SvgSrc.svgCatalystRotor)))
        {
            icon_svg.Load(stream);
        }

        var renderer = new SvgSkiaRenderer();
        renderer.Size = new SKSize(128, 128); // Ensure the icon stays the same size
        renderer.Crop = false; // Use Size
        var wnd = CrystalWindow.Create(400, 300, "SVG Icon Animation");

        wnd.CursorPix(ref cursor_pixdata, 6, 4);

        wnd.ApplicationRetain();
        wnd.Show(true);

        bool running = true;
        wnd.OnClose = (w) =>
        {
            running = false;
            w.ApplicationRelease();
        };

        double lastTime = 0;
        double fps = 30.0;

        wnd.OnResize = (handle, width, height) => { };

        wnd.OnDraw = handle =>
        {
            var pix = renderer.RenderPix(icon_svg);
            wnd.PresentPix(ref pix);
            pix.Dispose();
        };

        double spin_time = 0;

        wnd.OnMouseDown = (handle, button, x, y) =>
        {
            if (button == 1 && x < 128 && y < 128) spin_time = wnd.uptimeSeconds();
        };

        wnd.OnIdle = handle =>
        {
            double time = wnd.uptimeSeconds();
            if (time - lastTime >= 1.0 / fps)
            {
                lastTime = time;

                SKMatrix m;

                m = SKMatrix.Concat(SKMatrix.CreateTranslation(64, 64), SKMatrix.CreateRotation((float)Math.PI / 8f * (float) time*2));
                m = SKMatrix.Concat(m, SKMatrix.CreateTranslation(-64, -64)); 
                
                renderer.Matrix = m;
                    
                var pix = renderer.RenderPix(icon_svg);
                wnd.IconPix(ref pix);
                pix.Dispose();


                float spin = (float)(time - spin_time);
                if (spin > 1) spin = 0;
                
                SKPoint hotspot = new SKPoint(6, 4);
                
                m = SKMatrix.Concat(SKMatrix.CreateTranslation(hotspot.X, hotspot.Y), SKMatrix.CreateRotation((float)Math.PI * 2 * spin));
                m = SKMatrix.Concat(m, SKMatrix.CreateTranslation(-hotspot.X, -hotspot.Y));
                
                cursor_renderer.Matrix = m; 
                
                cursor_pixdata = cursor_renderer.RenderPix(cursor_svg);
                SKPoint translateHotSpot = cursor_renderer.TranslateHotSpot(hotspot);
                
                wnd.CursorPix(ref cursor_pixdata, (int) translateHotSpot.X, (int) translateHotSpot.Y);
                
                wnd.QueueRedraw();
            }
        };
        
        Application.Run();
        running = false;
    }
}
