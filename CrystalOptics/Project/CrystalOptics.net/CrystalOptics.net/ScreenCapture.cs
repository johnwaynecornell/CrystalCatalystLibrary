// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
using System.Runtime.InteropServices;
using CrystalCatalystLibrary.net;

namespace CrystalOptics.net;

[StructLayout(LayoutKind.Sequential)]
public struct DisplayInfo
{
    public int   Index;
    public int   X;
    public int   Y;
    public int   Width;
    public int   Height;
    [MarshalAs(UnmanagedType.U1)]
    public bool  IsPrimary;
}

public static class ScreenCapture
{
    public static int GetDisplayCount()
        => Imports.Capture_GetDisplayCount();

    public static DisplayInfo GetDisplayInfo(int index)
        => Imports.Capture_GetDisplayInfo(index);

    /// <summary>Capture the full virtual desktop (0,0,root_w,root_h) — most reliable.</summary>
    public static PixData CaptureDesktop()
        => Imports.Capture_Desktop();

    /// <summary>Capture a specific display by index.</summary>
    public static PixData CaptureDisplay(int index)
        => Imports.Capture_Display(index);

    /// <summary>Capture the foreground application window.</summary>
    public static PixData CaptureActiveWindow()
        => Imports.Capture_ActiveWindow();

    public static class Imports
    {
        [DllImport("CrystalOptics")]
        public static extern int Capture_GetDisplayCount();

        [DllImport("CrystalOptics")]
        public static extern DisplayInfo Capture_GetDisplayInfo(int display_index);

        [DllImport("CrystalOptics")]
        public static extern PixData Capture_Desktop();

        [DllImport("CrystalOptics")]
        public static extern PixData Capture_Display(int display_index);

        [DllImport("CrystalOptics")]
        public static extern PixData Capture_ActiveWindow();
    }
}
