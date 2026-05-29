using SkiaSharp;
using CrystalSkia.net;
using CrystalCatalystLibrary.net;
using SvgIconAnimation;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Application.Init(args);

        var icon_svg = Crystal.SKSvgFromText(SvgSrc.svgCatalystRotor);
        var cursor_svg = Crystal.SKSvgFromText(SvgSrc.svgCatalystCrystal);
        
        SvgSkiaRenderer cursor_renderer;
        PixData cursor_pixdata;


        var renderer = new SvgSkiaRenderer();
        renderer.Size = new SKSize(128, 128); // Ensure the icon stays the same size
        renderer.Crop = false; // Use Size
        var wnd = CrystalWindow.Create(400, 300, "SVG Icon Animation");

        SKPoint translateHotSpot;
        SKPoint hotspot;
        
        cursor_renderer = new SvgSkiaRenderer();
        cursor_renderer.Crop = true; // Use Size
        
        cursor_pixdata = cursor_renderer.RenderPix(cursor_svg);
        
        hotspot = new SKPoint(6, 4);
        translateHotSpot = cursor_renderer.TranslateHotSpot(hotspot);
        wnd.CursorPix(ref cursor_pixdata, (int) translateHotSpot.X, (int) translateHotSpot.Y);

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

        double spin_time = -1;

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

                m = SKMatrix.CreateRotation((float)Math.PI / 8f * (float) time*2,64,64);
                renderer.Matrix = m;
                    
                var pix = renderer.RenderPix(icon_svg);
                wnd.IconPix(ref pix);
                pix.Dispose();


                float spin = (float)(time - spin_time);
                if (spin > 1) spin = 0;
                
                m = SKMatrix.CreateRotation((float)Math.PI * 2 * spin, hotspot.X, hotspot.Y);
                cursor_renderer.Matrix = m; 
                
                cursor_pixdata = cursor_renderer.RenderPix(cursor_svg);
                translateHotSpot = cursor_renderer.TranslateHotSpot(hotspot);
                
                wnd.CursorPix(ref cursor_pixdata, (int) translateHotSpot.X, (int) translateHotSpot.Y);
                
                wnd.QueueRedraw();
            }
        };
        
        Application.Run();
    }
}
