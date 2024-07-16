#ifndef FONTS_H
#define FONTS_H

#include "../CrystalCatalystLibrary.h"

extern "C"
{
    _EXPORT_ bool CrystalCatalyst_Fonts_Has_MSCoreFonts(void (*callback)(utf8_string_const OS, utf8_string_const Instructions) = nullptr);
}

#endif //FONTS_H
