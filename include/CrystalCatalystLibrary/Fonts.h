// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#ifndef FONTS_H
#define FONTS_H

#include "CrystalCatalystLibrary.h"

using namespace JWCEssentials;

namespace NewAge {
    _EXPORT_ bool CrystalCatalyst_Fonts_Has_MSCoreFonts(void (*callback)(utf8_string_struct OS, utf8_string_struct Instructions) = nullptr);
}
#endif //FONTS_H
