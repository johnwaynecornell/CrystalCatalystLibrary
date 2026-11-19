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