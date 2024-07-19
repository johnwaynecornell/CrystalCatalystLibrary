// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include "../../CrystalCatalystLibrary.h"
#include "DragDrop.h"

#include <ios>

void DragActions_String(DragActions actions, utf8_string buffer, size_t length)
{
    std::string str = "";

    if (actions == DRAG_OPERATION_NONE) str += "DRAG_OPERATION_NONE";
    else {

        bool f = true;

        if (actions & DRAG_OPERATION_COPY) {
            if (!f) str += " | ";
            str += "DRAG_OPERATION_COPY";
            f = false;
            actions = (DragActions) (actions - DRAG_OPERATION_COPY);
        }

        if (actions & DRAG_OPERATION_MOVE) {
            if (!f) str += " | ";
            str += "DRAG_OPERATION_MOVE";
            f = false;
            actions = (DragActions) (actions - DRAG_OPERATION_MOVE);
        }

        if (actions & DRAG_OPERATION_LINK) {
            if (!f) str += " | ";
            str += "DRAG_OPERATION_LINK";
            f = false;
            actions = (DragActions) (actions - DRAG_OPERATION_LINK);
        }

        if (actions != DRAG_OPERATION_NONE) {
            if (!f) str += " | ";
            char t[4096];
            sprintf(t, "(DragActions) 0x%X", actions);
            str += t;

            f = false;
        }
    }

    size_t S = str.length() + 1;

    if (!utf8_string_copy(str.c_str(), S, buffer, length))
        throw std::runtime_error("MEMORY DAMAGED");
}

utf8_string_const BultinDataTypes[] = {"Text", "Uris", "Image", nullptr};

P_INSTANCE(DragDropData)  DragDropData_Create() {
    return new DragDropData();
}

void clear_selection(P_INSTANCE(DragDropData) drag) {
    if (drag->selected_format != nullptr) {
        free((P_INSTANCE(void) ) drag->selected_format);
        drag->selected_format = nullptr;
    }

    if (drag->selected_data != nullptr) {
        free(drag->selected_data);
        drag->selected_data = nullptr;
    }

    drag->selected_size = 0;
}


void DragDropData_Free(P_INSTANCE(DragDropData) drag) {
    P_INSTANCE(DragDropData::Node) I;
    for (I = DragDropData_FormatEnum(drag); I != nullptr; I = I->next) {
        DragDropData_Items_FormatRemove(drag, I);
    }
    clear_selection(drag);
    delete drag;
}

 bool DragDropData_FormatExists(P_INSTANCE(DragDropData) data, utf8_string_const format) {
    std::string f = format;

    for (P_INSTANCE(DragDropData::Node) n = data->data_head.next; n != nullptr; n = n->next) {
        if (f == n->type) return true;
    }
    return false;
}


P_INSTANCE(DragDropData::Node)  DragDropData_FormatAdd(P_INSTANCE(DragDropData) drop, utf8_string_const format) {
    if (strcmp(format, "text/uri=list") == 0) __asm("int3");

    P_INSTANCE(DragDropData::Node) N = &drop->data_head;
    while (N->next != nullptr) N = N->next;

    N = N->next = new DragDropData::Node();
    utf8_string_replace_dup(format, &N->type);

    return N;
}

P_INSTANCE(DragDropData::Node) DragDropData_FormatEnum(P_INSTANCE(DragDropData) drop) {
    return drop->data_head.next;
}

P_INSTANCE(DragDropData::Node) DragDropData_FormatEnum_Next(P_INSTANCE(DragDropData::Node) node) {
    return node->next;
}

void DragDropData_FormatEnum_Text(P_INSTANCE(DragDropData::Node) node, P_OUT(utf8_string_const) text) {
    *text = node->type;
}

P_INSTANCE(DragDropData::Node) DragDropData_Items_FormatRemove(P_INSTANCE(DragDropData) drop, P_INSTANCE(DragDropData::Node) node) {
    P_INSTANCE(DragDropData::Node) I;

    for (I = DragDropData_FormatEnum(drop); I != nullptr && I->next != node; I = I->next){}

    if (I == nullptr) return nullptr;

    P_INSTANCE(DragDropData::Node) R = I->next;
    if (R->type != nullptr) free((utf8_string )R->type);

    I->next = R->next;

    return I->next;
}

void DragDropData_Selection_Reveal(P_INSTANCE(DragDropData) drag, P_OUT(utf8_string_const) format, P_OUT(P_INSTANCE(void)) data, P_OUT(size_t) size)
{
    if (format) *format = drag->selected_format;
    if (data) *data = drag->selected_data;
    if (size) *size = drag->selected_size;
}

void DragDropData_Selection_Set(P_INSTANCE(DragDropData) drag, utf8_string_const format, P_INSTANCE(void) data, size_t size) {

    utf8_string_replace_dup(format, &drag->selected_format);
    drag->selected_size = size;

    if (drag->selected_data != nullptr) {
        free(drag->selected_data);
        drag->selected_data = nullptr;
    }

    drag->selected_data = malloc(size+4);

    int32_t index=0;

    for (index=0; index < size; index++) static_cast<P_ELEMENTS(uint8_t) >(drag->selected_data)[index] = static_cast<P_ELEMENTS(uint8_t) >(data)[index];
    for (int32_t i=0; i<4; i++) static_cast<P_ELEMENTS(uint8_t) >(drag->selected_data)[index++] = 0;

}
