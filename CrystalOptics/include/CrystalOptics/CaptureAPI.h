// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#ifndef CRYSTALOPTICS_CAPTUREAPI_H
#define CRYSTALOPTICS_CAPTUREAPI_H

#include "CrystalCatalystLibrary/CrystalCatalystLibrary.h"

// CrystalOptics — screen capture companion to CrystalCatalystLibrary.
// Depends on JWCEssentials (via CrystalCatalystLibrary) and returns PixData.
// Platform: X11 (Linux), GDI (Windows).

#ifdef BUILD_CRYSTALOPTICS
    #define _OPTICS_EXPORT_ __EXPORT__
#else
    #define _OPTICS_EXPORT_ __IMPORT__
#endif

namespace CrystalOptics {

    using namespace NewAge;
    using namespace JWCEssentials;

    // Describes a hardware display/monitor.
    struct DisplayInfo {
        int32_t index;
        int32_t x;
        int32_t y;
        int32_t width;
        int32_t height;
        bool    is_primary;
    };

    // Returns the number of available hardware displays.
    _OPTICS_EXPORT_ int32_t Capture_GetDisplayCount();

    // Returns geometry and primary flag for a display by index.
    _OPTICS_EXPORT_ DisplayInfo Capture_GetDisplayInfo(int32_t display_index);

    // Captures the entire virtual desktop (full root window / primary screen).
    // Use this as the most reliable default — no monitor-offset arithmetic.
    // Returned PixData format: "bgra:int8". Caller must Dispose/free.
    _OPTICS_EXPORT_ PixData Capture_Desktop();

    // Captures a specific hardware display by index.
    // Falls back to Capture_Desktop() if the display geometry is clamped to zero.
    // Returned PixData format: "bgra:int8". Caller must Dispose/free.
    _OPTICS_EXPORT_ PixData Capture_Display(int32_t display_index);

    // Captures the foreground application window.
    // Returned PixData format: "bgra:int8". Caller must Dispose/free.
    _OPTICS_EXPORT_ PixData Capture_ActiveWindow();

}

#endif // CRYSTALOPTICS_CAPTUREAPI_H
