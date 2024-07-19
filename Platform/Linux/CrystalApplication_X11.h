// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#ifndef CRYSTALAPPLICATION_X11_H
#define CRYSTALAPPLICATION_X11_H

#include "../../CrystalCatalystLibrary.h"
#include "Windowing/CrystalWindow_X11.h"
#include "Platform.h"

class CrystalApplication_X11 : public CrystalApplication {
public:
    XContext windowContext = XUniqueContext();
    P_INSTANCE(Display)  globalDisplay = nullptr;  // Global display connection

    virtual void Init();

    virtual P_INSTANCE(WindowHandle) WindowCreate(int32_t width, int32_t height, utf8_string_const title);

    virtual void DispatchCycle();
    virtual bool HasMessage();
};


#endif //CRYSTALAPPLICATION_X11_H
