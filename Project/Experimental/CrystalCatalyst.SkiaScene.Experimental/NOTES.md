CrystalCatalyst Skia Scene Experiment Notes
===========================================

Proof of Feel
-------------

This experimental project was intentionally guided by small, concrete demos rather than an abstract design. Each prototype was conceived to answer a specific question about how a retained‑mode Skia scene graph would _feel_ when animating objects, composing SVGs, or articulating a puppet. By keeping the scope narrow and visual, the experiment surfaced practical needs and patterns that might otherwise be lost in a purely theoretical architecture discussion.

Three Demos, Three Axes
-----------------------

**RollingWheelDemo – Mechanical Hierarchy**

The first proof of feel built a tiny cart with two wheels. A parent group translated across the canvas while each wheel group rotated locally. This established that hierarchical transforms and local rotation behave intuitively: a child’s rotation does not disrupt the parent’s translation, and vice versa.

**SvgSpiroSwirlyDemo – SVG Composition**

The second proof focused on composing simple SVG motifs. Multiple orbit arms rotated around a centre, each containing an SVG element that also rotated or counter‑rotated. This demonstrated that `SkiaSvgElement` can live naturally in the element tree and that nested transforms allow for rich decorative motion without complex math.

**AvatarPuppetDemo – Articulated Parts**

The third demo built a stick‑figure puppet from procedural shapes. Arms, legs, head and body were attached via pivot groups that rotated the limbs in opposite phases to simulate a walk. A root group moved and bobbed the entire avatar. This showed that named parts and pivot‑based animation are straightforward with the existing `OnUpdate` hook.

SkiaPresence
------------

A recurring pattern in the demos was the desire for a scene object that owns its own time and render context. The `SkiaPresence` class was introduced to wrap a `SkiaScene` with a persistent `AnimationContext` and `RenderContext`. This allows demos to be self‑contained: `presence.Update(dt)` advances the clock and updates the graph; `presence.Render(canvas)` draws the current state. At the same time, the lower‑level `SkiaScene.Update` and `SkiaScene.Render` methods remain available for hosts that manage their own timing.

PixData Ownership Lesson
------------------------

Early code attempted to use `Pixel_ConvertPixels` to obtain a mutable image. That API is designed for hot‑path format conversion and may return a view of existing pixels rather than copying. The experiment introduced `Pixels.CopyOrConvertPix` to provide an explicit copy‑or‑convert operation. Callers who need a writable, owned `PixData` should use the new API; callers who only need a conversion for immediate use can continue to use `Pixel_ConvertPixels`.

Retained Screen / Temporal Drawing
----------------------------------

The SpiroSwirlyDemo executable loader explored a different rendering pattern. It maintains a durable `background` PixData and a `screen` PixData that retains the previous frame. Each frame:

*   If necessary, regenerate the `background` with a Perlin‑noise gradient pattern.
*   Ensure `screen` is a copy of `background` via `Pixels.CopyOrConvertPix`.
*   Draw the demo on `screen` without clearing it.
*   Blend a faint copy of the `background` over `screen` to gently pull old strokes towards the stable texture.

Because the canvas is not cleared between frames, motion accumulates as trails on a stable background. The translucent blend acts like a decaying memory: old strokes gradually fade but do not disappear immediately. This pattern produces a motion‑blurred look without a formal effects pipeline.

What Not To Build Yet
---------------------

Several features were deliberately deferred in order to keep the experiment focused on feel rather than completeness:

*   **Timeline / keyframe system** – animation is currently driven via simple `OnUpdate` callbacks. A full timeline would add complexity that was not needed for the proofs.
*   **Puppet editor** – the puppet demo uses code to position limbs. An interactive editor might be desirable later but is outside the scope of this experiment.
*   **Asset pipeline** – there is no loader for external assets; SVGs are embedded strings and images are simple shapes.
*   **Full effects pipeline** – postprocessing and advanced compositing were not explored; the retained‑screen technique serves as a lightweight alternative.
*   **OpenGL bridge** – although PixData can come from OpenGL, this bridge was not exercised here.
*   **Production rendering framework** – nothing in this experiment assumes a production API; the demos run manually and are not part of the default build.

Next Natural Questions
----------------------

*   **Shared demo catalog?** With three demos, patterns emerge. Should there be a small interface or catalog to register and run demos consistently?
*   **Retained‑frame helper?** The temporal blending technique is useful. Should the library provide a helper for retained‑frame drawing with decay?
*   **SkiaPresence promotion?** Is `SkiaPresence` just an experiment, or should it be promoted to a more official part of CrystalCatalyst’s Skia layer?
*   **Implications for NewAge overhaul?** How do these small scene objects, PixData bridges, and temporal drawing inform the broader goals of NewAge proper? Do they change the way the new platform might handle 2D graphics or layering?