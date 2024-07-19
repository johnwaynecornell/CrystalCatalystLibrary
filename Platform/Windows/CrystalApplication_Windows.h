// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
//
// Created by johnw on 6/23/2024.
//

#ifndef CRYSTALAPPLICATION_WINDOWS_H
#define CRYSTALAPPLICATION_WINDOWS_H

#include "../../CrystalCatalystLibrary.h"
#include "Windowing/CrystalWindow_Windows.h"
#include "Platform.h"

class CrystalApplication_Windows : public CrystalApplication {
public:

    HINSTANCE hInstance;
    WNDCLASS wc;

    virtual void Init();

    virtual P_INSTANCE(WindowHandle) WindowCreate(int32_t width, int32_t height, utf8_string_const title);

    virtual void DispatchCycle();
    virtual bool HasMessage();

};

#endif //CRYSTALAPPLICATION_WINDOWS_H
