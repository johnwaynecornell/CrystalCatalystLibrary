// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include "CrystalCatalystLibrary/CrystalCatalystLibrary.h"

#include <ios>

namespace NewAge {
    utf8_string_struct DragDropData_DragActionsString(DragActions actions)
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

        static thread_local std::string last_drag_actions_str;
        last_drag_actions_str = str;
        utf8_string_struct R = (char*)last_drag_actions_str.c_str();
        return R;
    }

    P_INSTANCE(DragDropData)  DragDropData_Create()
    {
        return new DragDropData();
    }

    void DataInterchange::provide_for_drag(P_INSTANCE(DataInterchange) data, utf8_string_struct format)
    {
        DragDropData *dta = (DragDropData*)data;
        dta->m_handle->crystal_window->callbacks.on_drag_provide_chosen(dta->m_handle, dta, format);

    }
}