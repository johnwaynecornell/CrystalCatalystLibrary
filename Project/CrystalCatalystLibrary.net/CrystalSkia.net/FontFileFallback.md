# Ordered font-file fallback

`CrystalFontFileFallback` accepts font file paths in priority order. Paths are
supplied by the application, so bundled fonts and platform-specific locations
can be used without hard-coded operating-system paths. Missing or invalid files
are ignored. `FindTypeface(text)` returns the first loaded face containing all
glyphs in `text`, or `null` if none does. Returned typefaces belong to the
resolver and must not be disposed separately.

For `AnsiSkiaRenderer`, configure the resolver before parsing and drawing:

```csharp
using var fallback = new CrystalFontFileFallback(fontPaths);
var renderer = new AnsiSkiaRenderer { FontFileFallback = fallback };
// Parse, measure, and draw while fallback remains alive.
```

The renderer checks files first, then its existing installed-family fallback.
Keep the resolver alive throughout all use of the renderer and its cached fonts.
Changing fonts after parsing requires parsing again to recompute dimensions.
The resolver does not discover file paths, perform text shaping, or split text
across fonts; consumers choose their own text segments. The ANSI renderer's
existing character-based rendering does not provide complex-script shaping.