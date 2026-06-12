# SkiaScene Playground Demo (Phase 2)

## Overview
This project is Phase 2 of the SkiaScene experiment. It demonstrates the capabilities of the experimental retained-mode Skia scene graph system by composing multiple element types, nested transforms, and time-based animations.

### What this demo proves:
- **Composition**: Composing complex objects (like an avatar) from simple procedural elements.
- **Hierarchy**: Using parent-relative transforms so that child parts (arms, legs, head) move relative to their parent group.
- **Animation**: Implementation of movement, bobbing, swinging (using sine waves), and opacity/scale changes.
- **Debugability**: Integrated debug overlay showing FPS, elapsed time, and element count.
- **Debug Rendering**: Visualization of element local origins and bounds (purple dashed lines).

## How to Build and Run
This is a manual-build experimental project. To build and run it:

1. Ensure the core dependencies (`JWCEssentials.net.dll`, `CrystalCatalystLibrary.net.dll`, `CrystalSkia.net.dll`) are available in your `MyReferencePath`.
2. Open the solution or use `dotnet` CLI:
   ```bash
   dotnet build CrystalCatalystSkiaSceneDemo.Experimental.csproj
   dotnet run --project CrystalCatalystSkiaSceneDemo.Experimental.csproj
   ```

## Intentionally Experimental
- The `SkiaScene` system is still in a "retained-mode" experimental state.
- Animation logic is currently handled via `OnUpdate` delegates to avoid subclassing everything, but a more formal animation system might be needed in the future.
- The `RenderContext` and `AnimationContext` are minimal and designed to be expanded as needed.
- `LocalBounds` are manually specified for now; an automatic bounds-calculation system would be a logical next step.

## Next Recommended Steps
- Implement automatic bounds calculation based on children and drawing commands.
- Add hit-testing support using the `LocalBounds`.
- Explore a more declarative way to define scenes (e.g., via JSON or XML).
- Add support for common animation tweens (EaseIn, EaseOut, etc.).
