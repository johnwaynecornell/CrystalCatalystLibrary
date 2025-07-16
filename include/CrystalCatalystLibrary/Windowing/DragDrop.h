// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#ifndef CRYSTALCATALYST_DRAGDROP_H
#define CRYSTALCATALYST_DRAGDROP_H
#include "DataInterchange.h"

using namespace JWCEssentials;

namespace NewAge {
    typedef struct WindowHandle WindowHandle;

    extern utf8_string_struct BuiltinDataTypes[];

    enum DragActions {
        DRAG_OPERATION_NONE = 0,
        DRAG_OPERATION_COPY = 1 << 0,
        DRAG_OPERATION_MOVE = 1 << 1,
        DRAG_OPERATION_LINK = 1 << 2
    };

    struct DragStatus {
        bool accept;
        DragActions action;
    };

    class DragDropData : public DataInterchange{
    public:
        bool has_status = false;
        DragStatus status;
        DragActions action_selections = (DragActions)(DRAG_OPERATION_COPY | DRAG_OPERATION_MOVE | DRAG_OPERATION_LINK);
    };

    _EXPORT_ void CrystalWindow_RegisterDragTarget(P_INSTANCE(WindowHandle) window_handle);
    _EXPORT_ void CrystalWindow_DragStart(P_INSTANCE(WindowHandle) handle, P_INSTANCE(DragDropData)  data, int32_t x, int32_t y); // Implemented in Platform
    _EXPORT_ void CrystalWindow_DragChoose(P_INSTANCE(WindowHandle) handle, P_INSTANCE(DragDropData)  data, utf8_string_struct fmt); // Implemented in Platform
    _EXPORT_ utf8_string_struct DragDropData_DragActionsString(DragActions actions);
    _EXPORT_ P_INSTANCE(DragDropData)  DragDropData_Create();

}
#endif // CRYSTALCATALYST_DRAGDROP_H
