// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
using System.Runtime.InteropServices;
using CrystalCatalystLibrary.net;

namespace CrystalCatalyst.Optics.FacetCLI;

/// <summary>Hardware display geometry and identity.</summary>
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

/// <summary>P/Invoke bindings for CrystalCatalystLibrary screen capture exports.</summary>
public static class ScreenCapture
{
    public static int GetDisplayCount()
        => Imports.CrystalWindow_GetDisplayCount();

    public static DisplayInfo GetDisplayInfo(int displayIndex)
        => Imports.CrystalWindow_GetDisplayInfo(displayIndex);

    public static PixData CaptureDisplay(int displayIndex)
        => Imports.CrystalWindow_CaptureDisplay(displayIndex);

    public static PixData CaptureActiveWindow()
        => Imports.CrystalWindow_CaptureActiveWindow();

    public static class Imports
    {
        [DllImport("CrystalCatalystLibrary")]
        public static extern int CrystalWindow_GetDisplayCount();

        [DllImport("CrystalCatalystLibrary")]
        public static extern DisplayInfo CrystalWindow_GetDisplayInfo(int display_index);

        [DllImport("CrystalCatalystLibrary")]
        public static extern PixData CrystalWindow_CaptureDisplay(int display_index);

        [DllImport("CrystalCatalystLibrary")]
        public static extern PixData CrystalWindow_CaptureActiveWindow();
    }
}
