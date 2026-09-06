#ifndef CRYSTALCATALYST_CLIPBOARD_WINDOWS_H
#define CRYSTALCATALYST_CLIPBOARD_WINDOWS_H

#include <string>
#include "CrystalCatalystLibrary/CrystalCatalystLibrary.h"

using namespace JWCEssentials;

namespace NewAge {

    void handleDataInterchangeError(P_INSTANCE(WindowHandle) handle, P_INSTANCE(DataInterchange) di, std::string message);

}

#endif //CRYSTALCATALYST_CLIPBOARD_WINDOWS_H
