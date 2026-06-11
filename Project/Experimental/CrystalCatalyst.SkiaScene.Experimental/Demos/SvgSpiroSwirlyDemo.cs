using System;
using System.Numerics;
using SkiaSharp;

namespace CrystalCatalyst.SkiaScene.Experimental.Demos
{
    /// <summary>
    /// A simple SVG spiro swirly demo for the experimental Skia scene system.  This
    /// demo arranges several copies of a small SVG motif around a center and
    /// animates them in orbit to create a swirling, flower-like pattern.  The
    /// motion is driven by the <see cref="AnimationContext.TotalSeconds"/> value and
    /// demonstrates nested transforms, procedural animation via <see cref="SkiaElement.OnUpdate"/>, and
    /// the reuse of <see cref="SkiaSvgElement"/> as leaf nodes.
    /// </summary>
    public class SvgSpiroSwirlyDemo : SkiaPresence
    {
        /// <summary>
        /// Create a scene containing several orbiting SVG motifs.  Each orbiting
        /// group rotates around the center or last at its own speed and radius.  The
        /// child SVG also spins around its own center at a secondary speed to
        /// create a layered swirl effect.
        /// </summary>
        public SvgSpiroSwirlyDemo()
        {
            var scene = this;

            // Root group that holds all orbiting arms.  Its transform can be
            // adjusted to reposition the entire swirl if desired.
            var root = new SkiaGroup
            {
                Identifier = "SpiroRoot",
                Transform = Matrix3x2.CreateTranslation(0f, 0f)
            };

            // Define an embedded SVG motif.  This is a simple star-like shape
            // drawn within a -10..10 coordinate space so that it is centered
            // around the origin.  Feel free to experiment with other shapes to
            // change the aesthetic of the swirl.
            const string svgMotif =
                "<svg xmlns=\"http://www.w3.org/2000/svg\" " +
                "width=\"20\" height=\"20\" viewBox=\"-10 -10 20 20\">" +
                "<path d=\"M0,-8 L2,-3 L8,-3 L3,1 L5,7 L0,3 L-5,7 L-3,1 L-8,-3 L-2,-3 Z\" " +
                "fill=\"none\" stroke=\"yellow\" stroke-width=\"1\"/>" +
                "</svg>";

            // Configure several orbit arms.  Each arm has its own radius and
            // angular speed.  Speeds are in revolutions per second (RPS).
            var orbitConfigs = new[]
            {
                new { Radius = 30f, Speed = 0.5f, SpinSpeed = -1.2f },
                new { Radius = 45f, Speed = -0.8f, SpinSpeed = 1.5f },
                new { Radius = 60f, Speed = 1.1f, SpinSpeed = -0.7f }
            };

            SkiaElement currentParent = root;

            bool spyro = true;
            
            int index = 0;
            foreach (var cfg in orbitConfigs)
            {
                // Create a group representing an orbit arm.  Its OnUpdate will
                // rotate the group around the origin based on the elapsed time.
                var arm = new SkiaGroup { Identifier = $"OrbitArm{index}" };
                int capturedIndex = index; // capture for closure

                arm.OnUpdate = (elem, dt, animContext) =>
                {
                    var radians = (float)(cfg.Speed * 2.0 * Math.PI * animContext.TotalSeconds);
                    ((SkiaGroup)elem).Transform = Matrix3x2.CreateRotation(radians) * Matrix3x2.CreateTranslation(cfg.Radius, 0f);
                };

                // Create the SVG element for the motif.  This uses the embedded
                // SVG markup defined above.  Its OnUpdate spins the motif around
                // its own center to add additional motion.
                var svgElem = new SkiaSvgElement
                {
                    Identifier = $"SvgPetal{index}",
                    Svg = svgMotif
                };
                svgElem.OnUpdate = (elem, dt, animContext) =>
                {
                    var radians = (float)(cfg.SpinSpeed * 2.0 * Math.PI * animContext.TotalSeconds);
                    ((SkiaElement)elem).Transform = Matrix3x2.CreateRotation(radians);
                };

                // Position the SVG outward along the X axis by the radius of the
                // orbit.  This translation is applied after the orbit arm's
                // rotation so that the motif moves in a circle around the origin.
                svgElem.Transform = Matrix3x2.CreateTranslation(cfg.Radius, 0f);

                // Add the SVG element to the orbit arm, then add the arm to the root.
                arm.Children.Add(svgElem);
                currentParent.Children.Add(arm);
                if (spyro) currentParent = arm;
                index++;
            }

            // Optionally, add a central SVG motif for visual interest.  This
            // remains at the origin but could also spin if desired.
            var centerSvg = new SkiaSvgElement
            {
                Identifier = "CenterSvg",
                Svg = svgMotif,
                Transform = Matrix3x2.Identity
            };
            centerSvg.OnUpdate = (elem, dt, animContext) =>
            {
                // Sl1ow rotation for the center motif
                var radians = (float)(0.25 * 2.0 * Math.PI * animContext.TotalSeconds);
                ((SkiaElement)elem).Transform = Matrix3x2.CreateRotation(radians);
            };
            root.Children.Add(centerSvg);

            // Add the root group to the scene
            scene.Elements.Add(root);
        }
    }
}