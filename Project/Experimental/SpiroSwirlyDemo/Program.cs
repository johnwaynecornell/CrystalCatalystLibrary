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
            
            wnd.OnDraw = handle =>
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