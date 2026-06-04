# CrystalCatalystLibrary.net Solution

This solution contains the .NET bindings and integration libraries for CrystalCatalyst, along with several examples and test applications.

## Core Libraries

- **CrystalCatalystLibrary.net**: The primary .NET binding for the native CrystalCatalyst library. It provides the base `PixData` and windowing abstractions.
- **CrystalSkia.net**: Integration with SkiaSharp. Provides adapters for `PixData` to `SKBitmap` and `SKImage`, and high-level conversion utilities via `SkiaConvert`.
- **CrystalOpenGL**: OpenGL integration library. Provides helpers for OpenGL context management, texture creation from `PixData`, and Silk.NET bridges.

## Examples and Tests

- **OpenGLCrystalTest**: A basic test application for OpenGL integration.
- **OpenGLCrystalTest_Texture**: A comprehensive example demonstrating the integration of Skia, PixData, and OpenGL. Shows how to create animated textures by drawing with Skia onto `PixData` buffers and uploading them to OpenGL.
- **OpenGLCrystalTest_Ribbon**: An advanced OpenGL example showcasing ribbon/trail rendering effects with
  CrystalCatalyst integration.
- **SvgIconAnimation**: Demonstrates SVG rendering and animation using SkiaSharp within the CrystalCatalyst environment.
- **TestWindow**: A general-purpose test window application for verifying core library functionality.
- **MessageWindow**: A simple example/utility, for displaying message windows, supporting ansi escape codes.
