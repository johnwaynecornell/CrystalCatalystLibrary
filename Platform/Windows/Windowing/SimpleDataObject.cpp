#include <vector>
#include <ShlObj_core.h>
#include <iostream>
#include <sstream>
#include <wingdi.h>
#include "CrystalWindow_Windows.h"
#include "Clipboard_Windows.h"
#include "SimpleDataObject.h"

using namespace JWCEssentials;

namespace NewAge {

static bool DibToBmpStream(const void* dibData, size_t dibSize, std::vector<uint8_t>& bmpOut) {
    if (!dibData || dibSize < sizeof(DWORD)) return false;

    const uint8_t* pDib = static_cast<const uint8_t*>(dibData);

    // If it already has a BITMAPFILEHEADER (starts with 'BM' and size > 14)
    if (dibSize >= sizeof(BITMAPFILEHEADER) && pDib[0] == 'B' && pDib[1] == 'M') {
        bmpOut.assign(pDib, pDib + dibSize);
        return true;
    }

    DWORD headerSize = *reinterpret_cast<const DWORD*>(pDib);
    if (headerSize > dibSize) return false;

    DWORD offsetInDib = headerSize;

    if (headerSize == sizeof(BITMAPINFOHEADER)) { // 40 bytes
        const BITMAPINFOHEADER* bih = reinterpret_cast<const BITMAPINFOHEADER*>(pDib);
        DWORD clrUsed = bih->biClrUsed;
        if (clrUsed == 0 && bih->biBitCount <= 8 && bih->biBitCount > 0) {
            clrUsed = (1U << bih->biBitCount);
        }
        DWORD clrSize = clrUsed * sizeof(RGBQUAD);
        if (bih->biCompression == BI_BITFIELDS && bih->biClrUsed == 0) {
            clrSize = 3 * sizeof(DWORD);
        }
        offsetInDib = sizeof(BITMAPINFOHEADER) + clrSize;
    } else if (headerSize == sizeof(BITMAPV5HEADER)) { // 124 bytes
        const BITMAPV5HEADER* bV5 = reinterpret_cast<const BITMAPV5HEADER*>(pDib);
        DWORD clrUsed = bV5->bV5ClrUsed;
        if (clrUsed == 0 && bV5->bV5BitCount <= 8 && bV5->bV5BitCount > 0) {
            clrUsed = (1U << bV5->bV5BitCount);
        }
        offsetInDib = sizeof(BITMAPV5HEADER) + clrUsed * sizeof(RGBQUAD);
    } else if (headerSize == sizeof(BITMAPV4HEADER)) { // 108 bytes
        const BITMAPV4HEADER* bV4 = reinterpret_cast<const BITMAPV4HEADER*>(pDib);
        DWORD clrUsed = bV4->bV4ClrUsed;
        if (clrUsed == 0 && bV4->bV4BitCount <= 8 && bV4->bV4BitCount > 0) {
            clrUsed = (1U << bV4->bV4BitCount);
        }
        offsetInDib = sizeof(BITMAPV4HEADER) + clrUsed * sizeof(RGBQUAD);
    } else if (headerSize == sizeof(BITMAPCOREHEADER)) { // 12 bytes
        const BITMAPCOREHEADER* bch = reinterpret_cast<const BITMAPCOREHEADER*>(pDib);
        DWORD clrUsed = (bch->bcBitCount <= 8 && bch->bcBitCount > 0) ? (1U << bch->bcBitCount) : 0;
        offsetInDib = sizeof(BITMAPCOREHEADER) + clrUsed * sizeof(RGBTRIPLE);
    }

    if (offsetInDib > dibSize) {
        offsetInDib = headerSize;
    }

    BITMAPFILEHEADER bfh = {};
    bfh.bfType = 0x4D42; // "BM"
    bfh.bfSize = static_cast<DWORD>(sizeof(BITMAPFILEHEADER) + dibSize);
    bfh.bfReserved1 = 0;
    bfh.bfReserved2 = 0;
    bfh.bfOffBits = static_cast<DWORD>(sizeof(BITMAPFILEHEADER) + offsetInDib);

    bmpOut.resize(sizeof(BITMAPFILEHEADER) + dibSize);
    memcpy(bmpOut.data(), &bfh, sizeof(BITMAPFILEHEADER));
    memcpy(bmpOut.data() + sizeof(BITMAPFILEHEADER), pDib, dibSize);
    return true;
}

static void BmpStreamToDib(const void* bmpData, size_t bmpSize, const void*& dibOut, size_t& dibSizeOut) {
    if (!bmpData || bmpSize == 0) {
        dibOut = nullptr;
        dibSizeOut = 0;
        return;
    }
    const uint8_t* p = static_cast<const uint8_t*>(bmpData);
    if (bmpSize >= sizeof(BITMAPFILEHEADER) && p[0] == 'B' && p[1] == 'M') {
        dibOut = p + sizeof(BITMAPFILEHEADER);
        dibSizeOut = bmpSize - sizeof(BITMAPFILEHEADER);
    } else {
        dibOut = bmpData;
        dibSizeOut = bmpSize;
    }
}

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

HGLOBAL DataInterchange_MakeHGLOBAl(P_INSTANCE(DataInterchange) dataInterchange, utf8_string_struct format, UINT *cfFormat)
{
    P_INSTANCE(void)data_ptr = nullptr;
    size_t size = 0;

    std::string  f = format.c_str;

    if (cfFormat != nullptr) {

        if (f == "text/plain") {
            *cfFormat = CF_UNICODETEXT;
        } else if (f == "text/html") {
            *cfFormat = RegisterClipboardFormat(CFSTR_HTML);
        } else if (f == "text/file-uri") {
            *cfFormat = CF_HDROP;
        } else if (f == "image/png") {
            *cfFormat = RegisterClipboardFormatA("PNG");
        } else if (f == "image/bmp") {
            *cfFormat = CF_DIB;
        } else {
            // Unsupported format, return DV_E_FORMATETC
            return nullptr;
        }
    }

    dataInterchange->provide_chosen(dataInterchange, format);
    DataInterchange_SelectionReveal(dataInterchange, &format, &data_ptr, &size);


    HGLOBAL hGlobal = nullptr;

    if (strcmp(format, "text/plain") == 0) {
        std::string utf8_text = std::string((char *) data_ptr, size);
        int32_t len = MultiByteToWideChar(CP_UTF8, 0, utf8_text.c_str(), -1, nullptr, 0);
        hGlobal = GlobalAlloc(GMEM_MOVEABLE, (len + 1) * sizeof(WCHAR));
        if (hGlobal) {
            LPWSTR lpszText = (LPWSTR) GlobalLock(hGlobal);
            if (lpszText) {
                MultiByteToWideChar(CP_UTF8, 0, utf8_text.c_str(), -1, lpszText, len);
                lpszText[len] = 0; // Ensure null terminator
                GlobalUnlock(hGlobal);
            }
        }
    } else if (strcmp(format, "text/html") == 0) {
        hGlobal = GlobalAlloc(GMEM_MOVEABLE, size + 1);
        if (hGlobal) {
            char* lpszText = (char*)GlobalLock(hGlobal);
            if (lpszText) {
                memcpy(lpszText, data_ptr, size);
                lpszText[size] = 0;
                GlobalUnlock(hGlobal);
            }
        }
    } else if (strcmp(format, "image/png") == 0) {
        hGlobal = GlobalAlloc(GMEM_MOVEABLE, size);
        if (hGlobal) {
            void* lpszData = GlobalLock(hGlobal);
            if (lpszData) {
                memcpy(lpszData, data_ptr, size);
                GlobalUnlock(hGlobal);
            }
        }
    } else if (strcmp(format, "image/bmp") == 0) {
        const void* dibSrc = nullptr;
        size_t dibSize = 0;
        BmpStreamToDib(data_ptr, size, dibSrc, dibSize);
        if (dibSrc && dibSize > 0) {
            hGlobal = GlobalAlloc(GMEM_MOVEABLE, dibSize);
            if (hGlobal) {
                void* lpszData = GlobalLock(hGlobal);
                if (lpszData) {
                    memcpy(lpszData, dibSrc, dibSize);
                    GlobalUnlock(hGlobal);
                }
            }
        }
    } else if (strcmp(format, "text/file-uri") == 0) {
        // Parse URIs
        std::string uri_list = std::string((char *) data_ptr, size);
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
                LPWSTR cur = pwsz;
                size_t max = (total_size - sizeof(WCHAR) - sizeof(DROPFILES)) / sizeof(WCHAR);

                for (std::wstring &file: files) {
                    int i;
                    for (i=0; i<max+1 && i < file.length(); i++)
                    {
                        cur[i] = file[i];
                    }
                    if (i == max+1)
                        throw std::runtime_error("corruption");

                    cur[i] = 0;

                    cur += i; // Move the pointer to the next location
                    max -= i;// on after the null terminator
                }
                *cur = L'\0'; // Extra null terminator

                GlobalUnlock(hGlobal);
            }
        }
    }

    return hGlobal;
}

HRESULT SimpleDataObject::GetData(FORMATETC *pFormatEtc, STGMEDIUM *pMedium) {
    utf8_string_struct format = nullptr;
    P_INSTANCE(void)data_ptr = nullptr;
    size_t size = 0;

    if (pFormatEtc->cfFormat == CF_UNICODETEXT) {
        format = "text/plain";
    } else if (pFormatEtc->cfFormat == RegisterClipboardFormat(CFSTR_HTML)) {
        format = "text/html";
    } else if (pFormatEtc->cfFormat == CF_HDROP) {
        format = "text/file-uri";
    } else if (pFormatEtc->cfFormat == RegisterClipboardFormatA("PNG")) {
        format = "image/png";
    } else if (pFormatEtc->cfFormat == CF_DIB || pFormatEtc->cfFormat == CF_DIBV5) {
        format = "image/bmp";
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
        } else if (strcmp(n->type, "image/png") == 0) {
            current = current->next = new SingleLink_Node<FORMATETC>();
            memset(&current->value, 0, sizeof(current->value));

            current->value.cfFormat = RegisterClipboardFormatA("PNG");
            current->value.ptd = nullptr;
            current->value.dwAspect = DVASPECT_CONTENT;
            current->value.lindex = -1;
            current->value.tymed = TYMED_HGLOBAL;

            n_Format++;
        } else if (strcmp(n->type, "image/bmp") == 0) {
            current = current->next = new SingleLink_Node<FORMATETC>();
            memset(&current->value, 0, sizeof(current->value));

            current->value.cfFormat = CF_DIB;
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

        HRESULT hr = CreateDataObject(data, fmt, n_Format, data->m_handle, &pDataObject);
        if (FAILED(hr)) {
            std::stringstream ss;
            ss << mod_header() << " CreateDataObject failed. HRESULT: " << std::hex << hr;
            handleDataInterchangeError(data ? data->m_handle : nullptr, data, ss.str());
        }
        data->context = pDataObject;
    }
}

HRESULT DataInterchange_ReadFormats(P_INSTANCE(DataInterchange) data, IDataObject * pDataObject)
{
    IEnumFORMATETC *E;

    HRESULT hr = pDataObject->EnumFormatEtc(DATADIR_GET, &E);
    if (FAILED(hr)) {
        std::stringstream ss;
        ss << mod_header() << " EnumFormatEtc failed. HRESULT: " << std::hex << hr;
        handleDataInterchangeError(data ? data->m_handle : nullptr, data, ss.str());
        return hr;
    }

    ULONG fetched = 0;

    FORMATETC fmt;
    char name[1024];
    UINT cf_png = RegisterClipboardFormatA("PNG");

    do {
        E->Next(1, &fmt, &fetched);
        if (fetched) {
            utf8_string_struct my_type = nullptr;

            if (fmt.cfFormat == CF_UNICODETEXT) my_type = "text/plain";
            else if (fmt.cfFormat == RegisterClipboardFormat(CFSTR_HTML)) my_type = "text/html";
            else if (fmt.cfFormat == CF_HDROP) my_type = "text/file-uri";
            else if (fmt.cfFormat == cf_png) my_type = "image/png";
            else if (fmt.cfFormat == CF_DIB || fmt.cfFormat == CF_DIBV5) my_type = "image/bmp";

            name[GetClipboardFormatNameA(fmt.cfFormat, name, 1023)] = 0;

            std::string my_t;
            if (my_type == nullptr) my_t = "nullptr";
            else my_t = (std::string) "\"" + my_type.c_str + "\"";

            std::cerr << mod_header() << "\ttype:" << my_t << "\tformat:" << fmt.cfFormat << "\tName:\"" << name << "\"" << std::endl;

            if (my_type != nullptr) {
                if (!DataInterchange_FormatExists(data, my_type)) {
                    DataInterchange_FormatAdd(data, my_type);
                }
            }
        }

    } while (fetched);

    E->Release();
    return S_OK;
}

void DataInterchange_Select(P_INSTANCE(DataInterchange) data, utf8_string_struct format)
{

    FORMATETC fmt = { 0, nullptr, DVASPECT_CONTENT, -1, TYMED_HGLOBAL };

    std::string f = format.c_str;
    if (f == "text/plain") fmt.cfFormat = CF_UNICODETEXT;
    else if (f == "text/html") fmt.cfFormat = RegisterClipboardFormat(CFSTR_HTML);
    else if (f == "text/file-uri") fmt.cfFormat = CF_HDROP;
    else if (f == "image/png") fmt.cfFormat = RegisterClipboardFormatA("PNG");
    else if (f == "image/bmp") {
        fmt.cfFormat = CF_DIB;
        if (data && data->context && ((IDataObject *)data->context)->QueryGetData(&fmt) != S_OK) {
            fmt.cfFormat = CF_DIBV5;
        }
    }

    if (!data || !data->context) {
        handleDataInterchangeError(data ? data->m_handle : nullptr, data, ((std::string) mod_header() + " DataInterchange context is null.").c_str());
        return;
    }

    STGMEDIUM stg;
    HRESULT hr = ((IDataObject *)data->context)->GetData(&fmt, &stg);

    if (SUCCEEDED(hr)) {
        if (f == "text/plain") {
            LPWSTR lpszText = static_cast<LPWSTR>(GlobalLock(stg.hGlobal));
            if (lpszText != nullptr) {
                std::wstring ws(lpszText);
                int size_needed = WideCharToMultiByte(CP_UTF8, 0, ws.c_str(), (int)ws.length(), NULL, 0, NULL, NULL);
                std::string text_data(size_needed, 0);
                WideCharToMultiByte(CP_UTF8, 0, ws.c_str(), (int)ws.length(), &text_data[0], size_needed, NULL, NULL);
                GlobalUnlock(stg.hGlobal);

                DataInterchange_SelectionSet(data, format, (P_INSTANCE(void))text_data.c_str(), text_data.length() + 1);
            }
        } else if (f == "text/html") {
            char* lpszText = static_cast<char*>(GlobalLock(stg.hGlobal));
            if (lpszText != nullptr) {
                size_t data_size = GlobalSize(stg.hGlobal);
                // HTML Format is usually UTF-8.
                DataInterchange_SelectionSet(data, format, (P_INSTANCE(void))lpszText, data_size);
                GlobalUnlock(stg.hGlobal);
            }
        } else if (f == "image/png") {
            void* pBytes = GlobalLock(stg.hGlobal);
            if (pBytes != nullptr) {
                size_t data_size = GlobalSize(stg.hGlobal);
                DataInterchange_SelectionSet(data, format, pBytes, data_size);
                GlobalUnlock(stg.hGlobal);
            }
        } else if (f == "image/bmp") {
            void* pBytes = GlobalLock(stg.hGlobal);
            if (pBytes != nullptr) {
                size_t dib_size = GlobalSize(stg.hGlobal);
                std::vector<uint8_t> bmpData;
                if (DibToBmpStream(pBytes, dib_size, bmpData)) {
                    DataInterchange_SelectionSet(data, format, bmpData.data(), bmpData.size());
                } else {
                    DataInterchange_SelectionSet(data, format, pBytes, dib_size);
                }
                GlobalUnlock(stg.hGlobal);
            }
        } else if (f == "text/file-uri") {
            HDROP hDrop = static_cast<HDROP>(GlobalLock(stg.hGlobal));
            if (hDrop != nullptr) {
                uint32_t  fileCount = DragQueryFile(hDrop, 0xFFFFFFFF, nullptr, 0);

                std::cout << "drag of " << fileCount << " files" << std::endl;

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

                DataInterchange_SelectionSet(data, "text/file-uri", (P_INSTANCE(void) ) uri.c_str(),
                                              uri.length() + 1);
            }
        }
        ReleaseStgMedium(&stg);

		if (data->selection_type == DataInterchange::E_DND) {
    	    data->m_handle->crystal_window->callbacks.on_drag_receive_drop(data->m_handle, (P_INSTANCE(DragDropData)) data);
    	} else if (data->selection_type == DataInterchange::E_CLIPBOARD)
        	data->m_handle->crystal_window->callbacks.on_clipboard_receive_data(data->m_handle, data);

    } else {
        std::stringstream ss;
        ss << mod_header() << " GetData failed for format " << f << ". HRESULT: " << std::hex << hr;
        handleDataInterchangeError(data ? data->m_handle : nullptr, data, ss.str());
    }
}
}