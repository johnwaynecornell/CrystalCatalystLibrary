// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
// [ Entity: CrystalCatalyst.Optics.FacetCLI ]
// [ Architecture: .NET 10 / SkiaSharp / CrystalOptics native ]
// [ State Matrix: Dormant -> Invoked -> Processed -> Terminated ]

using CrystalCatalystLibrary.net;
using CrystalOptics.net;
using CrystalCatalyst.Optics.FacetCLI;

string? verb      = null;
int     display   = 0;
bool    desktop   = false;
bool    portal    = false;
bool    activeWin = false;
int[]?  bounds    = null;
string  format    = "webp";
int     quality   = 80;
bool    grayscale = false;
string  outMode   = "stdout";
string? outFile   = null;

var argv = Environment.GetCommandLineArgs();

for (int i = 1; i < argv.Length; i++)
{
    switch (argv[i])
    {
        case "capture":
        case "list-displays":
            verb = argv[i];
            break;
        case "--display":
            display = int.Parse(argv[++i]);
            break;
        case "--desktop":
            desktop = true;
            break;
        case "--portal":
            portal = true;
            break;
        case "--active-window":
            activeWin = true;
            break;
        case "--bounds":
            bounds = argv[++i].Split(',').Select(int.Parse).ToArray();
            if (bounds.Length != 4) Fail("--bounds requires x,y,w,h");
            break;
        case "--format":
            format = argv[++i].ToLowerInvariant();
            break;
        case "--quality":
            quality = int.Parse(argv[++i]);
            break;
        case "--grayscale":
            grayscale = true;
            break;
        case "--out":
            outMode = argv[++i].ToLowerInvariant();
            break;
        case "--out-file":
            outFile = argv[++i];
            break;
        default:
            Fail($"Unknown argument: {argv[i]}");
            break;
    }
}

if (verb == null) Fail("Usage: FacetCLI <capture|list-displays> [options]");

if (verb == "list-displays")
{
    int count = ScreenCapture.GetDisplayCount();
    Console.WriteLine("[");
    for (int d = 0; d < count; d++)
    {
        var info = ScreenCapture.GetDisplayInfo(d);
        string comma = d < count - 1 ? "," : "";
        Console.WriteLine($"  {{ \"index\": {info.Index}, \"x\": {info.X}, \"y\": {info.Y}, " +
                          $"\"width\": {info.Width}, \"height\": {info.Height}, " +
                          $"\"primary\": {info.IsPrimary.ToString().ToLower()} }}{comma}");
    }
    Console.WriteLine("]");
    return 0;
}

// verb == "capture"

// Detect Wayland — the native layer auto-routes through the portal, which
// returns the full virtual desktop. We track this so we can offset bounds.
bool onWayland = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"))
              || Environment.GetEnvironmentVariable("XDG_SESSION_TYPE") == "wayland";

// fullDesktopResult: the captured image covers the entire virtual desktop,
// so any display/window targeting must be resolved as an offset into it.
bool fullDesktopResult = portal || desktop || (onWayland && !desktop);

using PixData pix = portal       ? ScreenCapture.CapturePortal()
                  : desktop      ? ScreenCapture.CaptureDesktop()
                  : activeWin    ? ScreenCapture.CaptureActiveWindow()
                  :                ScreenCapture.CaptureDisplay(display);

if (!pix)
{
    if (onWayland)
        Fail("Capture failed on Wayland.\n" +
             "  The XDG portal was attempted but unavailable or cancelled.\n" +
             "  Ensure xdg-desktop-portal and a backend are running.\n" +
             "  You can also try: XDG_SESSION_TYPE=x11 FacetCLI capture --desktop");
    else
        Fail("Capture returned no data. The display server may not support framebuffer reads.");
}

// Compute the base rect: where in the image does the requested target start?
// X11 display/window captures are already cropped to the target (base = 0,0).
// Portal/desktop captures are full virtual desktop, so we resolve the offset.
int baseX = 0, baseY = 0, baseW = pix.width, baseH = pix.height;

if (fullDesktopResult && !desktop)
{
    if (!activeWin)
    {
        // Display-targeted capture: offset into the full desktop image.
        var info = ScreenCapture.GetDisplayInfo(display);
        if (info.Width > 0 && info.Height > 0)
        {
            baseX = info.X; baseY = info.Y;
            baseW = info.Width; baseH = info.Height;
        }
    }
    // activeWin + portal: window position unknown from managed side — use full desktop.
}

// Resolve --bounds as relative to the base rect, then clip to image dimensions.
int[]? effectiveBounds = null;
if (bounds != null)
{
    int bx = Math.Clamp(baseX + bounds[0], 0, pix.width  - 1);
    int by = Math.Clamp(baseY + bounds[1], 0, pix.height - 1);
    int bw = Math.Clamp(bounds[2], 1, pix.width  - bx);
    int bh = Math.Clamp(bounds[3], 1, pix.height - by);
    effectiveBounds = new[] { bx, by, bw, bh };
}
else if (fullDesktopResult && !desktop && (baseX != 0 || baseY != 0 || baseW != pix.width || baseH != pix.height))
{
    // Auto-crop to the target display region within the full desktop image.
    effectiveBounds = new[] { baseX, baseY, baseW, baseH };
}

byte[] encoded = ImageProcessor.Process(pix, format, quality, grayscale, effectiveBounds);

switch (outMode)
{
    case "base64":
        Console.Write(Convert.ToBase64String(encoded));
        break;
    case "stdout":
        using (var stdout = Console.OpenStandardOutput())
            stdout.Write(encoded, 0, encoded.Length);
        break;
    case "file":
        if (string.IsNullOrWhiteSpace(outFile))
            Fail("--out file requires --out-file <path>");
        File.WriteAllBytes(outFile!, encoded);
        Console.Error.WriteLine($"[FacetCLI] Written: {outFile}");
        break;
    default:
        Fail($"Unknown --out mode: {outMode}. Use base64 | stdout | file");
        break;
}

return 0;

static int Fail(string message)
{
    Console.Error.WriteLine($"[FacetCLI] ERROR: {message}");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  FacetCLI capture [--display N] [--desktop] [--portal] [--active-window]");
    Console.Error.WriteLine("           [--bounds x,y,w,h]");
    Console.Error.WriteLine("           [--format webp|png|jpeg] [--quality 1-100] [--grayscale]");
    Console.Error.WriteLine("           [--out base64|stdout|file] [--out-file PATH]");
    Console.Error.WriteLine();
    Console.Error.WriteLine("  FacetCLI list-displays");
    Environment.Exit(1);
    return 1;
}
