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
        cursor_pixdata.Dispose();

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
        
        int mouseX = 0;
        int mouseY = 0;
        
        
        wnd.OnMouseMove = (handle, x, y) =>
        {
            mouseX = x;
            mouseY = y;
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
                cursor_renderer.Border = 12;
                
                cursor_pixdata = cursor_renderer.RenderPix(cursor_svg, null, (bitmap, canvas) =>
                {
                    double dist = Math.Sqrt(Math.Pow(mouseX - 64, 2) + Math.Pow(mouseY - 64, 2));
                    double proximity = dist == 0 ? 1.0 : 1.0 - dist / 128.0;
                    
                    if (proximity <= 0.00) return;
                    
                    // Create a glow effect based on alpha
                    using (var paint = new SKPaint())
                    {
                        // Use a sine wave for smoother pulsing phase [0, 1]
                        float phase = (float)(Math.Sin(time * 2.0) * 0.5 + 0.5) * (float) proximity;
                        
                        // Define colors to interpolate between
                        SKColor startColor = SKColors.Black;
                        SKColor endColor = SKColors.Magenta;

                        // Interpolate between colors based on phase
                        byte r = (byte)(startColor.Red + (endColor.Red - startColor.Red) * phase);
                        byte g = (byte)(startColor.Green + (endColor.Green - startColor.Green) * phase);
                        byte b = (byte)(startColor.Blue + (endColor.Blue - startColor.Blue) * phase);
                        SKColor interpolatedColor = new SKColor(r, g, b);

                        // Pulse the blur radius with the phase
                        float blurRadius = 4.0f * phase;
                        paint.ImageFilter = SKImageFilter.CreateBlur(blurRadius, blurRadius);
                        
                        // SrcIn: replaces pixel color with interpolatedColor while keeping the blurred alpha
                        paint.ColorFilter = SKColorFilter.CreateBlendMode(
                            interpolatedColor, 
                            SKBlendMode.SrcIn
                        );

                        // Use Add or Screen blend mode to make it look like a "glow" on top of the original
                        paint.BlendMode = SKBlendMode.Plus;

                        // Draw the current bitmap state back onto itself through the glow filter
                        canvas.SaveLayer(paint);
                        canvas.ResetMatrix();
                        canvas.DrawBitmap(bitmap, 0, 0);
                        canvas.Restore();
                    }
                });
                translateHotSpot = cursor_renderer.TranslateHotSpot(hotspot);
                
                wnd.CursorPix(ref cursor_pixdata, (int) translateHotSpot.X, (int) translateHotSpot.Y);
                cursor_pixdata.Dispose();
                
                wnd.QueueRedraw();
            }
        };
        
        Application.Run();
    }
}
