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
        static void  Main(string[] args)
        {
            Application.Init(args);
            
            int width = 800;
            int height = 600;
            
            var wnd = CrystalWindow.Create(width, height, "Wheel Demo");
            
            wnd.ApplicationRetain();
            wnd.Show(true);

            SvgSpiroSwirlyDemo demo = new SvgSpiroSwirlyDemo();
            
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
                    demo.Render(canvas);
                });
                    
                wnd.PresentPix(ref pix);
                pix.Dispose();
            };

            PixData background = new PixData();
            
            PixData screen = new PixData();
            CrystalWindow.Delegate_on_draw fancyDraw = le =>
            {
                if (!background || background.width != width || background.height != height)
                {
                    background.Dispose();
                    background = FixedPixDataRenderer.CreateFixed(width, height, (canvas, info) =>
                    {
                        canvas.Clear(SKColors.Navy);
                        
                        // Create a textury bluish pattern
                        using (var paint = new SKPaint())
                        {
                            // Create a shader with a noise-like pattern
                            var colors = new SKColor[]
                            {
                                SKColors.Aqua,
                                new SKColor(20, 40, 140),    // Medium blue
                                new SKColor(40, 40, 180),    
                                new SKColor(30, 60, 160)     // Lighter blue
                            };

                            var positions = new float[] { 0f, 0.33f, 0.66f, 1f };

                            // Create turbulence shader for texture
                            var turbulence = SKShader.CreatePerlinNoiseTurbulence(
                                baseFrequencyX: 0.05f,
                                baseFrequencyY: 0.05f,
                                numOctaves: 4,
                                seed: 0);

                            // Combine with gradient for color variation
                            var gradient = SKShader.CreateLinearGradient(
                                new SKPoint(0, 0),
                                new SKPoint(width, height),
                                colors,
                                positions,
                                SKShaderTileMode.Mirror);

                            paint.Shader = SKShader.CreateCompose(gradient, turbulence, SKBlendMode.Multiply);
                            canvas.DrawRect(0, 0, width, height, paint);
                        }
                        
                    });
                }
                
                if (!screen || screen.width != width || screen.height != height)
                {
                    screen.Dispose();
                    screen = Pixels.ConvertPixelsPix(ref background, background.pix_format.ToString());
                }
                
                CrystalSkia.net.PixDataSkia.WithCanvasView(screen, (bitmap, canvas) =>
                {
                    canvas.Translate(width / 2, height / 2);
                    demo.Render(canvas);

                    
                    canvas.Save();
                    canvas.ResetMatrix();
                    
                    using (var paint = new SKPaint())
                    {
                        paint.Color = new SKColor(255, 255, 255, 30); // White with alpha 30
                        PixDataSkia.WithBitmapView(background, skBitmap => canvas.DrawBitmap(skBitmap, 0, 0, paint));
                    }
                    
                    canvas.Restore();
                    
                });
                
                wnd.PresentPix(ref screen);
            };
            
            ClickConverter click = new ClickConverter( (handle, button, x, y) =>
            {
                if (button != (int)CrystalMouseButton.Left) return;
                
                if (wnd.OnDraw == plainDraw)
                {
                    screen.Dispose();
                    screen = FixedPixDataRenderer.CreateFixed(width, height,
                        (canvas, info) => canvas.Clear(SKColors.Navy));

                    wnd.OnDraw = fancyDraw;
                }
                else wnd.OnDraw = plainDraw;
            });
            
            wnd.OnMouseDown = click.OnMouseDown;
            wnd.OnMouseUp = click.OnMouseUp;
            
            wnd.OnDraw = plainDraw;
            
            double lastTime = -1;
            
            wnd.OnIdle = (w) =>
            {
                double time = wnd.uptimeSeconds();
                
                double dt = 0;

                if (lastTime < 0)
                {
                    lastTime = time;
                    demo.Update(0);
                    wnd.QueueRedraw();
                }
                else dt = time - lastTime;
                
                if (dt > 1.0 / 30.0)
                {
                    lastTime = time;
                    
                    demo.Update(dt);
                    wnd.QueueRedraw();
                }
            };
            
            Application.Run();
        }
    }
}