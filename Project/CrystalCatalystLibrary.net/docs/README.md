CrystalCatalystLibrary .NET
===========================

CrystalCatalystLibrary is a lightweight native windowing and presentation host with generated .NET bindings.

The library is intended to provide a small cross-platform bridge between native platform windows and higher-level custom UI or compositing systems.

It is not intended to replace a full widget toolkit such as GTK, Qt, WinUI, or Cocoa. Instead, it provides the lower-level services needed by a custom-rendered UI framework.

Core Responsibilities
---------------------

*   Window creation and lifetime
*   Draw callbacks
*   Resize and input callbacks
*   Clipboard and drag/drop plumbing
*   Raw pixel presentation
*   Optional rendering integration through libraries such as SkiaSharp

Design Goal
-----------

The main goal is to keep the host layer small and predictable.

A higher-level UI system should own:

*   Layout
*   Widgets
*   Styling
*   Scene graph and composition
*   Invalidation
*   Animation
*   Input routing
*   Accessibility policy

CrystalCatalystLibrary should own:

*   Native windows
*   Platform event dispatch
*   Native callbacks
*   Pixel or surface presentation
*   Platform data-transfer boundaries

This separation allows a custom composited UI to move between backends without being tied to a specific desktop toolkit lifecycle.

Keyboard Callbacks
------------------

`OnKeyDown` and `OnKeyUp` use the existing `KeyCode` enum values for named keys
on both Windows and Linux. These values follow X11 keysyms; for example,
`KeyCode.Escape` is `0xFF1B`, including on Windows. Compare against the enum
instead of Windows virtual-key values or control characters.

Windows translates virtual keys together with the message's scan code and
extended-key bit. This distinguishes left/right modifiers, keypad Enter, and
keypad navigation with NumLock off. Function keys, navigation, locks, and keypad
operators also use the shared values. System-key messages deliver callbacks and
still receive normal Windows default processing. Shift+Tab reports `KeyCode.Tab`
on both platforms; Shift is available through its own modifier-key callbacks.

Printable keys retain the legacy, layout-sensitive callback behavior (for
example, `s` versus `S`). These callbacks are not a composed-text or IME API.
Linux preserves keysyms, including Unicode-encoded keysyms; they are not all
Unicode code points. Windows retains its single UTF-16-unit printable result
and uses non-mutating `ToUnicodeEx` translation to avoid consuming dead-key
state a second time. This flag requires Windows 10 version 1607 or later.

The native `test_keycodes` target checks Windows mappings against the managed
enum on either host. `test_keycodes --mapping-only` needs no display; running
without arguments also exercises each host's key-down/up callback dispatch.
Linux event tests expect a US X11 layout (a fresh Xvfb display supplies one).
Windows event tests temporarily select a US layout for the calling thread and
restore its original layout afterward.

Pixel Presentation
------------------

Windows can present raw image data using `PresentImage`.

The pixel format string describes the channel order and data type:

    <CHANNELS>:<TYPE>

Examples:

    rgba:int8
    bgra:int8
    RGBA:float32
    RGBA:float64

Supported data types are expected to be:

*   `int8`
*   `float32`
*   `float64`

For example, a SkiaSharp bitmap created with `SKColorType.Rgba8888` can be presented as:

    rgba:int8

SkiaSharp Usage
---------------

SkiaSharp is a good fit for custom composited UIs because it provides a stable 2D rendering API with high-quality text, paths, images, gradients, and effects.

Applications using SkiaSharp should reference the managed package:

    <PackageReference Include="SkiaSharp" Version="3.119.2" />

Executable projects should also reference the native asset packages needed for their supported platforms:

    <PackageReference Include="SkiaSharp.NativeAssets.Linux" Version="3.119.2" />
    <PackageReference Include="SkiaSharp.NativeAssets.macOS" Version="3.119.2" />
    <PackageReference Include="SkiaSharp.NativeAssets.Win32" Version="3.119.2" />

Reusable libraries should usually avoid forcing all native assets on consumers. Instead, the final executable should choose which native SkiaSharp assets to include.

Architecture
------------

Typical usage:

    Native platform window
            ↓
    CrystalCatalystLibrary host callbacks
            ↓
    Application or UI framework
            ↓
    Skia/custom compositor/software renderer
            ↓
    PresentImage

Recommended layering:

    UI Framework
    ├── Layout
    ├── Styling
    ├── Controls
    ├── Input routing
    ├── Composition
    └── Rendering
    
    Host Layer
    ├── Window creation
    ├── Draw events
    ├── Keyboard/mouse events
    ├── Clipboard
    ├── Drag/drop
    └── Pixel presentation

Public Repository Notes
-----------------------

For a public Git repository:

*   Commit project files and source code.
*   Do not commit `bin/` or `obj/`.
*   Do not commit machine-specific absolute dependency paths.
*   Prefer NuGet `PackageReference` dependencies.
*   Document required native runtime dependencies.
*   Keep generated files clearly separated from hand-written files.

Generated bindings should remain in a dedicated generated directory so they can be replaced safely by future generator runs.

Status
------

This project is currently an experimental/revival host for custom composited UI work.

APIs may change as the native host boundary, pixel presentation model, and platform integrations mature.
