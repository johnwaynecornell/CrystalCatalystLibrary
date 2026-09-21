#include <iostream>
#include <sstream>
#include <vector>
#include "Clipboard_Windows.h"
#include "SimpleDataObject.h"
#include "CrystalWindow_Windows.h"

using namespace JWCEssentials;

namespace NewAge {

P_INSTANCE(DataInterchange)  CrystalWindow_ClipboardPaste(P_INSTANCE(WindowHandle) handle)
{
    Application_DiagnosticMessage("Clipboard backend: Windows (OLE) operation=paste/show-avail");
    P_INSTANCE(DataInterchange) data = DataInterchange_Create();
	data->selection_type = DataInterchange::E_CLIPBOARD;
	data->m_handle = handle;

    IDataObject* pDataObject = nullptr;
    HRESULT hr = OleGetClipboard(&pDataObject);
    if (FAILED(hr)) {
        std::stringstream ss;
        ss << "Failed to get clipboard data. HRESULT: " << std::hex << hr;
        handleDataInterchangeError(handle, data, ss.str());
        return data;
    }

    data->context = pDataObject;
    hr = DataInterchange_ReadFormats(data, pDataObject);

    if (FAILED(hr)) {
        std::stringstream ss;
        ss << "Failed to get clipboard formats. HRESULT: " << std::hex << hr;
        handleDataInterchangeError(handle, data, ss.str());
    }

    return data;
}

void CrystalWindow_ClipboardCopy(P_INSTANCE(WindowHandle) handle, P_INSTANCE(DataInterchange) data)
{
    if (!data) return;
    data->m_handle = handle;
    data->selection_type = DataInterchange::E_CLIPBOARD;
    DataInterchange_CreateContext(data);

    IDataObject* pDataObject = (IDataObject *)data->context;
    if (!pDataObject) {
        handleDataInterchangeError(handle, data, "Failed to set clipboard data: no valid data object created.");
        return;
    }

    HRESULT hr = OleSetClipboard(pDataObject);
    if (FAILED(hr)) {
        if (hr == CO_E_NOTINITIALIZED) {
            handleDataInterchangeError(handle, data, "Failed to set clipboard data. COM/OLE not initialized on this thread. "
                         "Ensure Application_Init was called and the thread is STA.");
        } else {
            std::stringstream ss;
            ss << "Failed to set clipboard data. HRESULT: " << std::hex << hr;
            handleDataInterchangeError(handle, data, ss.str());
        }
    }

    pDataObject->Release();

    data->provide_chosen = DataInterchange::provide_for_clipboard;
}

void CrystalWindow_ClipboardCopyWithCallback(void (*provide)(P_INSTANCE(DataInterchange)  data, utf8_string_struct format), P_INSTANCE(DataInterchange)  data)
{
    if (!data) return;
    data->selection_type = DataInterchange::E_CLIPBOARD;
    DataInterchange_CreateContext(data);

    IDataObject* pDataObject = (IDataObject *)data->context;
    if (!pDataObject) {
        handleDataInterchangeError(data ? data->m_handle : nullptr, data, "Failed to set clipboard data: no valid data object created.");
        return;
    }

    HRESULT hr = OleSetClipboard(pDataObject);
    if (FAILED(hr)) {
        if (hr == CO_E_NOTINITIALIZED) {
            handleDataInterchangeError(data ? data->m_handle : nullptr, data, "Failed to set clipboard data. COM/OLE not initialized on this thread. "
                         "Ensure Application_Init was called and the thread is STA.");
        } else {
            std::stringstream ss;
            ss << "Failed to set clipboard data. HRESULT: " << std::hex << hr;
            handleDataInterchangeError(data ? data->m_handle : nullptr, data, ss.str());
        }
    }

    pDataObject->Release();

    data->provide_chosen = provide;
}

void CrystalWindow_ClipboardCopyPersist(P_INSTANCE(WindowHandle) handle, P_INSTANCE(DataInterchange) dataInterchange) {
    if (!dataInterchange) return;
    if (handle) dataInterchange->m_handle = handle;
    dataInterchange->selection_type = DataInterchange::E_CLIPBOARD;
    dataInterchange->provide_chosen = DataInterchange::provide_for_clipboard;

    HWND hwnd = nullptr;
    if (handle && handle->crystal_window) {
        hwnd = ((CrystalWindow_Windows*)handle->crystal_window)->hwnd;
    }

    Application_DiagnosticMessage("Clipboard backend: Windows (SetClipboardData) operation=copy-persist");
    // Materialize every advertised format before replacing the clipboard.
    std::vector<std::pair<UINT, HGLOBAL>> prepared;
    auto releasePrepared = [&]() {
        for (auto& item : prepared) if (item.second) GlobalFree(item.second);
    };
    for (auto* node = dataInterchange->data_head.next; node; node = node->next) {
        UINT format = 0;
        HGLOBAL memory = DataInterchange_MakeHGLOBAl(dataInterchange, node->type, &format);
        if (!memory || !format) {
            if (memory) GlobalFree(memory);
            releasePrepared();
            handleDataInterchangeError(handle, dataInterchange,
                "Windows clipboard persistence: failed to prepare format " + std::string(node->type.c_str));
            return;
        }
        prepared.emplace_back(format, memory);
    }
    if (prepared.empty()) {
        handleDataInterchangeError(handle, dataInterchange, "Windows clipboard persistence: no formats supplied.");
        return;
    }
    if (!OpenClipboard(hwnd)) {
        DWORD error = GetLastError();
        releasePrepared();
        handleDataInterchangeError(handle, dataInterchange,
            "Windows OpenClipboard failed, error " + std::to_string(error));
        return;
    }
    if (!EmptyClipboard()) {
        DWORD error = GetLastError();
        releasePrepared();
        CloseClipboard();
        handleDataInterchangeError(handle, dataInterchange,
            "Windows EmptyClipboard failed, error " + std::to_string(error));
        return;
    }
    for (auto& item : prepared) {
        if (!SetClipboardData(item.first, item.second)) {
            DWORD error = GetLastError();
            releasePrepared();
            CloseClipboard();
            handleDataInterchangeError(handle, dataInterchange,
                "Windows SetClipboardData failed for format " + std::to_string(item.first) +
                ", error " + std::to_string(error));
            return;
        }
        item.second = nullptr; // Ownership transferred to Windows.
        std::string message = "Windows SetClipboardData format=" + std::to_string(item.first);
        Application_DiagnosticMessage(message.c_str());
    }

    CloseClipboard();
}

void CrystalWindow_ClipboardClear()
{
    Application_DiagnosticMessage("Clipboard backend: Windows operation=clear");
    if (!OpenClipboard(nullptr)) {
        handleDataInterchangeError(nullptr, nullptr, "Failed to open clipboard.");
        return;
    }
    EmptyClipboard();
    CloseClipboard();
}
}