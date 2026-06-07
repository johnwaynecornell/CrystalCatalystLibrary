// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#ifndef CRYSTALCATALYST_SCREENCAPTURE_H
#define CRYSTALCATALYST_SCREENCAPTURE_H

#include "../CrystalCatalystLibrary.h"

namespace NewAge {

    // Describes a hardware display/monitor in the system.
    struct DisplayInfo {
        int32_t index;
        int32_t x;
        int32_t y;
        int32_t width;
        int32_t height;
        bool    is_primary;
    };

    // Returns the number of hardware displays available.
    _EXPORT_ int32_t CrystalWindow_GetDisplayCount();

    // Returns geometry and primary status for a display by index.
    _EXPORT_ DisplayInfo CrystalWindow_GetDisplayInfo(int32_t display_index);

    // Captures the full contents of a hardware display.
    // Returned PixData format: "bgra:int8". Caller must Dispose/free.
    // display_index: 0 = primary. Use CrystalWindow_GetDisplayCount() to enumerate.
    _EXPORT_ PixData CrystalWindow_CaptureDisplay(int32_t display_index);

    // Captures the foreground application window.
    // Returned PixData format: "bgra:int8". Caller must Dispose/free.
    _EXPORT_ PixData CrystalWindow_CaptureActiveWindow();

}

#endif // CRYSTALCATALYST_SCREENCAPTURE_H
