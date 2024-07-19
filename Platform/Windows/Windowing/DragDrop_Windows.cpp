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

#define mod_header() "DragDrop_Windows:"


extern utf8_string_const BuiltinDataTypes[] = { "text/plain", "text/html", "text/file-uri", nullptr };
P_INSTANCE(DragDropData) current_drag_data = nullptr;

#ifndef CFSTR_HTML
#define CFSTR_HTML TEXT("HTML Format")
#endif

class FormatEtcEnumerator : public IEnumFORMATETC {
public:
    FormatEtcEnumerator(FORMATETC* fmt, int32_t count)
            : m_refs(1), m_count(count), m_index(0) {
        m_fmt = new FORMATETC[count];
        for (int32_t i = 0; i < count; ++i) {
            m_fmt[i] = fmt[i];
        }
    }

    ~FormatEtcEnumerator() {
        delete[] m_fmt;
    }

    STDMETHOD(QueryInterface)(REFIID riid, P_OUT(P_INSTANCE(void))  ppv) {
        if (riid == IID_IUnknown || riid == IID_IEnumFORMATETC) {
            *ppv = static_cast<IEnumFORMATETC*>(this);
            AddRef();
            return S_OK;
        }
        *ppv = nullptr;
        return E_NOINTERFACE;
    }

    STDMETHOD_(ULONG, AddRef)() {
        return InterlockedIncrement(&m_refs);
    }

    STDMETHOD_(ULONG, Release)() {
        ULONG refs = InterlockedDecrement(&m_refs);
        if (refs == 0) delete this;
        return refs;
    }

    STDMETHOD(Next)(ULONG celt, FORMATETC* rgelt, ULONG* pceltFetched) {
        ULONG fetched = 0;
        while (m_index < m_count && fetched < celt) {
            rgelt[fetched] = m_fmt[m_index];
            ++m_index;
            ++fetched;
        }
        if (pceltFetched) {
            *pceltFetched = fetched;
        }
        return (fetched == celt) ? S_OK : S_FALSE;
    }

    STDMETHOD(Skip)(ULONG celt) {
        if (m_index + celt < m_count) {
            m_index += celt;
            return S_OK;
        }
        return S_FALSE;
    }

    STDMETHOD(Reset)() {
        m_index = 0;
        return S_OK;
    }

    STDMETHOD(Clone)(IEnumFORMATETC** ppEnum) {
        FormatEtcEnumerator* clone = new FormatEtcEnumerator(m_fmt, m_count);
        clone->m_index = m_index;
        clone->AddRef();
        *ppEnum = clone;
        return S_OK;
    }

private:
    LONG m_refs;
    int32_t m_count;
    int32_t m_index;
    FORMATETC* m_fmt;
};

class SimpleDataObject : public IDataObject {
public:
    P_INSTANCE(DragDropData) drag_data;

    SimpleDataObject(P_INSTANCE(DragDropData) drag_data, FORMATETC* fmt, int32_t count, P_INSTANCE(WindowHandle) handle)
            : m_refs(1), m_count(count), m_handle(handle) {
        this->drag_data = drag_data;

        m_fmt = new FORMATETC[count];
        for (int32_t i = 0; i < count; ++i) {
            m_fmt[i] = fmt[i];
        }
    }

    ~SimpleDataObject() {
        delete[] m_fmt;
    }

    STDMETHOD(QueryInterface)(REFIID riid, P_OUT(P_INSTANCE(void))  ppv) {
        if (riid == IID_IUnknown || riid == IID_IDataObject) {
            *ppv = static_cast<IDataObject*>(this);
            AddRef();
            return S_OK;
        }
        *ppv = nullptr;
        return E_NOINTERFACE;
    }

    STDMETHOD_(ULONG, AddRef)() {
        return InterlockedIncrement(&m_refs);
    }

    STDMETHOD_(ULONG, Release)() {
        ULONG refs = InterlockedDecrement(&m_refs);
        if (refs == 0) delete this;
        return refs;
    }

    STDMETHOD(GetData)(FORMATETC* pFormatEtc, STGMEDIUM* pMedium) {
        utf8_string_const format = nullptr;
        P_INSTANCE(void) data_ptr = nullptr;
        size_t size = 0;

        if (m_handle) {
            if (pFormatEtc->cfFormat == CF_UNICODETEXT) {
                format = "text/plain";
            } else if (pFormatEtc->cfFormat == RegisterClipboardFormat(CFSTR_HTML)) {
                format = "text/html";
            } else if (pFormatEtc->cfFormat == CF_HDROP) {
                format = "text/file-uri";
            } else {
                // Unsupported format, return DV_E_FORMATETC
                return DV_E_FORMATETC;
            }

            m_handle->crystal_window->callbacks.on_drag_provide_chosen(m_handle, drag_data, format);
            DragDropData_Selection_Reveal(drag_data, &format, &data_ptr, &size);

            if (strcmp(format, "text/plain") == 0 || strcmp(format, "text/html") == 0) {
                pMedium->tymed = TYMED_HGLOBAL;
                std::string utf8_text = static_cast<utf8_string_const >(data_ptr);
                int32_t len = MultiByteToWideChar(CP_UTF8, 0, utf8_text.c_str(), -1, nullptr, 0);
                HGLOBAL hGlobal = GlobalAlloc(GMEM_MOVEABLE, (len + 1) * sizeof(WCHAR));
                if (hGlobal) {
                    LPWSTR lpszText = (LPWSTR)GlobalLock(hGlobal);
                    if (lpszText) {
                        MultiByteToWideChar(CP_UTF8, 0, utf8_text.c_str(), -1, lpszText, len);
                        GlobalUnlock(hGlobal);
                    }
                }
                pMedium->hGlobal = hGlobal;
                pMedium->pUnkForRelease = nullptr;
                return S_OK;
            } else if (strcmp(format, "text/file-uri") == 0) {
                // Parse URIs
                std::string uri_list(static_cast<utf8_string_const >(data_ptr), size);
                std::vector<std::wstring> files;
                size_t pos = 0;
                size_t new_pos;

                while ((new_pos = uri_list.find('\n', pos)) != std::string::npos) {
                    std::string uri = uri_list.substr(pos, new_pos - pos);
                    pos = new_pos + 1;
                    if (uri.empty()) continue;

                    // Convert URI to wide string
                    int32_t len = MultiByteToWideChar(CP_UTF8, 0, uri.c_str(), -1, nullptr, 0);
                    std::wstring ws(len, L'\0');
                    MultiByteToWideChar(CP_UTF8, 0, uri.c_str(), -1, &ws[0], len);
                    files.push_back(ws);
                }

                // Calculate size of the global memory block
                size_t total_size = sizeof(DROPFILES);
                for (const auto& file : files) {
                    total_size += (file.length() + 1) * sizeof(WCHAR);
                }
                total_size += sizeof(WCHAR); // Extra null terminator

                // Allocate global memory block
                HGLOBAL hGlobal = GlobalAlloc(GMEM_MOVEABLE, total_size);
                if (hGlobal) {
                    DROPFILES* pDropFiles = (DROPFILES*)GlobalLock(hGlobal);
                    if (pDropFiles) {
                        pDropFiles->pFiles = sizeof(DROPFILES);
                        pDropFiles->pt.x = 0;
                        pDropFiles->pt.y = 0;
                        pDropFiles->fNC = TRUE;
                        pDropFiles->fWide = TRUE;

                        // Copy file paths to the global memory block
                        LPWSTR pwsz = (LPWSTR)((LPBYTE)pDropFiles + sizeof(DROPFILES));
                        for (const auto& file : files) {
                            wcscpy(pwsz, file.c_str());

                            std::string text_data_ptr(file.begin(), file.end());

                            //std::cerr << mod_header() << "SimpleDataObject::GetData " << text_data_ptr  << " included in drop" << std::endl;

                            pwsz += file.length(); // Move the pointer to the next location after the null terminator
                        }
                        *pwsz = L'\0'; // Extra null terminator

                        GlobalUnlock(hGlobal);
                    }
                }
                pMedium->tymed = TYMED_HGLOBAL;
                pMedium->hGlobal = hGlobal;
                pMedium->pUnkForRelease = nullptr;

                return S_OK;
            }
        }

        return DV_E_FORMATETC;
    }

    STDMETHOD(GetDataHere)(FORMATETC* pFormatEtc, STGMEDIUM* pMedium) {
        return E_NOTIMPL;
    }

    STDMETHOD(QueryGetData)(FORMATETC* pFormatEtc) {
        for (int32_t i = 0; i < m_count; ++i) {
            if ((pFormatEtc->tymed & m_fmt[i].tymed) &&
                pFormatEtc->cfFormat == m_fmt[i].cfFormat &&
                pFormatEtc->dwAspect == m_fmt[i].dwAspect) {
                return S_OK;
            }
        }
        return DV_E_FORMATETC;
    }

    STDMETHOD(GetCanonicalFormatEtc)(FORMATETC* pFormatEtc, FORMATETC* pFormatEtcOut) {
        return E_NOTIMPL;
    }

    STDMETHOD(SetData)(FORMATETC* pFormatEtc, STGMEDIUM* pMedium, BOOL fRelease) {
        return E_NOTIMPL;
    }

    STDMETHOD(EnumFormatEtc)(DWORD dwDirection, IEnumFORMATETC** ppEnumFormatEtc) {
        if (dwDirection == DATADIR_GET) {
            *ppEnumFormatEtc = new FormatEtcEnumerator(m_fmt, m_count);
            return S_OK;
        }
        return E_NOTIMPL;
    }

    STDMETHOD(DAdvise)(FORMATETC* pFormatEtc, DWORD advf, IAdviseSink* pAdvSink, DWORD* pdwConnection) {
        return E_NOTIMPL;
    }

    STDMETHOD(DUnadvise)(DWORD dwConnection) {
        return E_NOTIMPL;
    }

    STDMETHOD(EnumDAdvise)(IEnumSTATDATA** ppEnumAdvise) {
        return E_NOTIMPL;
    }

private:
    LONG m_refs;
    int32_t m_count;
    FORMATETC* m_fmt;
    P_INSTANCE(WindowHandle) m_handle;
};

HRESULT CreateDataObject(P_INSTANCE(DragDropData) dta, FORMATETC* fmt, int32_t count, P_INSTANCE(WindowHandle) handle, IDataObject** ppDataObject) {
    *ppDataObject = new SimpleDataObject(dta, fmt, count, handle);
    return (*ppDataObject) ? S_OK : E_OUTOFMEMORY;
}

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
