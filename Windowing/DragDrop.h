#ifndef CRYSTALCATALYST_DRAGDROP_H
#define CRYSTALCATALYST_DRAGDROP_H

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

class DragDropData {
public:
    class Node {
    public:
        utf8_string_const type;
        Node *next;
    };

    utf8_string_const selected_format;
    P_INSTANCE(void) selected_data;
    size_t selected_size;

    Node data_head;

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

    _EXPORT_ P_INSTANCE(DragDropData)  DragDropData_Create();
    _EXPORT_ void DragDropData_Free(P_INSTANCE(DragDropData) drag);

    _EXPORT_ P_INSTANCE(DragDropData::Node)  DragDropData_FormatAdd(P_INSTANCE(DragDropData) drop, utf8_string_const format);
    _EXPORT_ bool DragDropData_FormatExists(P_INSTANCE(DragDropData) data, utf8_string_const format);
    _EXPORT_ P_INSTANCE(DragDropData::Node) DragDropData_FormatEnum(P_INSTANCE(DragDropData) drop);
    _EXPORT_ P_INSTANCE(DragDropData::Node) DragDropData_FormatEnum_Next(P_INSTANCE(DragDropData::Node) node);
    _EXPORT_ void DragDropData_FormatEnum_Text(P_INSTANCE(DragDropData::Node) node, P_OUT(utf8_string_const) text);

    _EXPORT_ P_INSTANCE(DragDropData::Node) DragDropData_Items_FormatRemove(P_INSTANCE(DragDropData) drop, P_INSTANCE(DragDropData::Node) node);

    _EXPORT_ void DragDropData_Selection_Reveal(P_INSTANCE(DragDropData) drag, P_OUT(utf8_string_const) format, P_OUT(P_INSTANCE(void)) data, P_OUT(size_t) size);
    _EXPORT_ void DragDropData_Selection_Set(P_INSTANCE(DragDropData) drag, utf8_string_const format, P_INSTANCE(void) data, size_t size);

    _EXPORT_ void DragActions_String(DragActions actions, utf8_string buffer, size_t length);
#ifdef __cplusplus
}
#endif

inline std::string DragActions_AsString(DragActions actions) {
    utf8_string buf = new utf8_char[1024];
    DragActions_String(actions , buf, 1024);
    return std::string(buf);
}

#endif // CRYSTALCATALYST_DRAGDROP_H
