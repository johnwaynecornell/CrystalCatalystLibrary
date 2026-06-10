using System.Numerics;
using SkiaSharp;

namespace CrystalCatalyst.SkiaScene.Experimental.Demos
{
    /// <summary>
    /// A simple rolling wheel demo skeleton for the experimental Skia scene system.
    /// </summary>
    public static class RollingWheelDemo
    {
        public static SkiaScene CreateScene()
        {
            var scene = new SkiaScene();

            // Create a group to represent the wheel.
            var wheel = new SkiaGroup
            {
                Identifier = "Wheel"
            };

            // TODO: Add child elements such as circles or spokes.

            // Position the wheel at the origin.
            wheel.Transform = Matrix3x2.Identity;

            scene.Elements.Add(wheel);

            return scene;
        }

        /// <summary>
        /// Update the scene by advancing the wheel rotation based on distance.
        /// </summary>
        public static void Update(SkiaScene scene, double dt)
        {
            // TODO: Implement rotation/distance coupling for rolling motion.
            scene.Update(dt, new AnimationContext());
        }

        /// <summary>
        /// Render the scene onto the given canvas.
        /// </summary>
        public static void Render(SkiaScene scene, SKCanvas canvas)
        {
            scene.Render(canvas, new RenderContext());
        }
    }
}
