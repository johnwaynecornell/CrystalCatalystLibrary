// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#ifndef CRYSTALCATALYST_DRAGDROP_H
#define CRYSTALCATALYST_DRAGDROP_H
#include "DataInterchange.h"

typedef struct WindowHandle WindowHandle;

extern utf8_string_const BuiltinDataTypes[];

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

#ifdef __cplusplus
extern "C" {
#endif
    _EXPORT_ void CrystalWindow_RegisterDragTarget(P_INSTANCE(WindowHandle) window_handle);
    _EXPORT_ void CrystalWindow_DragStart(P_INSTANCE(WindowHandle) handle, P_INSTANCE(DragDropData)  data, int32_t x, int32_t y); // Implemented in Platform
    _EXPORT_ void CrystalWindow_DragChoose(P_INSTANCE(WindowHandle) handle, P_INSTANCE(DragDropData)  data, utf8_string_const fmt); // Implemented in Platform
    _EXPORT_ void DragActions_String(DragActions actions, utf8_string buffer, size_t length);
    _EXPORT_ P_INSTANCE(DragDropData)  DragDropData_Create();

#ifdef __cplusplus
}
#endif

inline std::string DragActions_AsString(DragActions actions) {
    utf8_string buf = new utf8_char[1024];
    DragActions_String(actions , buf, 1024);
    return std::string(buf);
}

#endif // CRYSTALCATALYST_DRAGDROP_H
