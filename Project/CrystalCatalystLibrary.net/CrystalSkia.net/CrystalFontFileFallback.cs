using System;
using System.Collections.Generic;
using SkiaSharp;

namespace CrystalSkia.net;

/// <summary>
/// Resolves text against font files in caller-specified order. Paths may point to
/// bundled or system fonts; missing, unreadable and invalid files are ignored.
/// The returned typefaces are owned by this instance and remain valid until disposal.
/// </summary>
public sealed class CrystalFontFileFallback : IDisposable
{
    private readonly List<SKTypeface> typefaces = new();
    private readonly Dictionary<string, SKTypeface?> glyphCache = new();
    private bool disposed;

    public CrystalFontFileFallback(IEnumerable<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        foreach (string path in paths)
        {
            if (string.IsNullOrWhiteSpace(path)) continue;
            try
            {
                var typeface = SKTypeface.FromFile(path);
                if (typeface != null) typefaces.Add(typeface);
            }
            catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                // A missing or unreadable optional font must not prevent fallback.
            }
        }
    }

    /// <summary>Returns the first file typeface containing all glyphs, or null.</summary>
    public SKTypeface? FindTypeface(string text)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(text);
        if (glyphCache.TryGetValue(text, out var cached)) return cached;
        foreach (var typeface in typefaces)
        {
            using var font = new SKFont(typeface, 16f);
            if (font.ContainsGlyphs(text)) return glyphCache[text] = typeface;
        }
        glyphCache[text] = null;
        return null;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        foreach (var typeface in typefaces) typeface.Dispose();
        typefaces.Clear();
        glyphCache.Clear();
    }
}