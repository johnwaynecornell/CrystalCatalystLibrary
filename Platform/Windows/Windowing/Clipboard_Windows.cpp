#include <iostream>
#include <sstream>
#include "Clipboard_Windows.h"
#include "SimpleDataObject.h"

using namespace JWCEssentials;

namespace NewAge {

P_INSTANCE(DataInterchange)  CrystalWindow_ClipboardPaste(P_INSTANCE(WindowHandle) handle)
{
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
    DataInterchange_CreateContext(data);
    data->m_handle = handle;
	data->selection_type = DataInterchange::E_CLIPBOARD;

    IDataObject* pDataObject = (IDataObject *)data->context;

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
    DataInterchange_CreateContext(data);
	data->selection_type = DataInterchange::E_CLIPBOARD;

    IDataObject* pDataObject = (IDataObject *)data->context;

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

void CrystalWindow_ClipboardCopyPersist(P_INSTANCE(DataInterchange) dataInterchange) {
    if (!OpenClipboard(nullptr)) {
        handleDataInterchangeError(dataInterchange ? dataInterchange->m_handle : nullptr, dataInterchange, "Failed to open clipboard.");
        return;
    }

	dataInterchange->selection_type = DataInterchange::E_CLIPBOARD;

    EmptyClipboard();

    for (DataInterchange::Node *node = dataInterchange->data_head.next; node != nullptr; node = node->next) {

        UINT cfFormat;
        HGLOBAL hGlobal;

        hGlobal = DataInterchange_MakeHGLOBAl(dataInterchange, node->type, &cfFormat);
        if (hGlobal) {
            SetClipboardData(cfFormat, hGlobal);
        } else {
            handleDataInterchangeError(dataInterchange ? dataInterchange->m_handle : nullptr, dataInterchange, "Failed to allocate global memory.");
        }
    }

    CloseClipboard();
}

void CrystalWindow_ClipboardClear()
{
    if (!OpenClipboard(nullptr)) {
        handleDataInterchangeError(nullptr, nullptr, "Failed to open clipboard.");
        return;
    }
    EmptyClipboard();
    CloseClipboard();
}
}