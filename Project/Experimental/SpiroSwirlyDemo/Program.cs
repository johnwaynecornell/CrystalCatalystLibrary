// See https://aka.ms/new-console-template for more information

using CrystalCatalyst.SkiaScene.Experimental;
using CrystalCatalyst.SkiaScene.Experimental.Demos;
using CrystalCatalystLibrary.net;
using CrystalSkia.net;
using SkiaSharp;

namespace SpiroSwirlyDemo
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.Init(args);
            
            int width = 800;
            int height = 600;
            
            var wnd = CrystalWindow.Create(width, height, "Wheel Demo");
            
            wnd.ApplicationRetain();
            wnd.Show(true);

            SkiaScene sceen = SvgSpiroSwirlyDemo.CreateScene();

            bool running = true;
            wnd.OnClose = (w) =>
            {
                running = false;
                w.ApplicationRelease();
            };
            
            wnd.OnResize = (w, newWidth, newHeight) =>
            {
                width = newWidth;
                height = newHeight;
            };
            
            CrystalWindow.Delegate_on_draw plainDraw = le =>
            {
                var pix = FixedPixDataRenderer.CreateFixed(width, height, (canvas, info) =>
                {
                    canvas.Clear(SKColors.Navy);
                    canvas.Translate(width / 2, height / 2);
                    SvgSpiroSwirlyDemo.Render(sceen, canvas);
                });
                    
                wnd.PresentPix(ref pix);
                pix.Dispose();
            };

            PixData screen = new PixData();
            CrystalWindow.Delegate_on_draw fancyDraw = le =>
            {
                if (!screen || screen.width != width || screen.height != height)
                {
                    screen.Dispose();
                    screen = FixedPixDataRenderer.CreateFixed(width, height, (canvas, info) => canvas.Clear(SKColors.Navy));
                }
                
                CrystalSkia.net.PixDataSkia.WithCanvasView(screen, (bitmap, canvas) =>
                {
                    canvas.Translate(width / 2, height / 2);
                    SvgSpiroSwirlyDemo.Render(sceen, canvas);
                    
                    canvas.ResetMatrix();
                    using (var paint = new SKPaint())
                    {
                        paint.Color = new SKColor(0, 0, 128, 30); // Navy with small alpha (~12%)
                        canvas.DrawRect(0, 0, width, height, paint);
                    }
                });
                
                wnd.PresentPix(ref screen);
            };
            
            wnd.OnMouseDown = (handle, button, i, i1) =>
            {
                if (wnd.OnDraw == plainDraw)
                {
                    screen.Dispose();
                    screen = FixedPixDataRenderer.CreateFixed(width, height,
                        (canvas, info) => canvas.Clear(SKColors.Navy));

                    wnd.OnDraw = fancyDraw;
                }
                else wnd.OnDraw = plainDraw;
            };

            wnd.OnDraw = plainDraw;
            
            double lastTime = -1;
            
            wnd.OnIdle = (w) =>
            {
                double time = wnd.uptimeSeconds();
                
                double dt = 0;

                if (lastTime < 0)
                {
                    lastTime = time;
                    SvgSpiroSwirlyDemo.Update(sceen, 0);
                    wnd.QueueRedraw();
                }
                else dt = time - lastTime;
                
                if (dt > 1.0 / 30.0)
                {
                    lastTime = time;
                    
                    SvgSpiroSwirlyDemo.Update(sceen, dt);
                    wnd.QueueRedraw();
                }
            };
            
            Application.Run();
        }
    }
}