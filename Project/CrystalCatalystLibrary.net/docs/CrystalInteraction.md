### Localized interaction and single-line editing

`ICrystal` describes a rectangular interaction target. `Bounds` is in window coordinates;
pointer positions delivered to the target are relative to its top-left corner.
`CrystalInteraction` registers targets with `Add` and `Remove`, hit-tests in reverse
registration order, focuses on primary-button presses, and captures primary drags until
release. Its `PointerDown`, `PointerMove`, `PointerUp`, `KeyDown`, and `TextInput`
methods are intended to be called by the host's window callbacks. The host tracks
modifier keys and supplies the `shift` and `control` flags. An outside primary press
clears focus. `SetFocus(null)` can also clear focus when the window loses focus.

`CrystalTextEditor` lives in the `CrystalSkia.net` assembly and implements `ICrystalSkia`,
which extends `ICrystal`. Construct it with bounds and an `SKFont`. `Text`, `Caret`, `SelectionStart`,
`SelectionLength`, `SelectedText`, `IsFocused`, and `Changed` expose editing state.
Indices are UTF-16 offsets at .NET text-element boundaries. The editor supports
pointer selection, arrows, Home/End, Shift selection, Backspace/Delete and Ctrl+A.
`TextInput` replaces the selection with committed text; control characters and line
separators are rejected with `ArgumentException`. `Changed` fires only when text changes;
hosts should invalidate their display after pointer, focus, and key events as well.

`ICrystal.OnIdle` accepts monotonic seconds; `CrystalInteraction.OnIdle` forwards it to
registered targets. The core library has no renderer dependency; `ICrystalSkia.OnDraw`
takes an `SKCanvas` in the same coordinate
space as `Bounds`. The editor saves, translates and clips the canvas before drawing its
background, selection, text and caret using `BackgroundColor`, `AccentColor` and `TextColor`.
The host supplies the editor's `Font` and may provide `TypefaceForGlyph` to resolve missing
glyphs; the host retains ownership of both. Drawing, hit testing, selection and caret use
the same per-codepoint font advances. SlideScramble uses MessageWindow's ordered file and
installed-family fallback. The editor vertically centers using the primary font metrics.
The Skia project depends on the core library, not the reverse. Measurements are assumed
to increase from left to right, so bidirectional layout, complex shaping, scrolling,
clipboard shortcuts, undo, and IME composition are not implemented. The current
`CrystalWindow` binding exposes named key callbacks but no committed Unicode text
callback; applications need a separate platform text-input source to call `TextInput`.
Do not treat named key codes as committed characters.

### Selection control

`CrystalSelectionControl` (in `CrystalSkia.net`) composes a `CrystalTextEditor` with a
scrollable string list and an ordered row of action buttons. It implements `ICrystalSkia`
so it can be registered with `CrystalInteraction` like any other target.

Construct it with bounds, an enumerable of strings, an initial text value, an ordered
collection of button labels, and an `SKFont`. The editor sits at the top; clicking a list
item copies that item's string into the editor without closing the control. The current
editor text is exposed as `ResultText`; `SelectedIndex` returns the matching list position
or -1 when the edited text no longer identifies a selection.

Keyboard navigation uses Up/Down for single-step movement, PageUp/PageDown for page
jumps, and Home/End for first/last. All navigation updates the editor text and keeps the
selected item visible by adjusting the scroll offset automatically.

Button activations raise `ButtonActivated` with a `SelectionButtonEventArgs` carrying the
button label, current text, and selected index. The control does not impose accept/cancel
semantics — the caller decides what each button means.

`CalculateHeight(itemCount, buttonCount, width)` is a static helper that returns the
minimum height required for a given configuration.

**Example — loading a session file:**

```csharp
var items = Directory.GetFiles(sessionsDir, "*.json")
    .Select(Path.GetFileNameWithoutExtension).ToList();
float height = CrystalSelectionControl.CalculateHeight(items.Count, 2, 360f);
var control = new CrystalSelectionControl(
    new RectangleF(0, 0, 360f, height), items, "session",
    new[] { "Load", "Cancel" }, font);
control.ButtonActivated += e =>
{
    if (e.ButtonLabel == "Cancel") popupHost.Cancel();
    else if (e.ButtonLabel == "Load")
        popupHost.Accept(e.CurrentText, e.SelectedIndex);
};
```

### Generic popup host

`CrystalPopupHost` (in `CrystalSkia.net`) displays any `ICrystalSkia` content as an in-window
modal overlay. It owns the popup lifecycle, routes localized pointer and keyboard input,
coordinates focus, and returns a `PopupResult` when closed.

Call `Show(content, windowBounds)` to display the popup centered within the window. The
popup handles Escape to cancel and forwards all other keys to the content. When the caller
wants to close the popup, it calls `Accept(resultText, selectedIndex)`, `Cancel()`, or
`Close()`. A `Closed` event fires with the result.

`PopupResult.Type` distinguishes `Accepted`, `Cancelled`, and `Closed`. Accepting an empty
string produces `PopupResultType.Accepted` with an empty `ResultText`, which is
distinguishable from `PopupResultType.Cancelled`.

After closing, the popup releases the content reference and removes focus. The host is
independent of `CrystalSelectionControl` — any `ICrystalSkia` target can be displayed.

**Example — showing a selection popup:**

```csharp
var popupHost = new CrystalPopupHost();
popupHost.Closed += result =>
{
    if (result.Type == PopupResultType.Accepted)
        LoadSession(result.ResultText);
};

var control = new CrystalSelectionControl(
    new RectangleF(0, 0, 360f, 200f), filenames, "",
    new[] { "Load", "Cancel" }, font);
control.ButtonActivated += e =>
{
    if (e.ButtonLabel == "Cancel") popupHost.Cancel();
    else popupHost.Accept(e.CurrentText, e.SelectedIndex);
};

popupHost.Show(control, new RectangleF(0, 0, 800f, 600f));
```

The popup draws a semi-transparent backdrop and a border around the content. Register it
with `CrystalInteraction` so pointer and keyboard events reach it, or route events
manually when the popup is visible.

### OpenGL PixData presenter

`GLPixDataPresenter` (in `CrystalOpenGL`) displays a CPU-side `PixData` buffer through an
OpenGL texture rendered as a full-screen quad. It is a reusable presentation component
that separates Skia drawing from framebuffer presentation.

**Architecture:**

Skia determines the pixels. `PixData` owns the CPU image. OpenGL presents that image.
The presenter owns its GPU resources (texture, shaders, VAO/VBO) but does not take
ownership of `PixData` — the caller retains ownership and lifetime.

**Usage:**

```csharp
// 1. Create a GL context on the window
wnd.GLInitVersioned(3, 3);
var gl = GL.GetApi(wnd.GLGetProcAddress);

// 2. Create the presenter
var presenter = new GLPixDataPresenter(gl);
presenter.SetViewport(width, height);

// 3. Each frame: draw to PixData with Skia, then present
PixData pixelBacking = FixedPixDataRenderer.CreateFixed(width, height, (canvas, info) =>
{
    // Draw game content with Skia
});

wnd.GLMakeCurrent();
presenter.Present(ref pixelBacking);
wnd.GLPresent();

// 4. On resize
presenter.SetViewport(newWidth, newHeight);

// 5. On shutdown
wnd.GLMakeCurrent();
presenter.Dispose();
```

**Pixel format support:**

The presenter uses `GLTextureHelper.WritePixels` for uploads, which supports all formats
mapped by `GLPixFormatMap`: `rgba:int8`, `bgra:int8`, `rgb:int8`, `bgr:int8`,
`rgba:float32`, `rgb:float32`, and their int16 variants. Unsupported formats are
automatically converted via fallback. SlideScramble's default `bgra:int8` format is
directly uploadable without conversion.

**Filtering:**

Set `presenter.Filter = TextureFilter.Nearest` for pixel-accurate presentation (default)
or `TextureFilter.Linear` for smooth scaling.

**Diagnostics:**

Enable `presenter.CollectDiagnostics = true` to collect per-frame timing data:
`TextureUploadMs` (CPU-side upload time), `DrawSubmissionMs` (CPU-side draw time),
and texture/viewport dimensions. These are CPU timings, not GPU completion times.

**Fallback:**

If GL context creation fails, fall back to `windowHandle.PresentPix(ref pixelBacking)`.
The presenter catches shader compilation errors and texture upload failures, setting
`LastError` for inspection.

**Relationship to OpenGLCrystalTest_Texture:**

The presenter follows the same GL context creation, shader compilation, and texture
management patterns established by `OpenGLCrystalTest_Texture`, but is designed as a
reusable component rather than a self-contained demo.

**Platform notes:**

- Requires OpenGL 3.3+ core profile.
- The GL context must be current on the calling thread.
- A non-GL window cannot be converted to a GL window after creation; call
  `GLInitVersioned` before the first draw.