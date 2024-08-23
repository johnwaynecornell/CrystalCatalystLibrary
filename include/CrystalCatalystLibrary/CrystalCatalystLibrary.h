// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#ifndef CRYSTALCATALYST__CRYSTALLIBRARY_H
#define CRYSTALCATALYST__CRYSTALLIBRARY_H

#include "JWCEssentials/JWCEssentials.h"

#ifdef BUILD_CRYSTALCATALYSTLIBRARY
    #define _EXPORT_ __EXPORT__
    #define _CLASSEXPORT_ __CLASSEXPORT__
#else
    #define _EXPORT_ __IMPORT__
    #define _CLASSEXPORT_ __CLASSIMPORT__
#endif

#include "Platform.h"
#include "Synchronization/CrystalSync.h"
#include "Windowing/CrystalWindow.h"
#include "Fonts.h"
#include "Application.h"
#include "Platform.h"
#include "TLS.h"

#include <atomic>

#include "ErrorSystem.h"

using namespace JWCEssentials;

namespace NewAge {
    _CLASSEXPORT_ class CrystalCatalystLibrary {
    public:
        std::atomic<int> initializationCount;
        _CLASSEXPORT_ bool Initialize();
        _CLASSEXPORT_ bool Free();

        P_INSTANCE(TLS) errors;
    };

    _CLASSEXPORT_ extern P_INSTANCE(CrystalCatalystLibrary)  TheCrystalCatalystLibrary;
    _CLASSEXPORT_ extern P_INSTANCE(CrystalCatalystMutex)  TheCrystalCatalystMutex;

    _EXPORT_ bool CrystalCatalystLibrary_Initialize();
    _EXPORT_ bool CrystalCatalystLibrary_Close();
    _EXPORT_ bool StartingWith(utf8_string_struct prefix, utf8_string_struct str);
}

#undef _EXPORT__
#undef _CLASSEXPORT_

#endif //CRYSTALCATALYST__CRYSTALLIBRARY_H
