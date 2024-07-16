#ifndef CRYSTALCATALYST__CRYSTALLIBRARY_H
#define CRYSTALCATALYST__CRYSTALLIBRARY_H

#include "Under/Under.h"
#include "Synchronization/CrystalSync.h"
#include "Windowing/CrystalWindow.h"
#include "Fonts/Fonts.h"
#include "Application.h"
#include "Platform.h"
#include "TLS.h"

#include <atomic>

#include "ErrorSystem.h"

class CrystalCatalystLibrary {
public:
    std::atomic<int> initializationCount;
    bool Initialize();
    bool Free();

    P_INSTANCE(TLS) errors;
};

extern P_INSTANCE(CrystalCatalystLibrary)  TheCrystalCatalystLibrary;
extern P_INSTANCE(CrystalCatalystMutex)  TheCrystalCatalystMutex;

extern "C"
{
_EXPORT_ bool CrystalCatalystLibrary_Initialize();
_EXPORT_ bool CrystalCatalystLibrary_Close();
_EXPORT_ bool StartingWith(utf8_string_const prefix, utf8_string_const str);

}

#endif //CRYSTALCATALYST__CRYSTALLIBRARY_H
