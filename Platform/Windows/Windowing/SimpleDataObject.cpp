#include <vector>
#include <ShlObj_core.h>
#include <iostream>
#include "CrystalWindow_Windows.h"
#include "SimpleDataObject.h"

FormatEtcEnumerator::FormatEtcEnumerator(FORMATETC *fmt, int32_t count)
        : m_refs(1), m_count(count), m_index(0) {
    m_fmt = new FORMATETC[count];
    for (int32_t i = 0; i < count; ++i) {
        m_fmt[i] = fmt[i];
    }
}

FormatEtcEnumerator::~FormatEtcEnumerator() {
    delete[] m_fmt;
}

HRESULT FormatEtcEnumerator::QueryInterface(REFIID riid, P_OUT(P_INSTANCE(void))ppv) {
    if (riid == IID_IUnknown || riid == IID_IEnumFORMATETC) {
        *ppv = static_cast<IEnumFORMATETC *>(this);
        AddRef();
        return S_OK;
    }
    *ppv = nullptr;
    return E_NOINTERFACE;
}

ULONG FormatEtcEnumerator::AddRef() {
    return InterlockedIncrement(&m_refs);
}

ULONG FormatEtcEnumerator::Release() {
    ULONG refs = InterlockedDecrement(&m_refs);
    if (refs == 0) delete this;
    return refs;
}

HRESULT FormatEtcEnumerator::Next(ULONG celt, FORMATETC *rgelt, ULONG *pceltFetched) {
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

HRESULT FormatEtcEnumerator::Skip(ULONG celt) {
    if (m_index + celt < m_count) {
        m_index += celt;
        return S_OK;
    }
    return S_FALSE;
}

HRESULT FormatEtcEnumerator::Reset() {
    m_index = 0;
    return S_OK;
}

HRESULT FormatEtcEnumerator::Clone(IEnumFORMATETC **ppEnum) {
    FormatEtcEnumerator *clone = new FormatEtcEnumerator(m_fmt, m_count);
    clone->m_index = m_index;
    clone->AddRef();
    *ppEnum = clone;
    return S_OK;
}

SimpleDataObject::SimpleDataObject(P_INSTANCE(DataInterchange) dataInterchange, FORMATETC *fmt, int32_t count,
                                   P_INSTANCE(WindowHandle)handle)
        : m_refs(1), m_count(count), m_handle(handle) {
    this->dataInterchange = dataInterchange;

    m_fmt = new FORMATETC[count];
    for (int32_t i = 0; i < count; ++i) {
        m_fmt[i] = fmt[i];
    }
}

SimpleDataObject::~SimpleDataObject() {
    delete[] m_fmt;
}

HRESULT SimpleDataObject::QueryInterface(REFIID riid, P_OUT(P_INSTANCE(void))ppv) {
    if (riid == IID_IUnknown || riid == IID_IDataObject) {
        *ppv = static_cast<IDataObject *>(this);
        AddRef();
        return S_OK;
    }
    *ppv = nullptr;
    return E_NOINTERFACE;
}

ULONG SimpleDataObject::AddRef() {
    return InterlockedIncrement(&m_refs);
}

ULONG SimpleDataObject::Release() {
    ULONG refs = InterlockedDecrement(&m_refs);
    if (refs == 0) delete this;
    return refs;
}

HGLOBAL DataInterchange_MakeHGLOBAl(P_INSTANCE(DataInterchange) dataInterchange, utf8_string_const format, UINT *cfFormat)
{
    P_INSTANCE(void)data_ptr = nullptr;
    size_t size = 0;

    std::string  f = format;

    if (cfFormat != nullptr) {

        if (f == "text/plain") {
            *cfFormat = CF_UNICODETEXT;

        } else if (f == "text/html") {
            *cfFormat = RegisterClipboardFormat(CFSTR_HTML);
        } else if (f == "text/file-uri") {
            *cfFormat = CF_HDROP;
        } else {
            // Unsupported format, return DV_E_FORMATETC
            return nullptr;
        }
    }

    dataInterchange->provide_chosen(dataInterchange, format);
    DataInterchange_Selection_Reveal(dataInterchange, &format, &data_ptr, &size);


    HGLOBAL hGlobal = nullptr;

    if (strcmp(format, "text/plain") == 0 || strcmp(format, "text/html") == 0) {
        std::string utf8_text = static_cast<utf8_string_const >(data_ptr);
        int32_t len = MultiByteToWideChar(CP_UTF8, 0, utf8_text.c_str(), -1, nullptr, 0);
        hGlobal = GlobalAlloc(GMEM_MOVEABLE, (len + 1) * sizeof(WCHAR));
        if (hGlobal) {
            LPWSTR lpszText = (LPWSTR) GlobalLock(hGlobal);
            if (lpszText) {
                MultiByteToWideChar(CP_UTF8, 0, utf8_text.c_str(), -1, lpszText, len);
                GlobalUnlock(hGlobal);
            }
        }
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
        for (const auto &file: files) {
            total_size += (file.length() + 1) * sizeof(WCHAR);
        }
        total_size += sizeof(WCHAR); // Extra null terminator

        // Allocate global memory block
        hGlobal = GlobalAlloc(GMEM_MOVEABLE, total_size);
        if (hGlobal) {
            DROPFILES *pDropFiles = (DROPFILES *) GlobalLock(hGlobal);
            if (pDropFiles) {
                pDropFiles->pFiles = sizeof(DROPFILES);
                pDropFiles->pt.x = 0;
                pDropFiles->pt.y = 0;
                pDropFiles->fNC = TRUE;
                pDropFiles->fWide = TRUE;

                // Copy file paths to the global memory block
                LPWSTR pwsz = (LPWSTR) ((LPBYTE) pDropFiles + sizeof(DROPFILES));
                for (const auto &file: files) {
                    wcscpy(pwsz, file.c_str());

                    std::string text_data_ptr(file.begin(), file.end());

                    //std::cerr << mod_header() << "SimpleDataObject::GetData " << text_data_ptr  << " included in drop" << std::endl;

                    pwsz += file.length(); // Move the pointer to the next location after the null terminator
                }
                *pwsz = L'\0'; // Extra null terminator

                GlobalUnlock(hGlobal);
            }
        }
    }

    return hGlobal;
}

HRESULT SimpleDataObject::GetData(FORMATETC *pFormatEtc, STGMEDIUM *pMedium) {
    utf8_string_const format = nullptr;
    P_INSTANCE(void)data_ptr = nullptr;
    size_t size = 0;

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

    pMedium->tymed = TYMED_HGLOBAL;
    pMedium->hGlobal = DataInterchange_MakeHGLOBAl(dataInterchange, format, nullptr);
    if (!pMedium->hGlobal) return E_FAIL;

    pMedium->pUnkForRelease = nullptr;

    return S_OK;

    //return DV_E_FORMATETC;
}

HRESULT SimpleDataObject::GetDataHere(FORMATETC *pFormatEtc, STGMEDIUM *pMedium) {
    return E_NOTIMPL;
}

HRESULT SimpleDataObject::QueryGetData(FORMATETC *pFormatEtc) {
    for (int32_t i = 0; i < m_count; ++i) {
        if ((pFormatEtc->tymed & m_fmt[i].tymed) &&
            pFormatEtc->cfFormat == m_fmt[i].cfFormat &&
            pFormatEtc->dwAspect == m_fmt[i].dwAspect) {
            return S_OK;
        }
    }
    return DV_E_FORMATETC;
}

HRESULT SimpleDataObject::GetCanonicalFormatEtc(FORMATETC *pFormatEtc, FORMATETC *pFormatEtcOut) {
    return E_NOTIMPL;
}

HRESULT SimpleDataObject::SetData(FORMATETC *pFormatEtc, STGMEDIUM *pMedium, BOOL fRelease) {
    return E_NOTIMPL;
}

HRESULT SimpleDataObject::EnumFormatEtc(DWORD dwDirection, IEnumFORMATETC **ppEnumFormatEtc) {
    if (dwDirection == DATADIR_GET) {
        *ppEnumFormatEtc = new FormatEtcEnumerator(m_fmt, m_count);
        return S_OK;
    }
    return E_NOTIMPL;
}

HRESULT SimpleDataObject::DAdvise(FORMATETC *pFormatEtc, DWORD advf, IAdviseSink *pAdvSink, DWORD *pdwConnection) {
    return E_NOTIMPL;
}

HRESULT SimpleDataObject::DUnadvise(DWORD dwConnection) {
    return E_NOTIMPL;
}

HRESULT SimpleDataObject::EnumDAdvise(IEnumSTATDATA **ppEnumAdvise) {
    return E_NOTIMPL;}

HRESULT CreateDataObject(P_INSTANCE(DataInterchange)dta, FORMATETC *fmt, int32_t count, P_INSTANCE(WindowHandle)handle,
                         IDataObject **ppDataObject) {
    *ppDataObject = new SimpleDataObject(dta, fmt, count, handle);
    return (*ppDataObject) ? S_OK : E_OUTOFMEMORY;
}

void DataInterchange_CreateContext(P_INSTANCE(DataInterchange) data)
{
    IDataObject* pDataObject = nullptr;
    SingleLink_Node<FORMATETC> head = {};
    SingleLink_Node<FORMATETC>* current = &head;

    int32_t n_Format = 0;

    for (P_INSTANCE(DragDropData::Node)  n = data->data_head.next; n != nullptr; n = n->next) {
        if (strcmp(n->type, "text/plain") == 0) {
            current = current->next = new SingleLink_Node<FORMATETC>();
            memset(&current->value, 0, sizeof(current->value));

            current->value.cfFormat = CF_UNICODETEXT;
            current->value.ptd = nullptr;
            current->value.dwAspect = DVASPECT_CONTENT;
            current->value.lindex = -1;
            current->value.tymed = TYMED_HGLOBAL;

            n_Format++;
        } else if (strcmp(n->type, "text/html") == 0) {
            current = current->next = new SingleLink_Node<FORMATETC>();
            memset(&current->value, 0, sizeof(current->value));

            current->value.cfFormat = RegisterClipboardFormat(CFSTR_HTML);
            current->value.ptd = nullptr;
            current->value.dwAspect = DVASPECT_CONTENT;
            current->value.lindex = -1;
            current->value.tymed = TYMED_HGLOBAL;

            n_Format++;
        } else if (strcmp(n->type, "text/file-uri") == 0) {
            current = current->next = new SingleLink_Node<FORMATETC>();
            memset(&current->value, 0, sizeof(current->value));

            current->value.cfFormat = CF_HDROP;
            current->value.ptd = nullptr;
            current->value.dwAspect = DVASPECT_CONTENT;
            current->value.lindex = -1;
            current->value.tymed = TYMED_HGLOBAL;

            n_Format++;
        }
    }

    if (n_Format != 0) {
        FORMATETC *fmt = new FORMATETC[n_Format];

        SingleLink_Node<FORMATETC> *n;

        int32_t I = 0;
        for (SingleLink_Node<FORMATETC> *i = head.next; i != nullptr; i = n) {
            n = i->next;
            fmt[I++] = i->value;
            delete i;
        }

        HRESULT_IsError(CreateDataObject(data, fmt, n_Format, data->m_handle, &pDataObject), "CreateDataObject");
        data->context = pDataObject;
    }
}

HRESULT DataInterchange_ReadFormats(P_INSTANCE(DataInterchange) data, IDataObject * pDataObject)
{
    IEnumFORMATETC *E;

    HRESULT_IsError(pDataObject->EnumFormatEtc(DATADIR_GET, &E), "pDataObject->EnumFormatEtc");

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

            if (my_type != nullptr) DataInterchange_FormatAdd(data, my_type);
        }

    } while (fetched);

    E->Release();
    return S_OK;
}

void DataInterchange_Select(P_INSTANCE(DataInterchange) data, utf8_string_const format)
{

    FORMATETC fmt = { 0, nullptr, DVASPECT_CONTENT, -1, TYMED_HGLOBAL };

    std::string f = format;
    if (f == "text/plain") fmt.cfFormat = CF_UNICODETEXT;
    else if (f == "text/html") fmt.cfFormat = RegisterClipboardFormat(CFSTR_HTML);
    else if (f == "text/file-uri") fmt.cfFormat = CF_HDROP;

    STGMEDIUM stg;
    HRESULT hr = ((IDataObject *)data->context)->GetData(&fmt, &stg);

    if (SUCCEEDED(hr)) {
        if (f == "text/plain" || f == "text/html") {
            LPWSTR lpszText = static_cast<LPWSTR>(GlobalLock(stg.hGlobal));
            if (lpszText != nullptr) {
                std::wstring ws(lpszText);
                std::string text_data(ws.begin(), ws.end());
                GlobalUnlock(stg.hGlobal);

                DataInterchange_Selection_Set(data, format, (P_INSTANCE(void))text_data.c_str(), text_data.length() + 1);
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

                DataInterchange_Selection_Set(data, "text/file-uri", (P_INSTANCE(void) ) uri.c_str(),
                                              uri.length() + 1);
            }
        }
        ReleaseStgMedium(&stg);

		if (data->selection_type == DataInterchange::E_DND) {
    	    data->m_handle->crystal_window->callbacks.on_drag_receive_drop(data->m_handle, (P_INSTANCE(DragDropData)) data);
    	} else if (data->selection_type == DataInterchange::E_CLIPBOARD)
        	data->m_handle->crystal_window->callbacks.on_clipboard_receive_data(data->m_handle, data);

    }
}
