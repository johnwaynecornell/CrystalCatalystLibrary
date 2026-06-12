using System;
using SkiaSharp;
using CrystalCatalyst.SkiaScene.Experimental;
using CrystalCatalystLibrary.net;
using CrystalSkia.net;
using JWCEssentials.net;

namespace CrystalCatalystSkiaSceneDemo.Experimental
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Initialize the application environment
            Application.Init(args);
            
            int width = 1024;
            int height = 768;
            
            // Create a window for the demo
            var wnd = CrystalWindow.Create(width, height, "SkiaScene Playground - Phase 2");
            
            wnd.ApplicationRetain();
            wnd.Show(true);

            // Initialize the experimental scene
            var playground = new SkiaScenePlayground();
            playground.RenderContext.ViewportWidth = width;
            playground.RenderContext.ViewportHeight = height;
            playground.RenderContext.DebugMode = true; // Enable debug rendering by default for Phase 2 proof

            wnd.OnClose = (w) =>
            {
                w.ApplicationRelease();
            };
            
            wnd.OnResize = (w, newWidth, newHeight) =>
            {
                width = newWidth;
                height = newHeight;
                // Update viewport size in render context
                playground.RenderContext.ViewportWidth = width;
                playground.RenderContext.ViewportHeight = height;
            };
            
            wnd.OnDraw = handle =>
            {
                // Create a pixel buffer and render the scene into it
                var pix = FixedPixDataRenderer.CreateFixed(width, height, (canvas, info) =>
                {
                    canvas.Clear(SKColors.Black);
                    playground.Render(canvas);
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
                    playground.Update(0);
                    wnd.QueueRedraw();
                }
                else
                {
                    dt = time - lastTime;
                }
                
                // Limit update rate to ~60 FPS to save CPU
                if (dt >= 1.0 / 60.0)
                {
                    lastTime = time;
                    playground.Update(dt);
                    wnd.QueueRedraw();
                }
            };
            
            // Start the application loop
            Application.Run();
        }
    }
}
