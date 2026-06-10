using System.Numerics;
using SkiaSharp;

namespace CrystalCatalyst.SkiaScene.Experimental.Demos
{
    /// <summary>
    /// A simple rolling wheel demo skeleton for the experimental Skia scene system.
    /// </summary>
    public static class RollingWheelDemo
    {
        // Maintain a single animation context across updates so that time and frame count
        // accumulate correctly when using the demo.  This is static because the demo
        // methods are also static and do not manage instance state.
        private static AnimationContext _context = new AnimationContext();

        /// <summary>
        /// Create a scene containing a simple cart with two wheels that roll as the cart
        /// moves across the canvas.  The wheels are drawn procedurally using
        /// <see cref="SkiaProceduralElement"/>, and the cart's position and wheel rotation
        /// are driven by the <see cref="AnimationContext.TotalSeconds"/> value.  All
        /// transforms are expressed using <see cref="Matrix3x2"/> so that the
        /// composition remains fast and easily composable.
        /// </summary>
        public static SkiaScene CreateScene()
        {
            var scene = new SkiaScene();

            // Create a parent group to represent the cart.  This group will translate
            // across the scene as time advances.  Child wheel groups will be positioned
            // relative to this parent.
            var cart = new SkiaGroup
            {
                Identifier = "Cart"
            };

            // Create the cart body as a procedural element.  This draws a simple
            // rectangle representing the cart's frame.  The rectangle is centered
            // around the local origin so that the transform applied to the cart group
            // controls its overall position.
            var body = new SkiaProceduralElement
            {
                Identifier = "CartBody"
            };
            body.OnRender = (canvas, element, renderContext) =>
            {
                using var paint = new SKPaint
                {
                    Color = SKColors.LightGray,
                    Style = SKPaintStyle.Fill
                };
                // Draw a centered rectangle 100 pixels wide and 20 pixels tall
                var rect = new SKRect(-50f, -10f, 50f, 10f);
                canvas.DrawRect(rect, paint);

                // Draw an outline for visual clarity
                using var outlinePaint = new SKPaint
                {
                    Color = SKColors.DarkGray,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 2f
                };
                canvas.DrawRect(rect, outlinePaint);
            };
            cart.Children.Add(body);

            // Helper method to create a wheel group with a procedural drawing.  Each
            // wheel draws a circle with spokes and rotates around its local origin.
            SkiaGroup CreateWheel(string id, float radius, int spokeCount, float xOffset)
            {
                var wheelGroup = new SkiaGroup
                {
                    Identifier = id
                };
                // Position the wheel relative to the cart using a translation
                wheelGroup.Transform = Matrix3x2.CreateTranslation(xOffset, 0f);

                // Draw the wheel using a procedural element
                var wheelShape = new SkiaProceduralElement
                {
                    Identifier = id + "_Shape"
                };
                wheelShape.OnRender = (canvas, element, renderContext) =>
                {
                    // Outer circle
                    using var paint = new SKPaint
                    {
                        Color = SKColors.Black,
                        Style = SKPaintStyle.Stroke,
                        StrokeWidth = 2f
                    };
                    canvas.DrawCircle(0f, 0f, radius, paint);

                    // Draw spokes
                    for (int i = 0; i < spokeCount; i++)
                    {
                        double angle = 2.0 * System.Math.PI * i / spokeCount;
                        var endX = radius * (float)System.Math.Cos(angle);
                        var endY = radius * (float)System.Math.Sin(angle);
                        canvas.DrawLine(0f, 0f, endX, endY, paint);
                    }
                };
                wheelGroup.Children.Add(wheelShape);

                // Rotate the wheel based on elapsed time.  The rotation speed is
                // arbitrary for the demo; in a real rolling simulation it would be
                // coupled to the cart's linear motion.  Here we rotate at one full
                // revolution per second (2 * PI radians per second).
                wheelGroup.OnUpdate = (elem, dt, animContext) =>
                {
                    var radians = (float)(2.0 * System.Math.PI * animContext.TotalSeconds);
                    // Apply rotation about the local origin and then translate by the xOffset
                    ((SkiaGroup)elem).Transform =
                        Matrix3x2.CreateRotation(radians) * Matrix3x2.CreateTranslation(xOffset, 0f);
                };
                return wheelGroup;
            }

            // Add left and right wheels to the cart.  They are spaced equally around
            // the origin of the cart body.  The radius and spoke count control the
            // visual appearance; adjust as desired.
            float wheelRadius = 10f;
            int spokeCount = 8;
            var leftWheel = CreateWheel("LeftWheel", wheelRadius, spokeCount, -25f);
            var rightWheel = CreateWheel("RightWheel", wheelRadius, spokeCount, 25f);
            cart.Children.Add(leftWheel);
            cart.Children.Add(rightWheel);

            // Animate the cart's translation across the canvas.  The x position
            // increases over time and wraps around when it exceeds a simple limit.
            cart.OnUpdate = (elem, dt, animContext) =>
            {
                // Horizontal speed in pixels per second
                float speed = 40f;
                float x = (float)((speed * animContext.TotalSeconds) % 200f) - 100f;
                // Fixed y position so the cart moves horizontally
                float y = 50f;
                ((SkiaGroup)elem).Transform = Matrix3x2.CreateTranslation(x, y);
            };

            // Add the cart to the scene
            scene.Elements.Add(cart);
            return scene;
        }

        /// <summary>
        /// Update the scene using a persistent animation context.  This advances
        /// the context by the given time delta and applies all update callbacks
        /// to the scene's elements.  Clients should call this once per frame.
        /// </summary>
        /// <param name="scene">The scene to update.</param>
        /// <param name="dt">Time delta in seconds.</param>
        public static void Update(SkiaScene scene, double dt)
        {
            if (scene == null)
            {
                return;
            }
            if (dt < 0)
            {
                dt = 0;
            }
            // Advance total time and frame count
            _context.Advance(dt);
            // Update the scene using the accumulated context
            scene.Update(dt, _context);
        }

        /// <summary>
        /// Render the scene onto the provided canvas.  The render context uses the
        /// animation context's total elapsed time to allow elements access to the
        /// global time value if needed.  The viewport dimensions may be left
        /// unspecified; they are provided here only for completeness.
        /// </summary>
        /// <param name="scene">The scene to render.</param>
        /// <param name="canvas">The Skia canvas to render into.</param>
        public static void Render(SkiaScene scene, SKCanvas canvas)
        {
            if (scene == null || canvas == null)
            {
                return;
            }
            var rc = new RenderContext
            {
                TotalSeconds = _context.TotalSeconds
            };
            scene.Render(canvas, rc);
        }
    }
}
