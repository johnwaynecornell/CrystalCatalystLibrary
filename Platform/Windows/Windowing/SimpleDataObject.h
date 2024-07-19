//
// Created by jwc on 7/19/24.
//

#ifndef SIMPLEDATAOBJECT_H
#define SIMPLEDATAOBJECT_H

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
#endif //SIMPLEDATAOBJECT_H
