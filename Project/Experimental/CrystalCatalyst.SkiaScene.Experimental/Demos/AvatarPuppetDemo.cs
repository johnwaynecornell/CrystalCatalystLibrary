using System;
using System.Numerics;
using SkiaSharp;

namespace CrystalCatalyst.SkiaScene.Experimental.Demos
{
    /// <summary>
    /// A simple procedural avatar/puppet demo for the experimental Skia scene system.  This
    /// demo constructs a stick-figure-like avatar from primitive shapes and animates
    /// its limbs using nested transforms.  The goal is to demonstrate how the
    /// retained-mode scene graph can articulate named body parts using parent/child
    /// relationships and the <see cref="SkiaElement.OnUpdate"/> hook for animation.
    /// </summary>
    public class AvatarPuppetDemo : SkiaPresence
    {
        /// <summary>
        /// Create a scene containing a simple articulated avatar composed of a
        /// body, head, arms and legs.  Each limb is attached via a pivot group
        /// that controls its rotation relative to the body.  The root of the
        /// avatar moves horizontally and the body bobs up and down to simulate
        /// walking.  Limb motions are phase-shifted to create a natural gait.
        /// </summary>
        public AvatarPuppetDemo()
        {
            var scene = this;

            // Root group for the entire avatar.  This will translate across the
            // canvas to simulate movement.  All other parts are children of this
            // group and therefore inherit its translation.
            var avatarRoot = new SkiaGroup { Identifier = "AvatarRoot" };
            // Horizontal movement speed (pixels/second) and bob amplitude
            float horizontalSpeed = 20f;
            float bobAmplitude = 5f;
            float bobFrequency = 2f; // oscillations per second
            avatarRoot.OnUpdate = (elem, dt, animCtx) =>
            {
                var t = animCtx.TotalSeconds;
                float x = horizontalSpeed * (float)t;
                float y = bobAmplitude * (float)Math.Sin(2.0 * Math.PI * bobFrequency * t);
                ((SkiaGroup)elem).Transform = Matrix3x2.CreateTranslation(x, y);
            };

            // Create the body as a simple rounded rectangle.  We center the body
            // about the origin; its height and width determine shoulder and hip
            // positions for limbs.
            var body = new SkiaProceduralElement { Identifier = "Body" };
            float bodyWidth = 20f;
            float bodyHeight = 40f;
            body.OnRender = (canvas, element, rc) =>
            {
                using var paint = new SKPaint { Color = SKColors.LightSlateGray, Style = SKPaintStyle.Fill };
                using var outline = new SKPaint { Color = SKColors.DarkSlateGray, Style = SKPaintStyle.Stroke, StrokeWidth = 2f };

                float RadiusX = 4f;
                float RadiusY = 4f;
                
                var rect = new SKRoundRect(new SKRect(-bodyWidth / 2f, -bodyHeight / 2f, bodyWidth / 2f, bodyHeight / 2f), RadiusX,RadiusY);
                canvas.DrawRoundRect(rect.Rect, RadiusX, RadiusY, paint);
                canvas.DrawRoundRect(rect.Rect, RadiusX, RadiusY, outline);
            };
            avatarRoot.Children.Add(body);

            // Create the head as a circle placed above the body.
            var head = new SkiaProceduralElement { Identifier = "Head" };
            float headRadius = 8f;
            head.OnRender = (canvas, element, rc) =>
            {
                using var paint = new SKPaint { Color = SKColors.LightGoldenrodYellow, Style = SKPaintStyle.Fill };
                using var outline = new SKPaint { Color = SKColors.DarkGoldenrod, Style = SKPaintStyle.Stroke, StrokeWidth = 2f };
                canvas.DrawCircle(0f, 0f, headRadius, paint);
                canvas.DrawCircle(0f, 0f, headRadius, outline);
            };
            // Position head above body: center of head is above body's top edge
            var headGroup = new SkiaGroup { Identifier = "HeadPivot", Transform = Matrix3x2.CreateTranslation(0f, -bodyHeight / 2f - headRadius) };
            headGroup.Children.Add(head);
            avatarRoot.Children.Add(headGroup);

            // Helper to create a limb pivot and shape.  Limb length and thickness
            // can be specified.  The pivot's translation defines the point
            // relative to the parent where the limb attaches.  The limb shape
            // draws a line from the pivot downwards by length.
            SkiaGroup CreateLimb(string name, Vector2 attachPoint, float length, float thickness)
            {
                var pivot = new SkiaGroup { Identifier = name + "Pivot", Transform = Matrix3x2.CreateTranslation(attachPoint) };
                var limb = new SkiaProceduralElement { Identifier = name };
                limb.OnRender = (canvas, element, rc) =>
                {
                    using var paint = new SKPaint { Color = SKColors.SlateGray, Style = SKPaintStyle.Stroke, StrokeWidth = thickness };
                    canvas.DrawLine(0f, 0f, 0f, length, paint);
                };
                // Translate limb downwards so that it originates at pivot
                limb.Transform = Matrix3x2.CreateTranslation(0f, 0f);
                pivot.Children.Add(limb);
                return pivot;
            }

            // Define attachment points for limbs relative to body center.  Shoulders
            // are located near the top of the body; hips are near the bottom.
            var leftShoulder = new Vector2(-bodyWidth / 2f + 1f, -bodyHeight / 2f + 5f);
            var rightShoulder = new Vector2(bodyWidth / 2f - 1f, -bodyHeight / 2f + 5f);
            var leftHip = new Vector2(-bodyWidth / 3f, bodyHeight / 2f);
            var rightHip = new Vector2(bodyWidth / 3f, bodyHeight / 2f);

            // Create limbs.  Adjust lengths and thicknesses for arms and legs.
            var leftArm = CreateLimb("LeftArm", leftShoulder, length: 25f, thickness: 2f);
            var rightArm = CreateLimb("RightArm", rightShoulder, length: 25f, thickness: 2f);
            var leftLeg = CreateLimb("LeftLeg", leftHip, length: 30f, thickness: 3f);
            var rightLeg = CreateLimb("RightLeg", rightHip, length: 30f, thickness: 3f);

            // Add limbs to the body
            avatarRoot.Children.Add(leftArm);
            avatarRoot.Children.Add(rightArm);
            avatarRoot.Children.Add(leftLeg);
            avatarRoot.Children.Add(rightLeg);

            // Limb animation parameters.  We use sine waves for natural motion.
            float armAmplitude = 30f; // degrees
            float armFrequency = 1f;  // cycles per second
            float legAmplitude = 25f; // degrees
            float legFrequency = 1f;

            // Animate arms: left and right arms have different phases.  Left arm
            // swings with sin, right arm swings with -sin to move oppositely.
            leftArm.OnUpdate = (elem, dt, animCtx) =>
            {
                var angleDeg = armAmplitude * (float)Math.Sin(2.0 * Math.PI * armFrequency * animCtx.TotalSeconds);
                var angleRad = MathF.PI / 180f * angleDeg;
                ((SkiaGroup)elem).Transform = Matrix3x2.CreateRotation(angleRad) * Matrix3x2.CreateTranslation(leftShoulder);
            };
            rightArm.OnUpdate = (elem, dt, animCtx) =>
            {
                var angleDeg = -armAmplitude * (float)Math.Sin(2.0 * Math.PI * armFrequency * animCtx.TotalSeconds);
                var angleRad = MathF.PI / 180f * angleDeg;
                ((SkiaGroup)elem).Transform = Matrix3x2.CreateRotation(angleRad) * Matrix3x2.CreateTranslation(rightShoulder);
            };

            // Animate legs: swing opposite to each other.  Use the same amplitude
            // but opposite phases.  Leg pivots are repositioned after rotation.
            leftLeg.OnUpdate = (elem, dt, animCtx) =>
            {
                var angleDeg = legAmplitude * (float)Math.Sin(2.0 * Math.PI * legFrequency * animCtx.TotalSeconds);
                var angleRad = MathF.PI / 180f * angleDeg;
                ((SkiaGroup)elem).Transform = Matrix3x2.CreateRotation(angleRad) * Matrix3x2.CreateTranslation(leftHip);
            };
            rightLeg.OnUpdate = (elem, dt, animCtx) =>
            {
                var angleDeg = -legAmplitude * (float)Math.Sin(2.0 * Math.PI * legFrequency * animCtx.TotalSeconds);
                var angleRad = MathF.PI / 180f * angleDeg;
                ((SkiaGroup)elem).Transform = Matrix3x2.CreateRotation(angleRad) * Matrix3x2.CreateTranslation(rightHip);
            };

            // Add the avatar root to the scene
            scene.Elements.Add(avatarRoot);
        }
    }
}