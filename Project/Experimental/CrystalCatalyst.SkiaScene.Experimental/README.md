# CrystalCatalystSkiaScene.Experimental

This directory contains an **experimental** retained‑mode scene graph and animation layer for CrystalCatalyst built on top of SkiaSharp.  It is a side project living alongside `CrystalCatalystLibrary.net` and is not part of the default or futures build system.

The goal of this experiment is to explore whether CrystalCatalyst should eventually grow a lightweight 2D element system capable of composing SVG, bitmap, PixData, and procedural drawing nodes using simple transforms and child hierarchies.  The system is deliberately minimal and focuses on clear extension points rather than completeness.

**Key design goals**

- A small base class (`SkiaElement`) with properties for identifier, local transform, visibility, opacity, and a child collection.
- A retained‑mode update/render model: each element can update itself per frame and render itself onto a `SKCanvas`.
- A transform stack based on `System.Numerics.Matrix3x2` to compose local transforms hierarchically.
- Simple stub implementations for SVG, bitmap, and PixData elements.
- A `SkiaScene` container to hold the top‑level elements and dispatch update and render calls.

**Manual build**

This project is not wired into the normal `build_managed.sh` pipeline.  To build it manually, run something like:

```sh
dotnet build Projects/CrystalCatalystSkiaScene.Experimental/CrystalCatalystSkiaScene.Experimental.csproj
```

For convenience, a simple `Dev/build_manual.sh` script is included in this directory.  This script invokes `dotnet build` for you.

**Status**

Everything here is experimental.  APIs are unstable and incomplete.  Integrations with JWCEssentials/PixData/OpenGL or a full animation timeline are intentionally deferred.  The system may not compile without adjusting package references or paths to suit your environment.
