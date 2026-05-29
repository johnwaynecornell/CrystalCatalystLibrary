# CrystalCatalystLibrary

CrystalCatalystLibrary is a cross-platform native and managed windowing/rendering substrate.

It provides a native C++ window layer with a managed .NET-facing API for presenting pixels, setting icons and cursors, handling window lifecycle, and experimenting with OS-agnostic rendering surfaces.

The current direction is:

~~~text
Native platform window
  + exported C ABI
  + managed .NET wrapper
  + PixData interop
  + Skia/SVG rendering
  = CrystalCatalyst surface
~~~

## Current capabilities

CrystalCatalystLibrary currently includes support for:

- Window creation and lifecycle
- Pixel presentation
- Window icons
- Custom cursors
- Standard cursors
- Animated cursor/icon experiments
- Window title management
- Window size and location management
- Drag/drop and clipboard infrastructure
- Managed SkiaSharp rendering support
- SVG-to-PixData rendering through `CrystalSkia.net`

Platform-specific native implementations currently target Windows and Linux/X11.

## Architecture

CrystalCatalystLibrary is built around three cooperating layers.

### Native C++ layer

The native layer owns platform-specific windowing behavior.

The public C-facing ABI is exposed through `_EXPORT_` functions such as:

- `CrystalWindow_Create`
- `CrystalWindow_CreateSimple`
- `CrystalWindow_ApplicationRetain`
- `CrystalWindow_ApplicationRelease`
- `CrystalWindow_PresentPix`
- `CrystalWindow_PresentImage`
- `CrystalWindow_CursorPix`
- `CrystalWindow_SetCursor`
- `CrystalWindow_SetStandardCursor`
- `CrystalWindow_IconPix`
- `CrystalWindow_SetIcon`
- `CrystalWindow_SetSize`
- `CrystalWindow_GetSize`
- `CrystalWindow_SetLocation`
- `CrystalWindow_GetLocation`
- `CrystalWindow_SetTitle`
- `CrystalWindow_GetTitle`
- `CrystalWindow_GetDefaultStockIcon`
- `CrystalWindow_uptimeSeconds`
- `CrystalWindow_uptimeReset`

The exported API is the stable seam used by managed code and may also be used by native consumers.

### Managed .NET layer

The managed layer wraps the native exports and presents a .NET-friendly API.

Managed applications can create windows, assign callbacks, present pixel buffers, set window icons, set custom cursors, and work with native-backed window lifecycle functions through the managed `CrystalWindow` surface.

### PixData layer

`PixData` is the pixel handoff structure used between managed and native code.

It is used for:

- Window presentation
- Window icons
- Custom cursors
- Stock icon data
- Skia-rendered output

The common format currently used by the Skia renderer is:

~~~text
bgra:int8
~~~

## Skia and SVG rendering

`CrystalSkia.net` adds SkiaSharp and Svg.Skia support.

`SvgSkiaRenderer` renders SVG content into `PixData` so the same SVG asset can be used as:

- A window image
- A window icon
- A cursor
- An animated cursor frame
- An animated icon frame

Important renderer features include:

- `Matrix` transform support
- Optional fixed `Size`
- Crop mode
- Configurable `Border`
- Hotspot translation through `TranslateHotSpot`

### Cursor border note

Some platforms, especially Windows, may require extra transparent border space around custom cursor pixel data to prevent corruption, clipping, or rendering artifacts.

`SvgSkiaRenderer.Border` provides this breathing room when rendering cropped cursor assets.

## Managed example

~~~csharp
using CrystalCatalystLibrary.net;

Application.Init(args);

var wnd = CrystalWindow.Create(400, 300, "CrystalCatalyst Window");

wnd.OnDraw = handle =>
{
    // Render or provide PixData here.
    // wnd.PresentPix(ref pix);
};

wnd.OnClose = w =>
{
    w.ApplicationRelease();
};

wnd.ApplicationRetain();
wnd.Show(true);

Application.Run();
~~~

## SVG icon/cursor example

~~~csharp
using SkiaSharp;
using CrystalSkia.net;
using CrystalCatalystLibrary.net;

var svg = Crystal.SKSvgFromText(SvgSrc.svgCatalystRotor);

var renderer = new SvgSkiaRenderer
{
    Size = new SKSize(128, 128),
    Crop = false
};

var pix = renderer.RenderPix(svg);

wnd.IconPix(ref pix);

pix.Dispose();
~~~

For custom cursors:

~~~csharp
var cursorSvg = Crystal.SKSvgFromText(SvgSrc.svgCatalystCrystal);

var cursorRenderer = new SvgSkiaRenderer
{
    Crop = true,
    Border = 8
};

var cursorPix = cursorRenderer.RenderPix(cursorSvg);

var hotspot = new SKPoint(6, 4);
var translatedHotspot = cursorRenderer.TranslateHotSpot(hotspot);

wnd.CursorPix(
    ref cursorPix,
    (int)translatedHotspot.X,
    (int)translatedHotspot.Y
);
~~~

## Demo: SvgIconAnimation

`SvgIconAnimation` demonstrates the current managed/native rendering path:

~~~text
SVG asset
  -> SvgSkiaRenderer
  -> PixData
  -> PresentPix / IconPix / CursorPix
  -> native window
~~~

The demo displays an SVG rotor in the window, uses SVG-rendered pixel data as the window icon, and applies a custom cursor rendered from SVG.

## Dependency guidance

Managed projects should reference the appropriate .NET assemblies/packages instead of relying on ad hoc DLL copying when possible.

SkiaSharp and its native assets should be kept on a coherent version family. For larger workspaces, central package management through `Directory.Packages.props` is recommended.

A typical policy is:

~~~text
Use one SkiaSharp version family across CrystalCatalyst-managed projects.
Keep managed SkiaSharp and native SkiaSharp assets aligned.
Avoid major-version upgrades until intentionally tested.
~~~

## Build instructions

CrystalCatalystLibrary is a NewAge family repository and requires the foundational `JWCEssentials` library and the `NewAge` environment.

### Prerequisites

The recommended way to set up the environment is through the `newage_go.sh` bootstrap script found in [`JWCEssentials`](https://github.com/johnwaynecornell/JWCEssentials)`/Bash/`. This script handles cloning, workspace
configuration, and initial builds.

1.  **Define the NewAge environment variable**:
    ```bash
    export NewAge="$HOME/NewAge"
    ```
2.  **Ensure [`JWCEssentials`](https://github.com/johnwaynecornell/JWCEssentials) is present**:
    If starting fresh, use `newage_go.sh` to initialize the workspace and fetch dependencies.

    or
    ```bash
    export NewAge="$HOME/Home.NewAge"
    mkdir -p "$NewAge"
    cd "$NewAge"
    
    git clone https://github.com/johnwaynecornell/JWCEssentials
    JWCEssentials/configure.sh --newage "$(pwd)"
    
    ./in_this_context.sh Debug -- bash #this enters the context type exit to leave
    ``` 
    
### Fetching

Once within a NewAge context, you can fetch CrystalCatalystLibrary using:

```bash
newage_get.sh CrystalCatalystLibrary
newage_get_deps.sh
newage_all_configure.sh
```

### Building

If `JWCEssentials` has not been built yet, perform a coordinated build of the entire workspace:

```bash
newage_all_build_coordinated.sh
```

To build CrystalCatalystLibrary specifically, use the following commands depending on your target:

```bash
# Full coordinated build (native + managed)
newage_build_coordinated.sh CrystalCatalystLibrary

# Native C++ library only
newage_build_native.sh CrystalCatalystLibrary

# Managed .NET wrapper only
newage_build_managed.sh CrystalCatalystLibrary
```

### Platform notes

#### Linux/X11
Native builds require X11, Xcursor, and OpenGL development headers. Common packages include `libx11-dev`, `libxcursor-dev`, and `libgl1-mesa-dev`.

#### Windows
Native builds require a C++17 compiler (such as MSVC from Visual Studio 2022) and CMake. Windows OS-level double buffering is supported through GDI.

## License

MIT License. See `LICENSE`.