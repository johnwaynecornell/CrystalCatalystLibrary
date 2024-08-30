// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include "DragDrop_Windows.h"
#include "CrystalWindow_Windows.h"
#include "SimpleDataObject.h"
#include <shlobj.h>
#include <ole2.h>
#include <strsafe.h>
#include <iostream>
#include <string>
#include <vector>

#ifdef mod_header
#undef mod_header
#endif

#define mod_header() "DragDrop_Windows:"

using namespace JWCEssentials;

namespace NewAge {

void CrystalWindow_Windows::RegisterDragTarget() {
    RegisterDragDrop(hwnd, static_cast<IDropTarget*>(this));
}

// Conversion from DragActions to DROPEFFECT
DWORD drag_actions_to_dropeffect(DragActions actions) {
    DWORD effect = DROPEFFECT_NONE;

    if (actions & DRAG_OPERATION_COPY) {
        effect |= DROPEFFECT_COPY;
    }
    if (actions & DRAG_OPERATION_MOVE) {
        effect |= DROPEFFECT_MOVE;
    }
    if (actions & DRAG_OPERATION_LINK) {
        effect |= DROPEFFECT_LINK;
    }

    return effect;
}

// Conversion from DragStatus to HRESULT
HRESULT drag_status_to_hresult(const DragStatus& status) {
    return status.accept ? S_OK : E_FAIL;
}

/*
HRESULT drag_status_read(std::string msg, DragStatus status, DWORD *dwEffect)
{

}
*/

HRESULT __stdcall CrystalWindow_Windows::DragEnter(IDataObject* pDataObject, DWORD grfKeyState, POINTL pt, DWORD* pdwEffect) {
    *pdwEffect = DROPEFFECT_NONE;

    current_drag_data = DragDropData_Create();
    current_drag_data->context = pDataObject;
	current_drag_data->selection_type = DataInterchange::E_DND;

    current_drag_data->m_handle = myHandle;

    DataInterchange_ReadFormats(current_drag_data, pDataObject);

    current_drag_data->action_selections = static_cast<DragActions>(DROPEFFECT_COPY | DROPEFFECT_MOVE | DROPEFFECT_LINK);

    // Map Windows key state to custom key state
    uint32_t key_state = 0;// map_windows_key_state(grfKeyState);

    if (callbacks.on_drag_receive_enter) {
        callbacks.on_drag_receive_enter(myHandle, current_drag_data);

        *pdwEffect = drag_actions_to_dropeffect(current_drag_data->status.action);

        return drag_status_to_hresult(current_drag_data->status);
    }

    return E_FAIL;
}

HRESULT __stdcall CrystalWindow_Windows::DragOver(DWORD grfKeyState, POINTL pt, DWORD* pdwEffect) {
    *pdwEffect = DROPEFFECT_COPY;

    uint32_t key_state = map_windows_key_state(grfKeyState);
    int32_t x = pt.x;
    int32_t y = pt.y;

    if (callbacks.on_drag_receive_motion) {
        callbacks.on_drag_receive_motion(myHandle, current_drag_data, x, y, key_state);

        *pdwEffect = drag_actions_to_dropeffect(current_drag_data->status.action);
        return drag_status_to_hresult(current_drag_data->status);
    }

    return S_OK;
}

HRESULT __stdcall CrystalWindow_Windows::DragLeave() {

    if (callbacks.on_drag_receive_leave) {
        callbacks.on_drag_receive_leave(myHandle, current_drag_data);
    }

    if (current_drag_data) {
        DataInterchange_Free(current_drag_data);
        current_drag_data = nullptr;
    }

    return S_OK;
}

HRESULT __stdcall CrystalWindow_Windows::Drop(IDataObject* pDataObject, DWORD grfKeyState, POINTL pt, DWORD* pdwEffect) {
    *pdwEffect = DROPEFFECT_COPY;

    utf8_string_struct format;
    format = callbacks.on_drag_receive_select(myHandle, current_drag_data);

    if (format != nullptr) {
        DataInterchange_Select(current_drag_data, format);

        //if (callbacks.on_drag_receive_drop) {
        //    callbacks.on_drag_receive_drop(myHandle, current_drag_data);

            *pdwEffect = drag_actions_to_dropeffect(current_drag_data->status.action);
            return drag_status_to_hresult(current_drag_data->status);
        //}
    }

    return S_OK;
}

HRESULT __stdcall CrystalWindow_Windows::QueryContinueDrag(BOOL fEscapePressed, DWORD grfKeyState) {
    if (fEscapePressed) return DRAGDROP_S_CANCEL;
    if (!(grfKeyState & MK_LBUTTON)) return DRAGDROP_S_DROP;
    return S_OK;
}

HRESULT __stdcall CrystalWindow_Windows::GiveFeedback(DWORD dwEffect) {
    return DRAGDROP_S_USEDEFAULTCURSORS;
}

void CrystalWindow_Windows::DragStart(P_INSTANCE(DragDropData)  data, int32_t x, int32_t y) {
    data->m_handle = myHandle;
    DataInterchange_CreateContext(data);
    data->provide_chosen = DataInterchange::provide_for_drag;

    IDataObject *pDataObject = (IDataObject *) data->context;
    DWORD dwEffect = drag_actions_to_dropeffect(data->action_selections);

    DWORD _dwEffect;
    HRESULT_IsError(DoDragDrop(pDataObject, static_cast<IDropSource *>(this), dwEffect, &_dwEffect), "DoDragDrop");
    pDataObject->Release();
}

}