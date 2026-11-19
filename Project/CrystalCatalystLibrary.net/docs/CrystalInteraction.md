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