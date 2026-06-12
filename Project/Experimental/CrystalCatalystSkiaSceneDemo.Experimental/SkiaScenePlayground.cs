using System;
using System.Collections.Generic;
using System.Numerics;
using SkiaSharp;
using CrystalCatalyst.SkiaScene.Experimental;

namespace CrystalCatalystSkiaSceneDemo.Experimental
{
    /// <summary>
    /// A comprehensive demo scene demonstrating multiple element types, nested transforms,
    /// animations, and debug features.
    /// </summary>
    public class SkiaScenePlayground : SkiaPresence
    {
        private int _frameCount = 0;
        private double _lastFpsTime = 0;
        private double _fps = 0;

        public SkiaScenePlayground()
        {
            // 1. Background Group (Sky and Ground)
            var background = new SkiaProceduralElement { Identifier = "Background" };
            background.OnRender = (canvas, elem, rc) =>
            {
                // Draw sky
                using var skyPaint = new SKPaint { Color = new SKColor(135, 206, 235) }; // Sky blue
                canvas.DrawRect(0, 0, rc.ViewportWidth, rc.ViewportHeight * 0.7f, skyPaint);

                // Draw ground
                using var groundPaint = new SKPaint { Color = new SKColor(34, 139, 34) }; // Forest green
                canvas.DrawRect(0, rc.ViewportHeight * 0.7f, rc.ViewportWidth, rc.ViewportHeight * 0.3f, groundPaint);

                // Draw simple grid on ground for depth/scale reference
                using var gridPaint = new SKPaint { Color = new SKColor(255, 255, 255, 40), Style = SKPaintStyle.Stroke, StrokeWidth = 1f };
                float groundY = rc.ViewportHeight * 0.7f;
                for (int i = 0; i < rc.ViewportWidth; i += 50)
                {
                    canvas.DrawLine(i, groundY, i, rc.ViewportHeight, gridPaint);
                }
                for (int i = (int)groundY; i < rc.ViewportHeight; i += 50)
                {
                    canvas.DrawLine(0, i, rc.ViewportWidth, i, gridPaint);
                }
            };
            Elements.Add(background);

            // 2. Animated Avatar Group
            var avatarRoot = new SkiaGroup { Identifier = "AvatarRoot" };
            avatarRoot.LocalBounds = new SKRect(-25, -75, 25, 25);
            
            // Root movement animation
            avatarRoot.OnUpdate = (elem, dt, animCtx) =>
            {
                // Move across the bottom of the canvas
                float x = (float)(animCtx.TotalSeconds * 60) % 1000 - 100;
                float baseHeight = 480; 
                float y = baseHeight + SkiaAnimationHelper.SineWave(animCtx.TotalSeconds, 8f/ (float)(2.0 * Math.PI), 5f); // Walking bob
                
                // Pulsing scale for "breathing" effect
                float scale = 1.0f + SkiaAnimationHelper.SineWave(animCtx.TotalSeconds, 2f/ (float)(2.0 * Math.PI), 0.05f);
                
                elem.Transform = Matrix3x2.CreateScale(scale) * Matrix3x2.CreateTranslation(x, y);
            };

            // Avatar Body
            var body = new SkiaProceduralElement { Identifier = "Body" };
            body.LocalBounds = new SKRect(-20, -40, 20, 20);
            body.OnRender = (canvas, elem, rc) =>
            {
                using var paint = new SKPaint { Color = SKColors.OrangeRed, Style = SKPaintStyle.Fill, IsAntialias = true };
                using var stroke = new SKPaint { Color = SKColors.DarkRed, Style = SKPaintStyle.Stroke, StrokeWidth = 2, IsAntialias = true };
                var rect = new SKRect(-20, -40, 20, 20);
                canvas.DrawRoundRect(rect, 10, 10, paint);
                canvas.DrawRoundRect(rect, 10, 10, stroke);
            };
            avatarRoot.Children.Add(body);

            // Avatar Head
            var head = new SkiaProceduralElement { Identifier = "Head" };
            head.LocalBounds = new SKRect(-15, -15, 15, 15);
            head.Transform = Matrix3x2.CreateTranslation(0, -55);
            head.OnRender = (canvas, elem, rc) =>
            {
                using var paint = new SKPaint { Color = SKColors.PeachPuff, Style = SKPaintStyle.Fill, IsAntialias = true };
                using var stroke = new SKPaint { Color = SKColors.BurlyWood, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f, IsAntialias = true };
                canvas.DrawCircle(0, 0, 15, paint);
                canvas.DrawCircle(0, 0, 15, stroke);
                
                // Eyes
                using var eyePaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
                canvas.DrawCircle(-5, -2, 2, eyePaint);
                canvas.DrawCircle(5, -2, 2, eyePaint);
                
                // Mouth
                using var mouthPaint = new SKPaint { Color = SKColors.IndianRed, Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = true };
                canvas.DrawArc(new SKRect(-5, 2, 5, 8), 0, 180, false, mouthPaint);
            };
            avatarRoot.Children.Add(head);

            // Swinging Arms
            var leftArmPivot = new SkiaGroup { Identifier = "LeftArmPivot" };
            leftArmPivot.LocalBounds = new SKRect(-4, 0, 4, 30);
            var leftArm = new SkiaProceduralElement { Identifier = "LeftArm" };
            leftArm.OnRender = (canvas, elem, rc) => {
                using var paint = new SKPaint { Color = SKColors.DarkRed, StrokeWidth = 8, StrokeCap = SKStrokeCap.Round, IsAntialias = true };
                canvas.DrawLine(0, 0, 0, 30, paint);
            };
            leftArmPivot.Children.Add(leftArm);
            leftArmPivot.OnUpdate = (elem, dt, animCtx) => {
                float angle = SkiaAnimationHelper.SineWave(animCtx.TotalSeconds, 8f / (float)(2.0 * Math.PI), 0.7f);
                elem.Transform = Matrix3x2.CreateRotation(angle) * Matrix3x2.CreateTranslation(-22, -35);
            };
            avatarRoot.Children.Add(leftArmPivot);

            var rightArmPivot = new SkiaGroup { Identifier = "RightArmPivot" };
            rightArmPivot.LocalBounds = new SKRect(-4, 0, 4, 30);
            var rightArm = new SkiaProceduralElement { Identifier = "RightArm" };
            rightArm.OnRender = (canvas, elem, rc) => {
                using var paint = new SKPaint { Color = SKColors.DarkRed, StrokeWidth = 8, StrokeCap = SKStrokeCap.Round, IsAntialias = true };
                canvas.DrawLine(0, 0, 0, 30, paint);
            };
            rightArmPivot.Children.Add(rightArm);
            rightArmPivot.OnUpdate = (elem, dt, animCtx) => {
                float angle = -SkiaAnimationHelper.SineWave(animCtx.TotalSeconds, 8f / (float)(2.0 * Math.PI), 0.7f);
                elem.Transform = Matrix3x2.CreateRotation(angle) * Matrix3x2.CreateTranslation(22, -35);
            };
            avatarRoot.Children.Add(rightArmPivot);

            Elements.Add(avatarRoot);

            // 3. Fading and Rotating Object (Floating Star)
            var fader = new SkiaProceduralElement { Identifier = "FloatingStar" };
            fader.LocalBounds = new SKRect(-30, -30, 30, 30);
            fader.OnRender = (canvas, elem, rc) => {
                using var paint = new SKPaint { 
                    Color = SKColors.Yellow.WithAlpha((byte)(elem.Opacity * 255)),
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true 
                };
                DrawStar(canvas, 0, 0, 30, 15, paint);
            };
            fader.OnUpdate = (elem, dt, animCtx) => {
                // Opacity pulses
                elem.Opacity = 0.5f + SkiaAnimationHelper.SineWave(animCtx.TotalSeconds, 3f / (float)(2.0 * Math.PI), 0.5f);
                // Rotates and floats
                float x = 600 + (float)Math.Cos(animCtx.TotalSeconds) * 50;
                float y = 150 + (float)Math.Sin(animCtx.TotalSeconds * 2) * 20;
                elem.Transform = Matrix3x2.CreateRotation((float)animCtx.TotalSeconds * 2) * Matrix3x2.CreateTranslation(x, y);
            };
            Elements.Add(fader);

            // 4. Debug Overlay (UI layer)
            var debugOverlay = new SkiaProceduralElement { Identifier = "DebugOverlay" };
            debugOverlay.OnRender = (canvas, elem, rc) =>
            {
                _frameCount++;
                double now = rc.TotalSeconds;
                if (now - _lastFpsTime >= 1.0)
                {
                    _fps = _frameCount / (now - _lastFpsTime);
                    _frameCount = 0;
                    _lastFpsTime = now;
                }

                using var font = new SKFont(SKTypeface.Default, 16);
                using var paint = new SKPaint { Color = SKColors.White, IsAntialias = true };
                using var labelFont = new SKFont(SKTypeface.Default, 14);
                using var labelPaint = new SKPaint { Color = SKColors.Cyan, IsAntialias = true };
                using var bgPaint = new SKPaint { Color = new SKColor(0, 0, 0, 160) };
                
                canvas.DrawRect(10, 10, 260, 120, bgPaint);
                canvas.DrawText("SkiaScene Playground Debug", 20, 30, SKTextAlign.Left, font, paint);
                canvas.DrawText($"Time: {rc.TotalSeconds:F2}s", 20, 55, SKTextAlign.Left, labelFont, labelPaint);
                canvas.DrawText($"FPS: {_fps:F1}", 20, 75, SKTextAlign.Left, labelFont, labelPaint);
                canvas.DrawText($"Element Count: {CountElements(this)}", 20, 95, SKTextAlign.Left, labelFont, labelPaint);
                canvas.DrawText($"Debug Mode (Bounds): {rc.DebugMode}", 20, 115, SKTextAlign.Left, labelFont, labelPaint);
            };
            Elements.Add(debugOverlay);
        }

        private int CountElements(SkiaScene scene)
        {
            int count = 0;
            foreach (var elem in scene.Elements)
            {
                count += CountRecursive(elem);
            }
            return count;
        }

        private int CountRecursive(SkiaElement elem)
        {
            int count = 1;
            foreach (var child in elem.Children)
            {
                count += CountRecursive(child);
            }
            return count;
        }

        private static void DrawStar(SKCanvas canvas, float x, float y, float outerRadius, float innerRadius, SKPaint paint)
        {
            using var path = new SKPath();
            for (int i = 0; i < 5; i++)
            {
                float angle = (float)(i * Math.PI * 2 / 5 - Math.PI / 2);
                float x1 = x + (float)Math.Cos(angle) * outerRadius;
                float y1 = y + (float)Math.Sin(angle) * outerRadius;
                if (i == 0) path.MoveTo(x1, y1);
                else path.LineTo(x1, y1);

                angle += (float)(Math.PI / 5);
                x1 = x + (float)Math.Cos(angle) * innerRadius;
                y1 = y + (float)Math.Sin(angle) * innerRadius;
                path.LineTo(x1, y1);
            }
            path.Close();
            canvas.DrawPath(path, paint);
        }
    }
}
