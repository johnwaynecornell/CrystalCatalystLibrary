# Reference.md

## Purpose
This document serves as an orientation guide for AI assistants and maintainers of the **CrystalCatalystLibrary**. It maps the repository's structure, exported entry points, architectural seams, and extension points to facilitate efficient maintenance and development. This is a practical map, not exhaustive API documentation.

## Repository Overview
**CrystalCatalystLibrary** is a managed and native support library providing cross-platform windowing, rendering helpers, and integration with the Crystal/NewAge ecosystem.

- **Primary Languages**: C#, C/C++, .NET 10.0, OpenGL.
- **Target Platforms**: Windows and Linux (X11 and Wayland).
- **Major Projects**:
    - **Native Core**: C++ library providing the foundational windowing, clipboard, and pixel management logic.
    - **Managed .NET Wrapper**: C# bindings for the native core, facilitating high-level usage in .NET applications.
    - **OpenGL Subsystem**: Utilities for OpenGL context management, texture rendering, and pixel interchange.
    - **Skia Subsystem**: Integration with SkiaSharp for 2D rendering.
    - **CrystalOptics** *(companion)*: Screen capture library and `FacetCLI` tool. Built as `add_subdirectory(CrystalOptics)`. See [CrystalOptics companion](#crystaloptics-companion) below.
- **Primary Solution/Project Files**:
    - `Project/CrystalCatalystLibrary.net/CrystalCatalystLibrary.net.sln` (.NET Solution)
    - `CMakeLists.txt` (Native Build Configuration)
    - `CrystalOptics/CMakeLists.txt` (CrystalOptics companion native build)

## Orientation for AI Assistants
- **Prefer Local Conventions**: Follow the established `_EXPORT_` and `Generated/` naming and structural patterns.
- **Check Existing Helpers**: Before introducing new abstractions, consult `GLHelper.cs`, `GLTextureHelper.cs`, and `SkiaConvert.cs`.
- **Repo-Relative Paths**: Always use repo-relative paths in notes, documentation, and when referencing symbols.
- **Platform Specificity**: Code within `Platform/Linux/` or `Platform/Windows/` is platform-specific. Avoid assuming platform neutrality in these areas.
- **Grep-Friendly Exports**: Do not reformat `_EXPORT_` declarations; they must remain single-line for automated tool discovery.

## Build / Runtime Assumptions
- **Native Interop**: Managed code relies on `DllImport` to call symbols in `CrystalCatalystLibrary` (native).
- **Graphics Stack**: Assumes OpenGL for core rendering and presentation. Uses Silk.NET for managed OpenGL bindings.
- **Event Loop**: Native implementation manages the main application event loop (see `Application_Run`).
- **Memory Management**: Uses a callback-based cleanup mechanism (`pix_data_free` in `PixData`) to safely release native memory from managed code.

## Native / Exported Entry Points
Native symbols are exported using the `_EXPORT_` marker, primarily defined in `include/CrystalCatalystLibrary/`.

| Symbol | File | Purpose | Notes |
|---|---|---|---|
| `_EXPORT_ Application_Init` | `include/CrystalCatalystLibrary/Application.h` | Initializes the application context with arguments. | |
| `_EXPORT_ Application_Run` | `include/CrystalCatalystLibrary/Application.h` | Starts the main event loop. | Blocking call. |
| `_EXPORT_ CrystalWindow_Create` | `include/CrystalCatalystLibrary/Windowing/CrystalWindow.h` | Creates a new managed window handle. | Returns `WindowHandle`. |
| `_EXPORT_ CrystalWindow_GLInit` | `include/CrystalCatalystLibrary/Windowing/CrystalWindow.h` | Initializes an OpenGL context for a window. | Default OpenGL initialization. |
| `_EXPORT_ CrystalWindow_PresentPix` | `include/CrystalCatalystLibrary/Windowing/CrystalWindow.h` | Presents raw `PixData` to the window. | |
| `_EXPORT_ CrystalWindow_ClipboardCopy` | `include/CrystalCatalystLibrary/Windowing/Clipboard.h` | Copies data to the system clipboard. | Uses `DataInterchange`. |
| `_EXPORT_ Pixels_ConvertPixels` | `include/CrystalCatalystLibrary/Platform.h` | Converts pixel data between specified formats. | |

## Managed Class Map

### Core & Interop
#### `CrystalWindow`
File: `Project/CrystalCatalystLibrary.net/CrystalCatalystLibrary.net/Generated/CrystalWindow.cs`
Purpose: Primary managed wrapper for native window instances, handling event dispatch and presentation.
Collaborates with: `Application`, `PixData`, `DataInterchange`.
Notes: Generated file. Uses `InstanceCache` to maintain the mapping between native pointers and managed objects.

#### `PixData`
File: `Project/CrystalCatalystLibrary.net/CrystalCatalystLibrary.net/PixData.cs`
Purpose: Structure representing raw pixel data, dimensions, and memory cleanup callbacks.
Collaborates with: `CrystalWindow`, `GLRenderer`, `CrystalSkia`.
Notes: Essential for cross-boundary image data transfer. Must be disposed to free native memory.

#### `DataInterchange` / `DragDropData`
File: `Project/CrystalCatalystLibrary.net/CrystalCatalystLibrary.net/Generated/DataInterchange.cs`
Purpose: Handles complex data transfer for Clipboard and Drag-and-Drop operations.
Collaborates with: `CrystalWindow`.

### OpenGL Subsystem
#### `GLRenderer`
File: `Project/CrystalCatalystLibrary.net/CrystalOpenGL/GLRenderer.cs`
Purpose: High-level utilities for rendering to OpenGL textures and capturing output as `PixData`.
Collaborates with: `GLTextureHelper`, `PixData`, `Silk.NET.OpenGL.GL`.
Notes: Manages FBO/RBO state and viewport restoration.

#### `GLTextureHelper`
File: `Project/CrystalCatalystLibrary.net/CrystalOpenGL/GLTextureHelper.cs`
Purpose: Provides utility methods for reading and writing pixels to/from OpenGL textures.
Collaborates with: `GLPixFormatMap`, `PixData`.

#### `GLHelper`
File: `Project/CrystalCatalystLibrary.net/CrystalOpenGL/GLHelper.cs`
Purpose: Low-level OpenGL utility functions (e.g., capability checks like `HasDSA`).

### Skia Subsystem
#### `CrystalSkia`
File: `Project/CrystalCatalystLibrary.net/CrystalSkia.net/CrystalSkia.cs`
Purpose: Entry point for Skia-based rendering integration.
Collaborates with: `SkiaSharp`, `PixData`.

#### `SkiaPixFormatMap`
File: `Project/CrystalCatalystLibrary.net/CrystalSkia.net/SkiaPixFormatMap.cs`
Purpose: Maps Crystal pixel format strings to `SKColorType`.

### Tests & Examples
#### `OpenGLCrystalTest`
File: `Project/CrystalCatalystLibrary.net/OpenGLCrystalTest/Program.cs`
Purpose: Example application demonstrating OpenGL rendering to a `CrystalWindow`.
Collaborates with: `CrystalWindow`, `GLRenderer`.

#### `test_client`
File: `Test/test_client.cpp`
Purpose: Native test client for verifying the C++ library functionality.
Collaborates with: `CrystalWindow`, `Application`.

## CrystalOptics Companion

`CrystalOptics/` is a screen capture companion built alongside `CrystalCatalystLibrary` via `add_subdirectory`. It depends on JWCEssentials and CrystalCatalystLibrary (for `PixData`) and produces `libCrystalOptics.so` / `CrystalOptics.dll`.

**Build integration:** `Dev/build_native.sh` and `Dev/build_managed.sh` cover both libraries. The companion is not a separate repo — it shares the git history and build scripts.

### Native exports (`CrystalOptics/include/CrystalOptics/CaptureAPI.h`)

| Symbol | Purpose | Notes |
|---|---|---|
| `Capture_GetDisplayCount()` | Returns number of hardware displays. | XRandR on Linux, EnumDisplayMonitors on Windows. |
| `Capture_GetDisplayInfo(index)` | Returns `DisplayInfo` (x, y, width, height, primary). | |
| `Capture_Desktop()` | Captures the full virtual desktop (most reliable default). | Portal on Wayland, XGetImage on X11, GDI on Windows. |
| `Capture_Display(index)` | Captures a specific display by index. | Falls back to desktop geometry if clamped. |
| `Capture_ActiveWindow()` | Captures the foreground application window. | |
| `Capture_Portal()` | Explicit XDG Desktop Portal capture (Wayland-safe). | Linux only; requires dbus-1. May show compositor dialog. |

All capture functions return `PixData` (`bgra:int8`). Caller must dispose.

### FacetCLI (`CrystalOptics/Project/CrystalOptics.net/FacetCLI/`)

CLI tool staged to `$NewAge/bin/` as `FacetCLI` (bash + bat dotnet wrappers).

```
FacetCLI list-displays
FacetCLI capture [--display N] [--desktop] [--portal] [--active-window]
                 [--bounds x,y,w,h] [--format webp|png|jpeg] [--quality 1-100]
                 [--grayscale] [--out base64|stdout|file] [--out-file PATH]
```

**`--bounds` is relative to the target** (display or window), not the virtual desktop. On Wayland, the portal returns the full desktop; `--display N` auto-crops to that display, and `--bounds` offsets within it.

**Platform behaviour:**

| Session | Path |
|---|---|
| X11 | `XGetImage` on root window. `--display` uses XRandR offsets. |
| Wayland (auto) | XDG portal → full desktop → auto-crop to requested display. |
| Wayland (`--portal`) | Explicit portal call; `--display` crops the result. |
| Windows | GDI `BitBlt` + `EnumDisplayMonitors`. |

**Extension point:** Add new capture modes in `CrystalOptics/Platform/<OS>/Capture_<OS>.cpp` and expose via `CaptureAPI.h` following the `_OPTICS_EXPORT_` convention.

## Architectural Seams
- **Managed/Native Interop**: Defined by `_EXPORT_` symbols in C++ and `Generated/` P/Invoke classes in C#. This boundary is bridged via the `CrystalCatalystLibrary` native DLL/SO.
- **Platform Abstraction**: Common native interfaces (e.g., `Application.h`) are implemented per-platform in `Platform/Linux/` and `Platform/Windows/`. A global `TheApplication` singleton dispatches calls to the appropriate platform implementation.
- **Pixel Currency**: `PixData` serves as the universal exchange format between rendering engines (OpenGL, Skia) and the windowing presentation system.
- **Generated Code Boundary**: The `Generated/` folders in the .NET project isolate the low-level P/Invoke wrappers from manual business logic.

## Extension Points
- **New Rendering Helpers**: Implement new rendering patterns in `CrystalOpenGL` or `CrystalSkia.net` following the `PixData` exchange pattern.
- **New Platform Support**: Create a new subdirectory in `Platform/` and implement the abstract interfaces defined in the core headers.
- **Native Exports**: Add new functional entry points to headers in `include/` using the `_EXPORT_` convention.
- **Managed Bindings**: Update `NewAgeGenerator.stub.sig` to include new native types or callbacks for automatic P/Invoke generation.

## Known Cautions
- **Single-Line Exports**: Maintenance of `_EXPORT_` symbols requires they stay on a single line for grep-friendliness.
- **OpenGL Context Ownership**: Always ensure the correct context is active via `CrystalWindow_GLMakeCurrent` before performing GL operations.
- **Memory Safety**: Be vigilant about `PixData` disposal. Native memory leaks will occur if the `pix_data_free` callback is not triggered.
- **Linux Environment**: The core library is tied to X11 (`CrystalApplication_X11.cpp`). CrystalOptics adds Wayland support via the XDG portal.
- **CrystalOptics Wayland**: Requires `dbus-1` and `libpng` at build time (detected via pkg-config). If absent, portal support is compiled out gracefully.

## Generated / Reflected / Derived Files
- **P/Invoke Bindings**: `Project/CrystalCatalystLibrary.net/CrystalCatalystLibrary.net/Generated/*.cs` are automatically generated.
- **Generator Config**: `NewAgeGenerator.stub.sig` (repo root) contains the signature and type mapping information used by the code generator. Do not modify the generated files manually if the generator is active.

## Maintenance Notes
- **Refresh Policy**: Update this document after adding major native entry points, new managed subsystems, or changing architectural seams.
- **Path Consistency**: Always use repo-relative paths.
- **Grep-Friendly**: Keep `_EXPORT_` entries single-line.
