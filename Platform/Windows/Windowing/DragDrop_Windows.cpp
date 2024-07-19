// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include "DragDrop_Windows.h"
#include "CrystalWindow_Windows.h"
#include <shlobj.h>
#include <ole2.h>
#include <strsafe.h>
#include <iostream>
#include <string>
#include <vector>

#ifdef mod_header
#undef mod_header
#endif



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

HRESULT drag_status_read(std::string msg, DragStatus status, DWORD *dwEffect)
{

}

HRESULT __stdcall CrystalWindow_Windows::DragEnter(IDataObject* pDataObject, DWORD grfKeyState, POINTL pt, DWORD* pdwEffect) {
    *pdwEffect = DROPEFFECT_NONE;

    current_drag_data = DragDropData_Create();

    IEnumFORMATETC *E;

    pDataObject->EnumFormatEtc(DATADIR_GET, &E);

    ULONG fetched = 0;

    FORMATETC fmt;
    char name[1024];

    do {
        E->Next(1, &fmt, &fetched);
        if (fetched) {
            utf8_string_const my_type = nullptr;

            if (fmt.cfFormat == CF_UNICODETEXT) my_type = "text/plain";
            else if (fmt.cfFormat == RegisterClipboardFormat(CFSTR_HTML)) my_type = "text/html";
            else if (fmt.cfFormat == CF_HDROP) my_type = "text/file-uri";

            name[GetClipboardFormatNameA(fmt.cfFormat, name, 1023)] = 0;

            std::string my_t;
            if (my_type == nullptr) my_t = "nullptr";
            else my_t = (std::string) "\"" + my_type + "\"";

            std::cerr << mod_header() << "\ttype:" << my_t << "\tformat:" << fmt.cfFormat << "\tName:\"" << name << "\"" << std::endl;

            if (my_type != nullptr) DragDropData_FormatAdd(current_drag_data, my_type);
        }

    } while (fetched);

    E->Release();

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
        DragDropData_Free(current_drag_data);
        current_drag_data = nullptr;
    }

    return S_OK;
}

HRESULT __stdcall CrystalWindow_Windows::Drop(IDataObject* pDataObject, DWORD grfKeyState, POINTL pt, DWORD* pdwEffect) {
    *pdwEffect = DROPEFFECT_COPY;

    utf8_string_const format;
    callbacks.on_drag_receive_select(myHandle, current_drag_data, &format);

    FORMATETC fmt = { 0, nullptr, DVASPECT_CONTENT, -1, TYMED_HGLOBAL };

    if (format != nullptr) {
        std::string f = format;
        if (f == "text/plain") fmt.cfFormat = CF_UNICODETEXT;
        else if (f == "text/html") fmt.cfFormat = RegisterClipboardFormat(CFSTR_HTML);
        else if (f == "text/file-uri") fmt.cfFormat = CF_HDROP;

        STGMEDIUM stg;
        HRESULT hr = pDataObject->GetData(&fmt, &stg);

        if (SUCCEEDED(hr)) {
            if (f == "text/plain" || f == "text/html") {
                LPWSTR lpszText = static_cast<LPWSTR>(GlobalLock(stg.hGlobal));
                if (lpszText != nullptr) {
                    std::wstring ws(lpszText);
                    std::string text_data(ws.begin(), ws.end());
                    GlobalUnlock(stg.hGlobal);

                    DragDropData_Selection_Set(current_drag_data, format, (P_INSTANCE(void))text_data.c_str(), text_data.length() + 1);
                }
            } else if (f == "text/file-uri") {
                HDROP hDrop = static_cast<HDROP>(GlobalLock(stg.hGlobal));
                if (hDrop != nullptr) {
                    uint32_t  fileCount = DragQueryFile(hDrop, 0xFFFFFFFF, nullptr, 0);

                    std::cout << "drop of " << fileCount << " files" << std::endl;

                    std::string uri = "";
                    for (uint32_t  i = 0; i < fileCount; i++) {
                        WCHAR filePath[MAX_PATH];
                        if (DragQueryFileW(hDrop, i, filePath, MAX_PATH)) {
                            std::wstring ws(filePath);
                            std::string filePathStr(ws.begin(), ws.end());
                            std::cout << "File Path: " << filePathStr << std::endl; // Debug log
                            uri += filePathStr + "\n";
                        }
                    }
                    GlobalUnlock(stg.hGlobal);

                    DragDropData_Selection_Set(current_drag_data, "text/file-uri", (P_INSTANCE(void) ) uri.c_str(),
                                               uri.length() + 1);
                }
            }
            ReleaseStgMedium(&stg);
        }

        if (callbacks.on_drag_receive_drop) {
            callbacks.on_drag_receive_drop(myHandle, current_drag_data);

            *pdwEffect = drag_actions_to_dropeffect(current_drag_data->status.action);
            return drag_status_to_hresult(current_drag_data->status);
        }
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
    IDataObject* pDataObject = nullptr;
    SingleLinkNode<FORMATETC> head = {};
    SingleLinkNode<FORMATETC>* current = &head;

    int32_t n_Format = 0;

    for (P_INSTANCE(DragDropData::Node)  n = data->data_head.next; n != nullptr; n = n->next) {
        if (strcmp(n->type, "text/plain") == 0) {
            current = current->next = new SingleLinkNode<FORMATETC>();
            memset(&current->member, 0, sizeof(current->member));

            current->member.cfFormat = CF_UNICODETEXT;
            current->member.ptd = nullptr;
            current->member.dwAspect = DVASPECT_CONTENT;
            current->member.lindex = -1;
            current->member.tymed = TYMED_HGLOBAL;

            n_Format++;
        } else if (strcmp(n->type, "text/html") == 0) {
            current = current->next = new SingleLinkNode<FORMATETC>();
            memset(&current->member, 0, sizeof(current->member));

            current->member.cfFormat = RegisterClipboardFormat(CFSTR_HTML);
            current->member.ptd = nullptr;
            current->member.dwAspect = DVASPECT_CONTENT;
            current->member.lindex = -1;
            current->member.tymed = TYMED_HGLOBAL;

            n_Format++;
        } else if (strcmp(n->type, "text/file-uri") == 0) {
            current = current->next = new SingleLinkNode<FORMATETC>();
            memset(&current->member, 0, sizeof(current->member));

            current->member.cfFormat = CF_HDROP;
            current->member.ptd = nullptr;
            current->member.dwAspect = DVASPECT_CONTENT;
            current->member.lindex = -1;
            current->member.tymed = TYMED_HGLOBAL;

            n_Format++;
        }
    }

    if (n_Format != 0) {
        FORMATETC* fmt = new FORMATETC[n_Format];

        SingleLinkNode<FORMATETC>* n;

        int32_t I = 0;
        for (SingleLinkNode<FORMATETC>* i = head.next; i != nullptr; i = n) {
            n = i->next;
            fmt[I++] = i->member;
            delete i;
        }

        DWORD dwEffect = drag_actions_to_dropeffect(data->action_selections);

        /*
        if (data->action_selections & DRAG_OPERATION_COPY) dwEffect |= DROPEFFECT_COPY;
        if (data->action_selections & DRAG_OPERATION_MOVE) dwEffect |= DROPEFFECT_MOVE;
        if (data->action_selections & DRAG_OPERATION_LINK) dwEffect |= DROPEFFECT_LINK;
        */

        HRESULT_IsError(CreateDataObject(data, fmt, n_Format, myHandle, &pDataObject), "CreateDataObject");

        IDropSource* pDropSource = nullptr;
        DWORD _dwEffect;
        HRESULT_IsError(DoDragDrop(pDataObject, static_cast<IDropSource*>(this), dwEffect, &_dwEffect), "DoDragDrop");
        pDataObject->Release();
    }
}
