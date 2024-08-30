// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.

#ifndef PLATFORM_LINUX_H
#define PLATFORM_LINUX_H
#include <X11/Xlib.h>
#include <X11/Xutil.h>
#include <X11/Xresource.h>
#include <GL/glx.h>

#include <string>
#include "CrystalCatalystLibrary/CrystalCatalystLibrary.h"

using namespace JWCEssentials;

namespace NewAge {
    std::string get_distro_name();
    bool is_rpm_based();

    utf8_string_struct XGetAtomName_struct(Display *display, Atom A);

    P_INSTANCE(CrystalApplication) platform_initialize();
}
#endif //PLATFORM_H
