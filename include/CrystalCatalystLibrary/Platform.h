// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#ifndef PLATFORM_H
#define PLATFORM_H

#include <cstdint>

typedef float float32;
typedef double float64;

#include <cstddef>

using namespace JWCEssentials;

namespace NewAge {
    // Key state flags compatible with both Windows and Linux
#define KEY_STATE_CONTROL 0x0008
#define KEY_STATE_SHIFT   0x0004
#define KEY_STATE_ALT     0x0010

    struct PixData
    {
        utf8_string_struct pix_format;
        P_ELEMENTS(uint8_t) pix_data;
        size_t pix_data_length;
        int32_t width;
        int32_t height;

        bool (*pix_data_free)(P_ELEMENTS(uint8_t) pixdata);

        operator bool();
    };

    _EXPORT_ PixData Pixels_ConvertPixels(utf8_string_struct pixformat, utf8_string_struct pixformat_dest, P_ELEMENTS(void)  pixdata, size_t pixdata_length, int32_t width, int32_t height);
}

#endif //PLATFORM_H
