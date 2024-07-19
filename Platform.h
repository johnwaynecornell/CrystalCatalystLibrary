// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#ifndef PLATFORM_H
#define PLATFORM_H

#include <string>
#include <cstdint>

typedef float float32;
typedef double float64;

#include <cstddef>

#include "Under/Under.h"

// Key state flags compatible with both Windows and Linux
#define KEY_STATE_CONTROL 0x0008
#define KEY_STATE_SHIFT   0x0004
#define KEY_STATE_ALT     0x0010

struct PixData
{
    utf8_string_const pix_format;
    P_ELEMENTS(uint8_t) pix_data;
    size_t pix_data_length;
    int32_t width;
    int32_t height;

    bool (*pix_data_free)(P_ELEMENTS(uint8_t) pixdata);
};

#ifdef __cplusplus
extern "C" {
#endif

_EXPORT_ ReturnBuffer Pixels_ConvertPixels(utf8_string_const pixformat, utf8_string_const pixformat_dest, P_ELEMENTS(void)  pixdata, size_t pixdata_length, int32_t width, int32_t height);

#ifdef __cplusplus
}
#endif


#endif //PLATFORM_H
