// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#ifndef CRYSTALCATALYST_LINUX_DRAGDROP_H
#define CRYSTALCATALYST_LINUX_DRAGDROP_H

#include "CrystalWindow_X11.h"

using namespace JWCEssentials;

namespace NewAge {
    // Function to convert DragActions to X11 int64_t
    int64_t drag_actions_to_xint64_t(DragActions actions, P_INSTANCE(Display)  display);

    // Function to convert X11 int64_t to DragActions
    DragActions xint64_t_to_drag_actions(int64_t xint64_t, P_INSTANCE(Display)  display);
}

#endif //CRYSTALCATALYST_DRAGDROP_H
