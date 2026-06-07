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
using PixData pix = portal       ? ScreenCapture.CapturePortal()
                  : desktop      ? ScreenCapture.CaptureDesktop()
                  : activeWin    ? ScreenCapture.CaptureActiveWindow()
                  :                ScreenCapture.CaptureDisplay(display);

if (!pix)
{
    var wayland = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");
    var session = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE");
    if (!string.IsNullOrEmpty(wayland) || session == "wayland")
        Fail("Capture failed on Wayland.\n" +
             "  The XDG portal was attempted but unavailable or cancelled.\n" +
             "  Ensure xdg-desktop-portal and a backend are running.\n" +
             "  You can also try: XDG_SESSION_TYPE=x11 FacetCLI capture --desktop");
    else
        Fail("Capture returned no data. The display server may not support framebuffer reads.");
}

byte[] encoded = ImageProcessor.Process(pix, format, quality, grayscale, bounds);

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
